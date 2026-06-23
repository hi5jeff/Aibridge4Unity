#nullable enable
using System.Globalization;
using AIBridge.Editor.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AIBridge.Editor.Handlers
{
    /// <summary>
    /// Creates a configured ParticleSystem from a preset (burst / explosion / stream / sparkle), using a
    /// 2D-friendly Sprites/Default material so it renders in URP 2D. Particles play in Play mode.
    /// </summary>
    public class ParticleCreateHandler : ICommandHandler
    {
        public string Command => "particle.create";

        [System.Serializable]
        class Request
        {
            public string name = "Particles";
            public string parentPath = "";
            public Vec3 position = new();
            public string preset = "burst";   // burst | explosion | stream | sparkle
            public string color = "1,1,1,1";
            public float size = 0.3f;
            public float lifetime = 1f;
            public int count = 30;
        }

        [System.Serializable] class Result { public ObjInfo created = new(); public string preset = ""; }

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();
            var scene = SceneManager.GetActiveScene();

            var go = new GameObject(string.IsNullOrEmpty(req.name) ? "Particles" : req.name);
            if (!string.IsNullOrEmpty(req.parentPath))
            {
                var parent = SceneLookup.FindByPath(req.parentPath, scene);
                if (parent != null) go.transform.SetParent(parent.transform, false);
            }
            go.transform.position = new Vector3(req.position.x, req.position.y, req.position.z);

            var ps = go.AddComponent<ParticleSystem>();
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));

            var col = ParseColor(req.color);
            var preset = (req.preset ?? "burst").ToLowerInvariant();

            var main = ps.main;
            main.startColor = col;
            main.startSize = req.size;
            main.startLifetime = req.lifetime;
            main.duration = Mathf.Max(0.2f, req.lifetime);
            main.loop = preset is "stream" or "sparkle";

            var emission = ps.emission;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.1f;

            switch (preset)
            {
                case "explosion":
                    main.startSpeed = 6f;
                    shape.radius = 0.05f;
                    emission.rateOverTime = 0f;
                    emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)req.count) });
                    break;
                case "stream":
                    main.startSpeed = 2f;
                    emission.rateOverTime = req.count;
                    break;
                case "sparkle":
                    main.startSpeed = 0.5f;
                    main.startSize = req.size * 0.5f;
                    shape.radius = 0.4f;
                    emission.rateOverTime = req.count;
                    break;
                default: // burst
                    main.startSpeed = 2.5f;
                    emission.rateOverTime = 0f;
                    emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)req.count) });
                    break;
            }

            Undo.RegisterCreatedObjectUndo(go, "AI Bridge: create particles");
            EditorUtility.SetDirty(go);
            if (!EditorApplication.isPlaying)
                EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = go;

            return CommandResult.Success(JsonUtility.ToJson(new Result
            {
                created = SceneObjectDescriber.Describe(go.transform),
                preset = preset
            }));
        }

        static Color ParseColor(string s)
        {
            var p = s.Split(',');
            float F(int i) => float.Parse(p[i].Trim(), CultureInfo.InvariantCulture);
            return p.Length >= 3 ? new Color(F(0), F(1), F(2), p.Length > 3 ? F(3) : 1f) : Color.white;
        }
    }
}

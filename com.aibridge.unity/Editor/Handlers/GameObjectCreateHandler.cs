#nullable enable
using System.Collections.Generic;
using AIBridge.Editor.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AIBridge.Editor.Handlers
{
    /// <summary>Creates a GameObject (empty or a primitive), optionally parented, positioned, and pre-loaded
    /// with components — so the AI can build scenes without manual Hierarchy work.</summary>
    public class GameObjectCreateHandler : ICommandHandler
    {
        public string Command => "gameobject.create";

        [System.Serializable]
        class Request
        {
            public string name = "GameObject";
            public string parentPath = "";                 // empty = scene root
            public string primitive = "";                  // "", Cube, Sphere, Capsule, Cylinder, Plane, Quad
            public Vec3 position = new();                   // world position
            public string[] components = System.Array.Empty<string>();
        }

        [System.Serializable]
        class Result
        {
            public ObjInfo created = new();
            public string[] unresolvedComponents = System.Array.Empty<string>();
        }

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();
            var scene = SceneManager.GetActiveScene();

            GameObject go;
            if (!string.IsNullOrEmpty(req.primitive))
            {
                if (!System.Enum.TryParse<PrimitiveType>(req.primitive, true, out var pt))
                    return CommandResult.Failure($"Unknown primitive '{req.primitive}'. Use Cube/Sphere/Capsule/Cylinder/Plane/Quad.");
                go = GameObject.CreatePrimitive(pt);
                go.name = string.IsNullOrEmpty(req.name) ? pt.ToString() : req.name;
            }
            else
            {
                go = new GameObject(string.IsNullOrEmpty(req.name) ? "GameObject" : req.name);
            }

            if (!string.IsNullOrEmpty(req.parentPath))
            {
                var parent = SceneLookup.FindByPath(req.parentPath, scene);
                if (parent == null)
                {
                    Object.DestroyImmediate(go);
                    return CommandResult.Failure($"Parent not found: '{req.parentPath}'.");
                }
                go.transform.SetParent(parent.transform, false);
            }

            go.transform.position = new Vector3(req.position.x, req.position.y, req.position.z);

            var unresolved = new List<string>();
            foreach (var cn in req.components)
            {
                var type = ComponentTypeResolver.Resolve(cn);
                if (type == null) { unresolved.Add(cn); continue; }
                if (go.GetComponent(type) == null)
                    go.AddComponent(type);
            }

            Undo.RegisterCreatedObjectUndo(go, "AI Bridge: create " + go.name);
            EditorUtility.SetDirty(go);
            if (!EditorApplication.isPlaying)
                EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = go;

            var result = new Result
            {
                created = SceneObjectDescriber.Describe(go.transform),
                unresolvedComponents = unresolved.ToArray()
            };
            return CommandResult.Success(JsonUtility.ToJson(result));
        }
    }
}

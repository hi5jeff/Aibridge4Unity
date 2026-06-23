#nullable enable
using AIBridge.Editor.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AIBridge.Editor.Handlers
{
    /// <summary>Instantiates a prefab asset into the active scene (optional name / parent / position).</summary>
    public class PrefabInstantiateHandler : ICommandHandler
    {
        public string Command => "prefab.instantiate";

        [System.Serializable]
        class Request
        {
            public string assetPath = "";
            public string name = "";
            public string parentPath = "";
            public bool setPosition;
            public Vec3 position = new();
        }

        [System.Serializable] class Result { public ObjInfo created = new(); }

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();
            if (string.IsNullOrEmpty(req.assetPath))
                return CommandResult.Failure("Required: assetPath.");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(req.assetPath);
            if (prefab == null)
                return CommandResult.Failure($"Prefab not found: '{req.assetPath}'.");

            var scene = SceneManager.GetActiveScene();
            var go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (go == null)
                return CommandResult.Failure("InstantiatePrefab returned null.");

            if (!string.IsNullOrEmpty(req.name))
                go.name = req.name;

            if (!string.IsNullOrEmpty(req.parentPath))
            {
                var parent = SceneLookup.FindByPath(req.parentPath, scene);
                if (parent != null)
                    go.transform.SetParent(parent.transform, false);
            }

            if (req.setPosition)
                go.transform.position = new Vector3(req.position.x, req.position.y, req.position.z);

            Undo.RegisterCreatedObjectUndo(go, "AI Bridge: instantiate prefab");
            EditorUtility.SetDirty(go);
            if (!EditorApplication.isPlaying)
                EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = go;

            return CommandResult.Success(JsonUtility.ToJson(new Result { created = SceneObjectDescriber.Describe(go.transform) }));
        }
    }
}

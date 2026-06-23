#nullable enable
using AIBridge.Editor.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AIBridge.Editor.Handlers
{
    /// <summary>Clones an existing GameObject (with all its components) — e.g. to spawn many copies of a
    /// sprite "template". Optionally renames and repositions the clone. Records Undo.</summary>
    public class GameObjectDuplicateHandler : ICommandHandler
    {
        public string Command => "gameobject.duplicate";

        [System.Serializable]
        class Request
        {
            public string path = "";
            public string name = "";
            public bool setPosition;     // when true, place the clone at `position`
            public Vec3 position = new();
        }

        [System.Serializable]
        class Result { public ObjInfo created = new(); }

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();
            if (string.IsNullOrEmpty(req.path))
                return CommandResult.Failure("Required: path.");

            var scene = SceneManager.GetActiveScene();
            var src = SceneLookup.FindByPath(req.path, scene);
            if (src == null)
                return CommandResult.Failure($"GameObject not found: '{req.path}'.");

            var clone = Object.Instantiate(src, src.transform.parent);
            clone.name = string.IsNullOrEmpty(req.name) ? src.name : req.name;
            if (req.setPosition)
                clone.transform.position = new Vector3(req.position.x, req.position.y, req.position.z);

            Undo.RegisterCreatedObjectUndo(clone, "AI Bridge: duplicate " + src.name);
            EditorUtility.SetDirty(clone);
            if (!EditorApplication.isPlaying)
                EditorSceneManager.MarkSceneDirty(scene);

            return CommandResult.Success(JsonUtility.ToJson(new Result
            {
                created = SceneObjectDescriber.Describe(clone.transform)
            }));
        }
    }
}

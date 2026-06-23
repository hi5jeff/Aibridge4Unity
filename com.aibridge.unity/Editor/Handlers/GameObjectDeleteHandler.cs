#nullable enable
using AIBridge.Editor.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AIBridge.Editor.Handlers
{
    /// <summary>Deletes a GameObject by path (Undo-able), so the AI can clean up its own mistakes.</summary>
    public class GameObjectDeleteHandler : ICommandHandler
    {
        public string Command => "gameobject.delete";

        [System.Serializable]
        class Request { public string path = ""; }

        [System.Serializable]
        class Result { public string deleted = ""; }

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();
            if (string.IsNullOrEmpty(req.path))
                return CommandResult.Failure("Required: path.");

            var scene = SceneManager.GetActiveScene();
            var go = SceneLookup.FindByPath(req.path, scene);
            if (go == null)
                return CommandResult.Failure($"GameObject not found: '{req.path}'.");

            var path = SceneObjectDescriber.GetPath(go.transform);
            Undo.DestroyObjectImmediate(go);
            if (!EditorApplication.isPlaying)
                EditorSceneManager.MarkSceneDirty(scene);

            return CommandResult.Success(JsonUtility.ToJson(new Result { deleted = path }));
        }
    }
}

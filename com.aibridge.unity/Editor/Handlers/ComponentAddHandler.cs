#nullable enable
using AIBridge.Editor.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AIBridge.Editor.Handlers
{
    /// <summary>Adds a component (by type name) to a GameObject — the auto version of "Add Component".</summary>
    public class ComponentAddHandler : ICommandHandler
    {
        public string Command => "component.add";

        [System.Serializable]
        class Request
        {
            public string path = "";
            public string component = "";
        }

        [System.Serializable]
        class Result
        {
            public string path = "";
            public string added = "";
            public string[] components = System.Array.Empty<string>();
        }

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();
            if (string.IsNullOrEmpty(req.path) || string.IsNullOrEmpty(req.component))
                return CommandResult.Failure("Required: path, component.");

            var scene = SceneManager.GetActiveScene();
            var go = SceneLookup.FindByPath(req.path, scene);
            if (go == null)
                return CommandResult.Failure($"GameObject not found: '{req.path}'.");

            var type = ComponentTypeResolver.Resolve(req.component);
            if (type == null)
                return CommandResult.Failure($"Component type not found: '{req.component}'.");

            Component added;
            try { added = Undo.AddComponent(go, type); }
            catch (System.Exception e) { return CommandResult.Failure($"AddComponent failed: {e.Message}"); }
            if (added == null)
                return CommandResult.Failure($"Could not add '{req.component}' (duplicate or disallowed on this object).");

            EditorUtility.SetDirty(go);
            if (!EditorApplication.isPlaying)
                EditorSceneManager.MarkSceneDirty(scene);

            var info = SceneObjectDescriber.Describe(go.transform);
            return CommandResult.Success(JsonUtility.ToJson(new Result
            {
                path = info.path,
                added = type.Name,
                components = info.components
            }));
        }
    }
}

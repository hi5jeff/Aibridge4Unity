#nullable enable
using AIBridge.Editor.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AIBridge.Editor.Handlers
{
    /// <summary>
    /// Wires an object-reference field programmatically — the auto-link that replaces dragging things into
    /// the Inspector. Uses SerializedObject so it records Undo and handles prefab overrides correctly.
    /// </summary>
    public class ReferenceWireHandler : ICommandHandler
    {
        public string Command => "reference.wire";

        [System.Serializable]
        class Request
        {
            public string sourcePath = "";         // GameObject holding the component to modify
            public string sourceComponent = "";    // component type name on that GameObject
            public int sourceComponentIndex;        // when several of the same type exist (default 0)
            public string field = "";              // serialized field name to set

            // Provide ONE of the following to choose what gets assigned (or none to clear the field):
            public string targetPath = "";         // a scene GameObject...
            public string targetComponent = "";    // ...optionally a specific component on it
            public string targetAssetPath = "";    // ...or an asset (e.g. Assets/Art/icon.png)
        }

        [System.Serializable]
        class Result
        {
            public string source = "";
            public string field = "";
            public string assigned = "";
        }

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();

            if (string.IsNullOrEmpty(req.sourcePath) || string.IsNullOrEmpty(req.sourceComponent) || string.IsNullOrEmpty(req.field))
                return CommandResult.Failure("Required: sourcePath, sourceComponent, field.");

            var scene = SceneManager.GetActiveScene();

            var srcGo = SceneLookup.FindByPath(req.sourcePath, scene);
            if (srcGo == null)
                return CommandResult.Failure($"Source GameObject not found: '{req.sourcePath}'.");

            var component = SceneLookup.GetComponent(srcGo, req.sourceComponent, req.sourceComponentIndex);
            if (component == null)
                return CommandResult.Failure($"Component '{req.sourceComponent}' (index {req.sourceComponentIndex}) not found on '{req.sourcePath}'.");

            var so = new SerializedObject(component);
            var prop = so.FindProperty(req.field);
            if (prop == null)
                return CommandResult.Failure($"Field '{req.field}' not found on '{req.sourceComponent}'. Use the serialized field name.");
            if (prop.propertyType != SerializedPropertyType.ObjectReference)
                return CommandResult.Failure($"Field '{req.field}' is not an object-reference field (type: {prop.propertyType}).");

            Object? value;
            string assigned;
            if (!string.IsNullOrEmpty(req.targetAssetPath))
            {
                value = AssetDatabase.LoadAssetAtPath<Object>(req.targetAssetPath);
                if (value == null)
                    return CommandResult.Failure($"Asset not found: '{req.targetAssetPath}'.");
                assigned = "asset: " + req.targetAssetPath;
            }
            else if (!string.IsNullOrEmpty(req.targetPath))
            {
                var tgtGo = SceneLookup.FindByPath(req.targetPath, scene);
                if (tgtGo == null)
                    return CommandResult.Failure($"Target GameObject not found: '{req.targetPath}'.");

                if (!string.IsNullOrEmpty(req.targetComponent))
                {
                    var tcomp = SceneLookup.GetComponent(tgtGo, req.targetComponent, 0);
                    if (tcomp == null)
                        return CommandResult.Failure($"Component '{req.targetComponent}' not found on '{req.targetPath}'.");
                    value = tcomp;
                    assigned = $"{req.targetPath} ({req.targetComponent})";
                }
                else
                {
                    value = tgtGo;
                    assigned = $"{req.targetPath} (GameObject)";
                }
            }
            else
            {
                value = null;
                assigned = "null (cleared)";
            }

            prop.objectReferenceValue = value;
            so.ApplyModifiedProperties();          // registers Undo automatically
            EditorUtility.SetDirty(component);
            if (!EditorApplication.isPlaying)
                EditorSceneManager.MarkSceneDirty(scene);

            // Unity silently rejects a type-incompatible reference — verify the assignment actually took.
            so.Update();
            var check = so.FindProperty(req.field);
            if (value != null && check.objectReferenceValue == null)
                return CommandResult.Failure(
                    $"Type mismatch: '{req.sourceComponent}.{req.field}' cannot hold that object. " +
                    "Specify a compatible targetComponent or asset.");

            var result = new Result
            {
                source = $"{req.sourcePath} ({req.sourceComponent})",
                field = req.field,
                assigned = assigned
            };
            return CommandResult.Success(JsonUtility.ToJson(result));
        }
    }
}

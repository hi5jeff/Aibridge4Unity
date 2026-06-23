#nullable enable
using AIBridge.Editor.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AIBridge.Editor.Handlers
{
    /// <summary>
    /// Assigns a sprite (by asset path) to a target's sprite field — works for SpriteRenderer and UI Image
    /// (set via SerializedObject's `m_Sprite`, no UI-assembly dependency). If the asset isn't imported as a
    /// Sprite yet, its texture import settings are switched to Sprite and reimported first.
    /// </summary>
    public class SpriteSetHandler : ICommandHandler
    {
        public string Command => "sprite.set";

        [System.Serializable]
        class Request
        {
            public string path = "";        // target GameObject
            public string assetPath = "";   // the sprite/texture asset
            public string component = "";   // optional: which component to set (else first with an m_Sprite field)
        }

        [System.Serializable]
        class Result { public string target = ""; public string assetPath = ""; public string sprite = ""; public string component = ""; }

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();
            if (string.IsNullOrEmpty(req.path) || string.IsNullOrEmpty(req.assetPath))
                return CommandResult.Failure("Required: path, assetPath.");

            var scene = SceneManager.GetActiveScene();
            var go = SceneLookup.FindByPath(req.path, scene);
            if (go == null)
                return CommandResult.Failure($"GameObject not found: '{req.path}'.");

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(req.assetPath);
            if (sprite == null)
            {
                // Not a sprite yet — try switching the texture import type to Sprite, then reimport.
                if (AssetImporter.GetAtPath(req.assetPath) is TextureImporter importer)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.SaveAndReimport();
                    sprite = AssetDatabase.LoadAssetAtPath<Sprite>(req.assetPath);
                }
            }
            if (sprite == null)
                return CommandResult.Failure($"No Sprite at '{req.assetPath}' (and it could not be imported as one).");

            // Find the component that holds an 'm_Sprite' field (SpriteRenderer, UI Image, ...).
            Component? target = null;
            foreach (var c in go.GetComponents<Component>())
            {
                if (c == null) continue;
                if (!string.IsNullOrEmpty(req.component) && c.GetType().Name != req.component) continue;
                if (new SerializedObject(c).FindProperty("m_Sprite") != null) { target = c; break; }
            }
            if (target == null)
                return CommandResult.Failure("No component with a sprite field (e.g. SpriteRenderer or Image) on the target.");

            var so = new SerializedObject(target);
            so.FindProperty("m_Sprite").objectReferenceValue = sprite;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            if (!EditorApplication.isPlaying)
                EditorSceneManager.MarkSceneDirty(scene);

            return CommandResult.Success(JsonUtility.ToJson(new Result
            {
                target = req.path,
                assetPath = req.assetPath,
                sprite = sprite.name,
                component = target.GetType().Name
            }));
        }
    }
}

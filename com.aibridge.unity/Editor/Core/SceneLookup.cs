#nullable enable
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AIBridge.Editor.Core
{
    /// <summary>Resolves scene GameObjects by hierarchy path and components by type name. Shared by handlers.</summary>
    public static class SceneLookup
    {
        /// <summary>
        /// Resolve a target GameObject for editing, robust to where the user actually is:
        /// • "@selection" / "@sel" / "@" → the current Editor selection (works in ANY context, including an
        ///   open Prefab Mode stage, which a plain scene-path lookup cannot reach).
        /// • otherwise a "Root/Child/Leaf" path, searched in the active scene first, then the open Prefab Mode
        ///   stage (so you can target a node inside a prefab you're editing), with a recursive leaf fallback.
        /// Edit commands (object.modify / component.set) use this so they act on what you have selected — even
        /// inside a prefab.
        /// </summary>
        public static GameObject? FindByPathOrSelection(string path)
        {
            if (path == "@selection" || path == "@sel" || path == "@")
                return Selection.activeGameObject;

            var go = FindByPath(path, SceneManager.GetActiveScene());
            if (go != null) return go;

            // Reach into an open Prefab Mode stage (its objects aren't in any loaded scene).
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            var root = stage != null ? stage.prefabContentsRoot : null;
            if (root != null)
            {
                var parts = path.Split('/');
                if (root.name == parts[0])
                {
                    if (parts.Length == 1) return root;
                    var t = root.transform.Find(string.Join("/", parts.Skip(1)));
                    if (t != null) return t.gameObject;
                }
                var leaf = parts[parts.Length - 1];
                foreach (var tr in root.GetComponentsInChildren<Transform>(true))
                    if (tr.name == leaf) return tr.gameObject;
            }
            return null;
        }

        /// <summary>Finds a GameObject by "Root/Child/Leaf" path within a scene (same path shape scene.dump emits).</summary>
        public static GameObject? FindByPath(string path, Scene scene)
        {
            if (string.IsNullOrEmpty(path) || !scene.IsValid())
                return null;

            var parts = path.Split('/');

            GameObject? root = null;
            foreach (var r in scene.GetRootGameObjects())
            {
                if (r.name == parts[0])
                {
                    root = r;
                    break;
                }
            }
            if (root == null)
                return null;

            if (parts.Length == 1)
                return root;

            var rest = string.Join("/", parts.Skip(1));
            var t = root.transform.Find(rest);
            return t != null ? t.gameObject : null;
        }

        /// <summary>Gets the i-th component of the given type name on a GameObject (matches by simple type name).</summary>
        public static Component? GetComponent(GameObject go, string typeName, int index)
        {
            var matches = go.GetComponents<Component>()
                .Where(c => c != null && c.GetType().Name == typeName)
                .ToList();
            if (index < 0 || index >= matches.Count)
                return null;
            return matches[index];
        }
    }
}

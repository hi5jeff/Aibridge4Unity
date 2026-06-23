#nullable enable
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AIBridge.Editor.Core
{
    /// <summary>Resolves scene GameObjects by hierarchy path and components by type name. Shared by handlers.</summary>
    public static class SceneLookup
    {
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

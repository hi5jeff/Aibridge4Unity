#nullable enable
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AIBridge.Editor.Core
{
    /// <summary>An asset selected in the Project window (sprite, prefab, material, script, folder, …).</summary>
    [System.Serializable]
    public class AssetInfo
    {
        public string name = "";
        public string assetPath = "";
        public string type = "";
        public string guid = "";
    }

    /// <summary>Describes Project-window asset selections, so "this sprite / prefab / material" resolves too.</summary>
    public static class AssetDescriber
    {
        public static AssetInfo Describe(Object asset)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            return new AssetInfo
            {
                name = asset != null ? asset.name : "",
                assetPath = path,
                type = asset != null ? asset.GetType().Name : "",
                guid = string.IsNullOrEmpty(path) ? "" : AssetDatabase.AssetPathToGUID(path)
            };
        }

        /// <summary>Assets currently selected in the Project window (scene objects are excluded — they have no asset path).</summary>
        public static AssetInfo[] CurrentSelection()
        {
            var list = new List<AssetInfo>();
            foreach (var o in Selection.objects)
            {
                if (o == null) continue;
                if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(o)))
                    list.Add(Describe(o));
            }
            return list.ToArray();
        }
    }
}

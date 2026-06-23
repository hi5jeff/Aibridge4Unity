#nullable enable
using System.Collections.Generic;
using AIBridge.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace AIBridge.Editor.Handlers
{
    /// <summary>Finds project assets by Unity search filter — so the AI can discover available art,
    /// prefabs, materials, etc. before using them.</summary>
    public class AssetFindHandler : ICommandHandler
    {
        public string Command => "asset.find";

        [System.Serializable]
        class Request
        {
            public string filter = "";   // Unity search syntax, e.g. "t:Sprite hero", "t:Prefab", "t:Material"
            public string folder = "Assets";
            public int max = 100;
        }

        [System.Serializable] class Asset { public string path = ""; public string name = ""; public string type = ""; public string guid = ""; }

        [System.Serializable]
        class Result { public int count; public Asset[] assets = System.Array.Empty<Asset>(); }

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();
            var folders = new[] { string.IsNullOrEmpty(req.folder) ? "Assets" : req.folder };

            var guids = AssetDatabase.FindAssets(req.filter ?? "", folders);
            var max = req.max < 1 ? 100 : req.max;

            var list = new List<Asset>();
            foreach (var guid in guids)
            {
                if (list.Count >= max) break;
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var type = AssetDatabase.GetMainAssetTypeAtPath(path);
                list.Add(new Asset
                {
                    path = path,
                    name = System.IO.Path.GetFileNameWithoutExtension(path),
                    type = type != null ? type.Name : "",
                    guid = guid
                });
            }

            return CommandResult.Success(JsonUtility.ToJson(new Result { count = list.Count, assets = list.ToArray() }));
        }
    }
}

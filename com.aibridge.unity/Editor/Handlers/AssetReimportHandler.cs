#nullable enable
using AIBridge.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace AIBridge.Editor.Handlers
{
    /// <summary>Force-reimports an asset or folder (recursive) — e.g. to make an importer/postprocessor
    /// (like spine-unity) regenerate its derived assets for files imported before the plugin was present.</summary>
    public class AssetReimportHandler : ICommandHandler
    {
        public string Command => "asset.reimport";

        [System.Serializable] class Request { public string path = ""; }
        [System.Serializable] class Result { public string path = ""; public string note = ""; }

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();
            if (string.IsNullOrEmpty(req.path))
                return CommandResult.Failure("Required: path.");

            AssetDatabase.ImportAsset(req.path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);
            AssetDatabase.Refresh();

            return CommandResult.Success(JsonUtility.ToJson(new Result
            {
                path = req.path,
                note = "Force-reimported (recursive)."
            }));
        }
    }
}

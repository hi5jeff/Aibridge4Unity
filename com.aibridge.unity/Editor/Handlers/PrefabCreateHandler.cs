#nullable enable
using System.IO;
using AIBridge.Editor.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AIBridge.Editor.Handlers
{
    /// <summary>Saves a scene GameObject as a reusable prefab asset (and connects the scene object to it).</summary>
    public class PrefabCreateHandler : ICommandHandler
    {
        public string Command => "prefab.create";

        [System.Serializable]
        class Request
        {
            public string path = "";        // scene GameObject to turn into a prefab
            public string assetPath = "";   // where to save, e.g. "Assets/Prefabs/Enemy.prefab"
        }

        [System.Serializable] class Result { public string assetPath = ""; public string name = ""; }

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();
            if (string.IsNullOrEmpty(req.path) || string.IsNullOrEmpty(req.assetPath))
                return CommandResult.Failure("Required: path, assetPath.");

            var assetPath = req.assetPath.Replace('\\', '/');
            if (!assetPath.StartsWith("Assets/"))
                return CommandResult.Failure("assetPath must be under 'Assets/'.");
            if (!assetPath.EndsWith(".prefab"))
                assetPath += ".prefab";

            var go = SceneLookup.FindByPath(req.path, SceneManager.GetActiveScene());
            if (go == null)
                return CommandResult.Failure($"GameObject not found: '{req.path}'.");

            EnsureFolder(Path.GetDirectoryName(assetPath)!.Replace('\\', '/'));

            var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(go, assetPath, InteractionMode.AutomatedAction, out var success);
            if (!success || prefab == null)
                return CommandResult.Failure($"Failed to save prefab at '{assetPath}'.");

            AssetDatabase.SaveAssets();
            return CommandResult.Success(JsonUtility.ToJson(new Result { assetPath = assetPath, name = prefab.name }));
        }

        static void EnsureFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder))
                return;
            var parent = Path.GetDirectoryName(folder)!.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}

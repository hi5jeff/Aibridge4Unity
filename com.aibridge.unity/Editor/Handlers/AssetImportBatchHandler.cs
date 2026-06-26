#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using AIBridge.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace AIBridge.Editor.Handlers
{
    /// <summary>
    /// asset.importBatch — copy a folder of files into the project's <c>Assets/</c> and apply importer
    /// settings in one shot. Closes the "delivered ≠ imported" gap (Bridge4Unity_개선제안 P0-2): art handed
    /// over in an external folder (e.g. shared/) gets copied in + set to Sprite without manual dragging.
    /// Supports <c>dryRun</c> (lists what would be imported, copies nothing).
    ///
    /// Request: { "srcDir":"D:/.../cutouts" (abs, or relative to the project root),
    ///            "destDir":"Assets/Game/Resources/art/cutouts",   // must be under Assets/
    ///            "pattern":"*.png", "textureType":"sprite"|"default", "spriteMode":"single"|"multiple",
    ///            "pixelsPerUnit":100, "overwrite":true, "dryRun":false }
    /// </summary>
    public class AssetImportBatchHandler : ICommandHandler
    {
        public string Command => "asset.importBatch";

        [Serializable] class Request
        {
            public string srcDir = "";
            public string destDir = "";
            public string pattern = "*.png";
            public string textureType = "sprite";
            public string spriteMode = "single";
            public float pixelsPerUnit = 100f;
            public bool overwrite = true;
            public bool dryRun = false;
        }
        [Serializable] class Result
        {
            public bool dryRun;
            public int imported;
            public string[] files = Array.Empty<string>();
            public string[] skipped = Array.Empty<string>();
        }

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();
            if (string.IsNullOrEmpty(req.srcDir)) return CommandResult.Failure("Required: srcDir.");
            if (string.IsNullOrEmpty(req.destDir)) return CommandResult.Failure("Required: destDir (under Assets/).");
            var destRel = req.destDir.Replace("\\", "/").TrimEnd('/');
            if (!destRel.StartsWith("Assets/")) return CommandResult.Failure("destDir must be under Assets/.");

            string projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            string src = Path.IsPathRooted(req.srcDir) ? req.srcDir : Path.GetFullPath(Path.Combine(projectRoot, req.srcDir));
            if (!Directory.Exists(src)) return CommandResult.Failure($"srcDir not found: {src}");

            string destAbs = Path.Combine(projectRoot, destRel);
            var imported = new List<string>();
            var skipped = new List<string>();
            string[] files;
            try { files = Directory.GetFiles(src, string.IsNullOrEmpty(req.pattern) ? "*.png" : req.pattern); }
            catch (Exception e) { return CommandResult.Failure($"enumerate failed: {e.Message}"); }

            if (!req.dryRun) Directory.CreateDirectory(destAbs);
            foreach (var f in files)
            {
                string name = Path.GetFileName(f);
                string destAssetPath = destRel + "/" + name;
                if (req.dryRun) { imported.Add(destAssetPath); continue; }
                string destAbsFile = Path.Combine(destAbs, name);
                if (File.Exists(destAbsFile) && !req.overwrite) { skipped.Add(name + ":exists"); continue; }
                File.Copy(f, destAbsFile, true);
                AssetDatabase.ImportAsset(destAssetPath, ImportAssetOptions.ForceUpdate);
                if (req.textureType == "sprite" && AssetImporter.GetAtPath(destAssetPath) is TextureImporter ti)
                {
                    ti.textureType = TextureImporterType.Sprite;
                    ti.spriteImportMode = req.spriteMode == "multiple" ? SpriteImportMode.Multiple : SpriteImportMode.Single;
                    ti.spritePixelsPerUnit = req.pixelsPerUnit;
                    ti.SaveAndReimport();
                }
                imported.Add(destAssetPath);
            }
            if (!req.dryRun) AssetDatabase.Refresh();

            var res = new Result { dryRun = req.dryRun, imported = imported.Count, files = imported.ToArray(), skipped = skipped.ToArray() };
            return CommandResult.Success(JsonUtility.ToJson(res));
        }
    }
}

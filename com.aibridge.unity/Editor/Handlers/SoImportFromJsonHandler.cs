#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using AIBridge.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace AIBridge.Editor.Handlers
{
    /// <summary>
    /// so.importFromJson — create or update a ScriptableObject asset from JSON (Bridge4Unity_개선제안 P1-1).
    /// Resolves the SO type by simple or full name across all assemblies, instantiates it (or loads the
    /// existing asset when update=true), populates fields via JsonUtility.FromJsonOverwrite, and writes the
    /// `.asset`. Lets an agent generate data/config assets (tables, tuning, level defs) straight from JSON
    /// without hand-editing the Inspector — the data→asset counterpart to asset.importBatch (files→assets).
    ///
    /// JsonUtility rules apply: only public fields / [SerializeField] are populated; names must match.
    /// Request: { "soType":"MyConfig" | "Game.Data.MyConfig", "outAsset":"Assets/.../X.asset",
    ///            "json":"{...}" (inline)  OR  "jsonPath":"Assets/.../x.json",
    ///            "update":false (true = overwrite fields on the existing asset in place, keeping its GUID) }
    /// </summary>
    public class SoImportFromJsonHandler : ICommandHandler
    {
        public string Command => "so.importFromJson";

        [Serializable] class Request { public string soType = ""; public string outAsset = ""; public string json = ""; public string jsonPath = ""; public bool update; }
        [Serializable] class Result { public string outAsset = ""; public string soType = ""; public bool created; public bool updatedInPlace; }

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();
            if (string.IsNullOrEmpty(req.soType)) return CommandResult.Failure("Required: soType.");
            if (string.IsNullOrEmpty(req.outAsset) || !req.outAsset.Replace("\\", "/").StartsWith("Assets/"))
                return CommandResult.Failure("Required: outAsset under Assets/.");
            if (!req.outAsset.EndsWith(".asset")) req.outAsset += ".asset";

            var type = ResolveScriptableType(req.soType);
            if (type == null) return CommandResult.Failure($"ScriptableObject type not found: '{req.soType}'.");

            // Source JSON: inline wins, else read jsonPath (abs or project-relative).
            string data = req.json;
            if (string.IsNullOrEmpty(data) && !string.IsNullOrEmpty(req.jsonPath))
            {
                var projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
                var jp = Path.IsPathRooted(req.jsonPath) ? req.jsonPath : Path.GetFullPath(Path.Combine(projectRoot, req.jsonPath));
                if (!File.Exists(jp)) return CommandResult.Failure($"jsonPath not found: {jp}");
                data = File.ReadAllText(jp);
            }
            if (string.IsNullOrEmpty(data)) return CommandResult.Failure("Required: json or jsonPath.");

            var existing = AssetDatabase.LoadAssetAtPath<ScriptableObject>(req.outAsset);

            // Update-in-place: keep the asset's GUID so existing references survive.
            if (existing != null && req.update && type.IsInstanceOfType(existing))
            {
                try { JsonUtility.FromJsonOverwrite(data, existing); }
                catch (Exception e) { return CommandResult.Failure($"json overwrite failed: {e.Message}"); }
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssets();
                return CommandResult.Success(JsonUtility.ToJson(new Result { outAsset = req.outAsset, soType = type.FullName, created = false, updatedInPlace = true }));
            }

            // Create (replacing any existing asset at the path for a deterministic outcome).
            var so = ScriptableObject.CreateInstance(type);
            try { JsonUtility.FromJsonOverwrite(data, so); }
            catch (Exception e) { UnityEngine.Object.DestroyImmediate(so); return CommandResult.Failure($"json overwrite failed: {e.Message}"); }

            if (existing != null) AssetDatabase.DeleteAsset(req.outAsset);
            var projRoot = Directory.GetParent(Application.dataPath)!.FullName;
            var dir = Path.GetDirectoryName(req.outAsset);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(Path.Combine(projRoot, dir));
            AssetDatabase.CreateAsset(so, req.outAsset);
            AssetDatabase.SaveAssets();

            return CommandResult.Success(JsonUtility.ToJson(new Result { outAsset = req.outAsset, soType = type.FullName, created = true, updatedInPlace = false }));
        }

        // Resolve a concrete ScriptableObject subclass from a simple ("MyConfig") or full name.
        static Type? ResolveScriptableType(string name)
        {
            Type? byFull = null, byName = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray()!; }
                foreach (var t in types)
                {
                    if (t == null || t.IsAbstract || !typeof(ScriptableObject).IsAssignableFrom(t)) continue;
                    if (t.FullName == name) return t;
                    if (t.Name == name && byName == null) byName = t;
                }
            }
            return byFull ?? byName;
        }
    }
}

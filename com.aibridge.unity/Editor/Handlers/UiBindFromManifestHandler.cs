#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using AIBridge.Editor.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AIBridge.Editor.Handlers
{
    /// <summary>
    /// ui.bindFromManifest — auto-place static-text image sprites into a prefab's text slots.
    /// Walks every Graphic that exposes a writable string `text` (TMP_Text or UI.Text), looks its
    /// current string up in a manifest (text→file), and for each match adds a child "~img" Image with
    /// the mapped sprite (height/width-stretched, preserveAspect) and blanks the glyphs. Non-matching
    /// (dynamic) slots are left as TMP. Non-destructive: only touched slots gain a "~img"; everything
    /// else (the user's tuned layout) is preserved. Supports dryRun (returns the would-bind diff) and
    /// per-slot `overrides` for slots whose prefab text differs from the manifest string.
    ///
    /// Request: { "prefabPath":"Assets/.../X.prefab",
    ///            "manifestResource":"art/text/uitext_manifest",   // Resources path to the manifest TextAsset
    ///            "spriteRoot":"art/text/",                         // Resources prefix for manifest `file`
    ///            "dryRun":false,
    ///            "overrides":[ {"slot":"Title","text":"다음 어디로 갈까?"} ] }
    /// </summary>
    public class UiBindFromManifestHandler : ICommandHandler
    {
        public string Command => "ui.bindFromManifest";

        [Serializable] class Override { public string slot = ""; public string text = ""; }
        [Serializable] class Request
        {
            public string prefabPath = "";
            public string manifestResource = "art/text/uitext_manifest";
            public string spriteRoot = "art/text/";
            public bool dryRun = false;
            public Override[] overrides = Array.Empty<Override>();
        }
        [Serializable] class MItem { public string text = ""; public string file = ""; public string category = ""; }
        [Serializable] class Manifest { public MItem[] items = Array.Empty<MItem>(); }
        [Serializable] class Result
        {
            public string prefabPath = "";
            public bool dryRun;
            public int bound;
            public string[] slots = Array.Empty<string>();
            public string[] skipped = Array.Empty<string>();
        }

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();
            if (string.IsNullOrEmpty(req.prefabPath))
                return CommandResult.Failure("Required: prefabPath.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(req.prefabPath) == null)
                return CommandResult.Failure($"Prefab not found: '{req.prefabPath}'.");

            var ta = Resources.Load<TextAsset>(req.manifestResource);
            if (ta == null)
                return CommandResult.Failure($"Manifest not found in Resources: '{req.manifestResource}'.");
            var mf = JsonUtility.FromJson<Manifest>(ta.text) ?? new Manifest();
            var map = new Dictionary<string, string>();
            foreach (var it in mf.items)
                if (!string.IsNullOrEmpty(it.text) && !string.IsNullOrEmpty(it.file))
                    map[it.text.Trim()] = it.file;

            var ov = new Dictionary<string, string>();
            foreach (var o in req.overrides)
                if (!string.IsNullOrEmpty(o.slot)) ov[o.slot] = o.text;

            var bound = new List<string>();
            var skipped = new List<string>();
            var root = PrefabUtility.LoadPrefabContents(req.prefabPath);
            try
            {
                foreach (var g in root.GetComponentsInChildren<Graphic>(true))
                {
                    if (g == null) continue;
                    var prop = g.GetType().GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
                    if (prop == null || prop.PropertyType != typeof(string) || !prop.CanRead || !prop.CanWrite) continue;

                    var slot = g.gameObject;
                    if (slot.name == "~img") continue;
                    string current = (string)(prop.GetValue(g) ?? "");
                    string lookup = ov.TryGetValue(slot.name, out var oText) ? oText : current;
                    if (string.IsNullOrWhiteSpace(lookup)) continue;
                    if (!map.TryGetValue(lookup.Trim(), out var file)) { skipped.Add($"{slot.name}:'{lookup}'"); continue; }
                    var sprite = Resources.Load<Sprite>(req.spriteRoot + file.Replace(".png", ""));
                    if (sprite == null) { skipped.Add($"{slot.name}:sprite-null({file})"); continue; }

                    if (!req.dryRun)
                    {
                        GetOrAddImg(slot.transform).sprite = sprite;
                        prop.SetValue(g, "");   // hide glyphs; the image carries the text
                    }
                    bound.Add($"{slot.name} ← {file}");
                }
                if (!req.dryRun && bound.Count > 0)
                    PrefabUtility.SaveAsPrefabAsset(root, req.prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }

            var res = new Result
            {
                prefabPath = req.prefabPath, dryRun = req.dryRun, bound = bound.Count,
                slots = bound.ToArray(), skipped = skipped.ToArray()
            };
            return CommandResult.Success(JsonUtility.ToJson(res));
        }

        static Image GetOrAddImg(Transform slot)
        {
            var t = slot.Find("~img");
            if (t != null) return t.GetComponent<Image>();
            var go = new GameObject("~img", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(slot, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.preserveAspect = true; img.raycastTarget = false;
            return img;
        }
    }
}

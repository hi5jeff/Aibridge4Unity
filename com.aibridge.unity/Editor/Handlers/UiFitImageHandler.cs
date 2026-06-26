#nullable enable
using System;
using AIBridge.Editor.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AIBridge.Editor.Handlers
{
    /// <summary>
    /// ui.fitImage — size a UI Image's RectTransform to its sprite's native aspect, fitted to the screen
    /// (canvas) by a mode, centered — so a background/portrait isn't squashed by a full-stretch rect.
    /// Bundles the manual "compute sizeDelta from native size × scale" math into one call.
    ///
    /// Modes (scale = how the sprite maps onto the target W×H, keeping aspect):
    ///   height  — height fills the target; width follows (may overflow → crop sides). The usual "背景撑满高度".
    ///   width   — width fills; height follows.
    ///   cover   — fills the whole target (max scale); overflow on one axis is cropped.
    ///   contain — fits inside the target (min scale); letterboxed.
    ///
    /// Target W×H = explicit targetWidth/targetHeight, else the node's Canvas reference resolution, else 1080×1920.
    /// Works on a prefab asset ({prefabPath, node}) OR a live/selected object ({path}, incl. "@selection" —
    /// the only way to reach a node inside an open Prefab Mode stage).
    ///
    /// Request: { "prefabPath":"Assets/.../X.prefab", "node":"BgImage",  // asset path
    ///            "path":"@selection" | "Root/BgImage",                  // OR a live object
    ///            "mode":"height", "targetWidth":0, "targetHeight":0, "center":true, "preserveAspect":true }
    /// </summary>
    public class UiFitImageHandler : ICommandHandler
    {
        public string Command => "ui.fitImage";

        [Serializable] class Request
        {
            public string prefabPath = ""; public string node = ""; public string path = "";
            public string mode = "height"; public float targetWidth = 0f; public float targetHeight = 0f;
            public bool center = true; public bool preserveAspect = true;
        }
        [Serializable] class Result { public string target = ""; public string mode = ""; public float nativeW, nativeH, targetW, targetH, sizeW, sizeH; }

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();

            if (!string.IsNullOrEmpty(req.prefabPath))
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(req.prefabPath) == null)
                    return CommandResult.Failure($"Prefab not found: '{req.prefabPath}'.");
                if (string.IsNullOrEmpty(req.node)) return CommandResult.Failure("Required: node (with prefabPath).");
                var root = PrefabUtility.LoadPrefabContents(req.prefabPath);
                try
                {
                    var go = SceneLookup.FindByPath(req.node, root.scene) ?? FindDeep(root.transform, req.node);
                    if (go == null) return CommandResult.Failure($"node not found in prefab: '{req.node}'");
                    var res = Fit(go, req);
                    if (res == null) return CommandResult.Failure("node has no Image with a sprite.");
                    PrefabUtility.SaveAsPrefabAsset(root, req.prefabPath);
                    AssetDatabase.SaveAssets();
                    return CommandResult.Success(JsonUtility.ToJson(res));
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }

            // Live / selected object.
            if (string.IsNullOrEmpty(req.path)) return CommandResult.Failure("Required: path (or prefabPath+node).");
            var target = SceneLookup.FindByPathOrSelection(req.path);
            if (target == null) return CommandResult.Failure($"GameObject not found: '{req.path}'. (Use \"@selection\" for the current selection / Prefab Mode.)");
            var r = Fit(target, req);
            if (r == null) return CommandResult.Failure("object has no Image with a sprite.");
            EditorUtility.SetDirty(target);
            return CommandResult.Success(JsonUtility.ToJson(r));
        }

        static Result? Fit(GameObject go, Request req)
        {
            var img = go.GetComponent<Image>();
            if (img == null || img.sprite == null) return null;
            var rt = go.transform as RectTransform;
            if (rt == null) return null;

            float nW = img.sprite.rect.width, nH = img.sprite.rect.height;
            if (nW <= 0 || nH <= 0) return null;

            // Target: explicit, else Canvas reference resolution, else 1080×1920.
            float tW = req.targetWidth, tH = req.targetHeight;
            if (tW <= 0 || tH <= 0)
            {
                var scaler = go.GetComponentInParent<CanvasScaler>();
                if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
                { tW = scaler.referenceResolution.x; tH = scaler.referenceResolution.y; }
                else { tW = 1080f; tH = 1920f; }
            }

            float s = (req.mode ?? "height").Trim().ToLowerInvariant() switch
            {
                "width" => tW / nW,
                "cover" => Mathf.Max(tW / nW, tH / nH),
                "contain" => Mathf.Min(tW / nW, tH / nH),
                _ => tH / nH,   // height
            };

            if (req.center)
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
            }
            rt.sizeDelta = new Vector2(nW * s, nH * s);
            img.preserveAspect = req.preserveAspect;

            return new Result { target = go.name, mode = req.mode, nativeW = nW, nativeH = nH, targetW = tW, targetH = tH, sizeW = rt.sizeDelta.x, sizeH = rt.sizeDelta.y };
        }

        static GameObject? FindDeep(Transform t, string name)
        {
            if (t.name == name) return t.gameObject;
            for (int i = 0; i < t.childCount; i++) { var r = FindDeep(t.GetChild(i), name); if (r) return r; }
            return null;
        }
    }
}

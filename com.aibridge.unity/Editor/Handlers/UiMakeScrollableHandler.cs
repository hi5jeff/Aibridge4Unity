#nullable enable
using System;
using System.IO;
using AIBridge.Editor.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AIBridge.Editor.Handlers
{
    /// <summary>
    /// ui.makeScrollable — wrap an existing layout container (a node with a GridLayoutGroup / Vertical or
    /// HorizontalLayoutGroup) in a proper UGUI ScrollRect, IN PLACE, without rebuilding the prefab. Solves the
    /// "the list shows everything instead of scrolling inside my set width/height" gap: a spec-built container
    /// just overflows; this gives it a fixed viewport (clipped by RectMask2D) + size-fitted scrolling content.
    ///
    /// The node's CURRENT RectTransform (anchor/pos/size — your Inspector tuning) becomes the VIEWPORT size,
    /// so your width/height is respected. The original node stays the Content (same name, so screen scripts
    /// that find it by name and clone cards into it keep working) and gets a ContentSizeFitter so it grows
    /// with its children and scrolls.
    ///
    /// Request: { "prefabPath":"Assets/.../Dex.prefab", "node":"DexGrid",
    ///            "vertical":true, "horizontal":false,
    ///            "width":0, "height":0   // optional override; 0 = keep the node's current size }
    /// </summary>
    public class UiMakeScrollableHandler : ICommandHandler
    {
        public string Command => "ui.makeScrollable";

        [Serializable] class Request { public string prefabPath = ""; public string node = ""; public bool vertical = true; public bool horizontal = false; public float width = 0f; public float height = 0f; }
        [Serializable] class Result { public string prefabPath = ""; public string node = ""; public string scrollRoot = ""; public string viewport = ""; public bool ok = true; }

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();
            if (string.IsNullOrEmpty(req.prefabPath)) return CommandResult.Failure("Required: prefabPath.");
            if (string.IsNullOrEmpty(req.node)) return CommandResult.Failure("Required: node (the container to make scrollable).");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(req.prefabPath) == null) return CommandResult.Failure($"Prefab not found: '{req.prefabPath}'.");

            var root = PrefabUtility.LoadPrefabContents(req.prefabPath);
            try
            {
                var content = FindChild(root.transform, req.node);
                if (content == null) return CommandResult.Failure($"node not found: '{req.node}'");
                var contentRt = content as RectTransform;
                if (contentRt == null) return CommandResult.Failure($"'{req.node}' is not a UI (RectTransform) object.");
                if (content.GetComponentInParent<ScrollRect>() != null)
                    return CommandResult.Failure($"'{req.node}' is already inside a ScrollRect.");

                var parent = content.parent;
                int sib = content.GetSiblingIndex();

                // Snapshot the node's current rect — this defines the visible viewport (respects your tuning).
                Vector2 aMin = contentRt.anchorMin, aMax = contentRt.anchorMax, piv = contentRt.pivot;
                Vector2 aPos = contentRt.anchoredPosition, size = contentRt.sizeDelta;
                if (req.width > 0f) size.x = req.width;
                if (req.height > 0f) size.y = req.height;

                // 1) ScrollRoot — takes the node's place + rect; carries the ScrollRect.
                var scrollRoot = new GameObject(req.node + "Scroll", typeof(RectTransform));
                var srRt = (RectTransform)scrollRoot.transform;
                srRt.SetParent(parent, false);
                srRt.SetSiblingIndex(sib);
                srRt.anchorMin = aMin; srRt.anchorMax = aMax; srRt.pivot = piv;
                srRt.anchoredPosition = aPos; srRt.sizeDelta = size;

                // 2) Viewport — fills ScrollRoot, clips with RectMask2D (no graphic/material needed).
                var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
                var vpRt = (RectTransform)viewport.transform;
                vpRt.SetParent(srRt, false);
                vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one; vpRt.pivot = new Vector2(0f, 1f);
                vpRt.offsetMin = Vector2.zero; vpRt.offsetMax = Vector2.zero;

                // 3) Content = the original node, reparented under Viewport, anchored top, size-fitted to grow.
                contentRt.SetParent(vpRt, false);
                contentRt.anchorMin = new Vector2(0f, 1f);
                contentRt.anchorMax = new Vector2(1f, 1f);
                contentRt.pivot = new Vector2(0.5f, 1f);
                contentRt.anchoredPosition = Vector2.zero;
                contentRt.sizeDelta = new Vector2(0f, contentRt.sizeDelta.y);   // width follows viewport; height from fitter
                var fitter = content.GetComponent<ContentSizeFitter>();
                if (fitter == null) fitter = content.gameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = req.vertical ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
                fitter.horizontalFit = req.horizontal ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;

                // 4) ScrollRect on the root, wired to content + viewport.
                var sr = scrollRoot.AddComponent<ScrollRect>();
                sr.content = contentRt;
                sr.viewport = vpRt;
                sr.horizontal = req.horizontal;
                sr.vertical = req.vertical;
                sr.movementType = ScrollRect.MovementType.Elastic;
                sr.elasticity = 0.1f;
                sr.scrollSensitivity = 30f;

                PrefabUtility.SaveAsPrefabAsset(root, req.prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }

            AssetDatabase.SaveAssets();
            return CommandResult.Success(JsonUtility.ToJson(new Result { prefabPath = req.prefabPath, node = req.node, scrollRoot = req.node + "Scroll", viewport = "Viewport" }));
        }

        static Transform? FindChild(Transform root, string path)
        {
            if (string.IsNullOrEmpty(path)) return root;
            var t = root.Find(path);
            if (t != null) return t;
            var leaf = path.Contains("/") ? path.Substring(path.LastIndexOf('/') + 1) : path;
            foreach (var c in root.GetComponentsInChildren<Transform>(true))
                if (c.name == leaf) return c;
            return null;
        }
    }
}

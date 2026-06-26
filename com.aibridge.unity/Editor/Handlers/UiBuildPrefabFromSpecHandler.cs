#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using AIBridge.Editor.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AIBridge.Editor.Handlers
{
    /// <summary>
    /// ui.buildPrefabFromSpec — a `.ui.json` node tree → a UGUI prefab (Bridge4Unity_개선제안 P0-3).
    /// Builds a Canvas root + each node (Image/Text/Button/Container) with RectTransform anchors, sprite,
    /// font, GridLayout, 9-slice. Static labels stay TMP placeholders (imagize later with ui.bindFromManifest);
    /// the point is the engine gets the spec WITHOUT hand-coding coordinates. TMP is created via reflection
    /// so the package needs no TMPro asmdef reference. After build, tune in place with prefab.modify.
    ///
    /// Request: { "specPath":"D:/.../dex.ui.json" (abs or project-relative),
    ///            "outPrefab":"Assets/Game/Resources/ui/Dex.prefab",
    ///            "spriteRoots":["art/ui/","art/ui/components/","art/text/","art/text/ui/"],
    ///            "fontResource":"fonts/KoreanSDF" }
    /// </summary>
    public class UiBuildPrefabFromSpecHandler : ICommandHandler
    {
        public string Command => "ui.buildPrefabFromSpec";

        [Serializable] class SpecRect { public string anchor = "center"; public float x, y, w, h; }
        [Serializable] class SpecFont { public int size = 32; public string weight = ""; public string color = ""; public string align = ""; }
        [Serializable] class SpecNode
        {
            public string name = ""; public string type = ""; public string parent = "Root";
            public SpecRect? rect; public string sprite = ""; public bool nineSlice;
            public SpecFont? font; public string placeholder = ""; public string bind = "";
            public string layout = ""; public string runtime = ""; public string note = "";
        }
        [Serializable] class SpecCanvas { public int w = 1080; public int h = 1920; }
        [Serializable] class Spec { public string screen = ""; public SpecCanvas? canvas; public SpecNode[] nodes = Array.Empty<SpecNode>(); }
        [Serializable] class Request
        {
            public string specPath = ""; public string outPrefab = "";
            public string[] spriteRoots = { "art/ui/", "art/ui/components/", "art/text/", "art/text/ui/" };
            public string fontResource = "fonts/KoreanSDF";
        }
        [Serializable] class Result { public string outPrefab = ""; public int nodes; public string[] built = Array.Empty<string>(); public string[] warnings = Array.Empty<string>(); }

        static readonly Type? TmpType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
        static readonly Type? TmpFontType = Type.GetType("TMPro.TMP_FontAsset, Unity.TextMeshPro");
        static readonly Type? TmpAlignType = Type.GetType("TMPro.TextAlignmentOptions, Unity.TextMeshPro");

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();
            if (string.IsNullOrEmpty(req.specPath)) return CommandResult.Failure("Required: specPath.");
            if (string.IsNullOrEmpty(req.outPrefab) || !req.outPrefab.Replace("\\", "/").StartsWith("Assets/"))
                return CommandResult.Failure("Required: outPrefab under Assets/.");

            string projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            string spec = Path.IsPathRooted(req.specPath) ? req.specPath : Path.GetFullPath(Path.Combine(projectRoot, req.specPath));
            if (!File.Exists(spec)) return CommandResult.Failure($"specPath not found: {spec}");
            Spec s;
            try { s = JsonUtility.FromJson<Spec>(File.ReadAllText(spec)) ?? new Spec(); }
            catch (Exception e) { return CommandResult.Failure($"spec parse failed: {e.Message}"); }

            var warnings = new List<string>();
            var built = new List<string>();
            var cw = s.canvas?.w ?? 1080; var ch = s.canvas?.h ?? 1920;

            var root = new GameObject(string.IsNullOrEmpty(s.screen) ? "Screen" : s.screen, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(cw, ch);
            scaler.matchWidthOrHeight = 0.5f;

            var map = new Dictionary<string, Transform> { ["Root"] = root.transform };
            try
            {
                foreach (var n in s.nodes)
                {
                    if (string.IsNullOrEmpty(n.name)) continue;
                    var parent = (n.parent != null && map.TryGetValue(n.parent, out var p)) ? p : root.transform;
                    var go = new GameObject(n.name, typeof(RectTransform));
                    var rt = (RectTransform)go.transform;
                    rt.SetParent(parent, false);
                    ApplyRect(rt, n.rect);

                    switch ((n.type ?? "").ToLowerInvariant())
                    {
                        case "image": AddImage(go, n, req, warnings); break;
                        case "button": AddImage(go, n, req, warnings); go.AddComponent<Button>(); break;
                        case "text": AddText(go, n, req, warnings); break;
                        case "container": if (!string.IsNullOrEmpty(n.layout)) AddLayout(go, n.layout); break;
                        default: AddImage(go, n, req, warnings); break;
                    }
                    map[n.name] = rt;
                    built.Add($"{n.name}:{n.type}");
                }

                var dir = Path.GetDirectoryName(req.outPrefab);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(Path.Combine(projectRoot, dir));
                PrefabUtility.SaveAsPrefabAsset(root, req.outPrefab);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }

            var res = new Result { outPrefab = req.outPrefab, nodes = built.Count, built = built.ToArray(), warnings = warnings.ToArray() };
            return CommandResult.Success(JsonUtility.ToJson(res));
        }

        static void ApplyRect(RectTransform rt, SpecRect? r)
        {
            r ??= new SpecRect();
            // anchor token "vert-horiz" → min/max/pivot
            var a = (r.anchor ?? "center").ToLowerInvariant();
            float minX = 0.5f, maxX = 0.5f, minY = 0.5f, maxY = 0.5f, pX = 0.5f, pY = 0.5f;
            if (a.Contains("top")) { minY = maxY = pY = 1f; }
            else if (a.Contains("bottom")) { minY = maxY = pY = 0f; }
            if (a.Contains("left")) { minX = maxX = pX = 0f; }
            else if (a.Contains("right")) { minX = maxX = pX = 1f; }
            bool stretchX = a.Contains("stretch") && !a.Contains("left") && !a.Contains("right");
            bool fullStretch = a == "stretch";
            if (stretchX || fullStretch) { minX = 0f; maxX = 1f; pX = 0.5f; }
            if (fullStretch) { minY = 0f; maxY = 1f; pY = 0.5f; }
            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.pivot = new Vector2(pX, pY);
            rt.anchoredPosition = new Vector2(r.x, r.y);
            rt.sizeDelta = new Vector2(r.w, r.h);   // w=0 with stretched X → full width
        }

        void AddImage(GameObject go, SpecNode n, Request req, List<string> warn)
        {
            var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            if (!string.IsNullOrEmpty(n.sprite))
            {
                var sp = ResolveSprite(n.sprite, req.spriteRoots);
                if (sp != null) { img.sprite = sp; if (n.nineSlice) img.type = Image.Type.Sliced; }
                else { warn.Add($"{n.name}:sprite-unresolved({n.sprite})"); img.color = new Color(1, 1, 1, 0.15f); }
            }
            else img.color = new Color(1, 1, 1, 0.08f);
        }

        void AddText(GameObject go, SpecNode n, Request req, List<string> warn)
        {
            if (TmpType == null) { warn.Add($"{n.name}:TMP-missing"); return; }
            var tmp = go.AddComponent(TmpType);
            SetProp(tmp, "text", n.placeholder ?? "");
            var f = n.font ?? new SpecFont();
            SetProp(tmp, "fontSize", (float)f.size);
            if (TryParseColor(f.color, out var col)) SetProp(tmp, "color", col);
            if (TmpFontType != null)
            {
                var fa = Resources.Load(req.fontResource, TmpFontType);
                if (fa != null) SetProp(tmp, "font", fa); else warn.Add($"{n.name}:font-missing({req.fontResource})");
            }
            if (TmpAlignType != null && !string.IsNullOrEmpty(f.align))
            {
                string an = f.align.ToLowerInvariant() switch { "center" => "Center", "left" => "Left", "right" => "Right", _ => "" };
                if (an != "") { try { SetProp(tmp, "alignment", Enum.Parse(TmpAlignType, an)); } catch { } }
            }
        }

        static void AddLayout(GameObject go, string layout)
        {
            // "GridLayoutGroup cell=300x380 spacing=24 cols=3"
            if (layout.Contains("Grid"))
            {
                var g = go.AddComponent<GridLayoutGroup>();
                var cell = Regex.Match(layout, @"cell=(\d+)x(\d+)");
                if (cell.Success) g.cellSize = new Vector2(int.Parse(cell.Groups[1].Value), int.Parse(cell.Groups[2].Value));
                var sp = Regex.Match(layout, @"spacing=(\d+)");
                if (sp.Success) { var v = int.Parse(sp.Groups[1].Value); g.spacing = new Vector2(v, v); }
                var cols = Regex.Match(layout, @"cols=(\d+)");
                if (cols.Success) { g.constraint = GridLayoutGroup.Constraint.FixedColumnCount; g.constraintCount = int.Parse(cols.Groups[1].Value); }
            }
            else if (layout.Contains("Vertical")) go.AddComponent<VerticalLayoutGroup>();
            else if (layout.Contains("Horizontal")) go.AddComponent<HorizontalLayoutGroup>();
        }

        Sprite? ResolveSprite(string path, string[] roots)
        {
            string file = Path.GetFileNameWithoutExtension(path);
            string raw = path.Replace(".png", "");
            var sp = Resources.Load<Sprite>(raw);
            if (sp != null) return sp;
            foreach (var r in roots)
            {
                sp = Resources.Load<Sprite>(r.TrimEnd('/') + "/" + file);
                if (sp != null) return sp;
            }
            return null;
        }

        static void SetProp(object o, string name, object value)
        {
            var p = o.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (p != null && p.CanWrite) { try { p.SetValue(o, value); } catch { } }
        }

        static bool TryParseColor(string? s, out Color c)
        {
            c = Color.white;
            if (string.IsNullOrEmpty(s)) return false;
            if (s.StartsWith("#")) return ColorUtility.TryParseHtmlString(s, out c);
            var parts = s.Split(',');
            if (parts.Length >= 3 && float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var r)
                && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var g)
                && float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var b))
            { c = new Color(r, g, b, parts.Length >= 4 && float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var a) ? a : 1f); return true; }
            return false;
        }
    }
}

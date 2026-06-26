#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using AIBridge.Editor.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AIBridge.Editor.Handlers
{
    /// <summary>
    /// Non-destructive prefab editing. Loads the prefab's CONTENTS (every existing object plus the
    /// coordinates authored in the Inspector), applies only the listed ops to the named child
    /// objects, then saves back. Unlike a builder that rebuilds the whole hierarchy with
    /// SaveAsPrefabAsset on a fresh root, this preserves everything it does not touch — so a user's
    /// hand-tuned layout is never clobbered.
    ///
    /// Request: { "prefabPath":"Assets/.../X.prefab", "edits":[ { "path":"A/B/C", "ops":[ {property,value} ] } ] }
    /// Properties: active|name|addComponent|text|sprite|color|anchoredPosition|sizeDelta|anchorMin|anchorMax|pivot|localScale.
    ///   addComponent — value = a component type (simple or full name); idempotent (skips if already present).
    ///   text   — set via reflection on any component exposing a writable string `text` (TMP or UI.Text).
    ///   sprite — value = asset path to a Sprite; set on the object's Image.
    ///   color  — "r,g,b[,a]" (0–1) or "#RRGGBB(AA)"; set on the object's Graphic.
    /// path is a Transform path relative to the prefab root (root name optional); falls back to a
    /// recursive search by leaf name.
    /// An edit may also ADD children under the found node via "addChild":[ {name,type,sprite,text,color,
    /// preserveAspect, anchorMin,anchorMax,anchoredPosition,sizeDelta,pivot, fontSize,fontResource} ] —
    /// type is image|text|empty, defaulting to a full-stretch RectTransform. New children aren't in the
    /// pre-edit snapshots, so the non-destructive restore never touches them (same safety as duplicateAs).
    /// </summary>
    public class PrefabModifyHandler : ICommandHandler
    {
        public string Command => "prefab.modify";

        [System.Serializable] class Op { public string property = ""; public string value = ""; }
        [System.Serializable] class ChildSpec
        {
            public string name = ""; public string type = "image";   // image | text | empty
            public string sprite = ""; public string text = ""; public string color = ""; public bool preserveAspect;
            public string anchorMin = ""; public string anchorMax = ""; public string anchoredPosition = ""; public string sizeDelta = ""; public string pivot = "";
            public int fontSize = 32; public string fontResource = "";
            public int index = -1;   // sibling order: -1 = append (last/front); 0 = first (back, e.g. a background)
        }
        [System.Serializable] class Edit { public string path = ""; public string duplicateAs = ""; public Op[] ops = System.Array.Empty<Op>(); public ChildSpec[] addChild = System.Array.Empty<ChildSpec>(); }
        [System.Serializable] class Request { public string prefabPath = ""; public Edit[] edits = System.Array.Empty<Edit>(); }
        [System.Serializable] class Result { public string prefabPath = ""; public string[] applied = System.Array.Empty<string>(); public string[] errors = System.Array.Empty<string>(); }

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();
            if (string.IsNullOrEmpty(req.prefabPath))
                return CommandResult.Failure("Required: prefabPath.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(req.prefabPath) == null)
                return CommandResult.Failure($"Prefab not found: '{req.prefabPath}'.");

            var applied = new List<string>();
            var errors = new List<string>();

            var root = PrefabUtility.LoadPrefabContents(req.prefabPath);
            try
            {
                // Snapshot every RectTransform and Graphic so a layout/canvas rebuild triggered by an
                // edit (e.g. childAlignment, or an Image color change that re-renders TMP) cannot move
                // or recolor objects we never targeted.
                var allRts = root.GetComponentsInChildren<RectTransform>(true);
                var snap = new Dictionary<RectTransform, RtState>();
                foreach (var r in allRts) snap[r] = RtState.Of(r);
                var allGraphics = root.GetComponentsInChildren<Graphic>(true);
                var colSnap = new Dictionary<Graphic, Color>();
                foreach (var g in allGraphics) colSnap[g] = g.color;
                var rectTargeted = new HashSet<RectTransform>();
                var colorTargeted = new HashSet<Graphic>();

                foreach (var edit in req.edits)
                {
                    var src = FindChild(root.transform, edit.path);
                    if (src == null) { errors.Add($"path not found: '{edit.path}'"); continue; }
                    var go = src;
                    if (!string.IsNullOrEmpty(edit.duplicateAs))
                    {
                        go = Object.Instantiate(src, src.transform.parent);   // new sibling; not in the snapshots, so never restored
                        go.name = edit.duplicateAs;
                        applied.Add($"{edit.path}:duplicate->{edit.duplicateAs}");
                    }
                    foreach (var op in edit.ops)
                    {
                        try
                        {
                            ApplyOp(go, op);
                            if (IsRectOp(op.property) && go.transform is RectTransform grt) rectTargeted.Add(grt);
                            if (op.property == "color") { var gg = go.GetComponent<Graphic>(); if (gg != null) colorTargeted.Add(gg); }
                            applied.Add($"{edit.path}:{op.property}");
                        }
                        catch (System.Exception e) { errors.Add($"{edit.path}:{op.property}: {e.Message}"); }
                    }
                    // New children: created under the found node. They are NOT in the pre-edit snapshots,
                    // so the restore pass below never touches them — same safety as duplicateAs.
                    foreach (var ch in edit.addChild)
                    {
                        try { CreateChild(go.transform, ch); applied.Add($"{edit.path}:addChild->{ch.name}"); }
                        catch (System.Exception e) { errors.Add($"{edit.path}:addChild({ch.name}): {e.Message}"); }
                    }
                }

                // Restore everything we did not explicitly target (undoes collateral rebuild changes).
                foreach (var r in allRts)
                    if (r != null && !rectTargeted.Contains(r) && snap.TryGetValue(r, out var s)) s.ApplyTo(r);
                foreach (var g in allGraphics)
                    if (g != null && !colorTargeted.Contains(g) && colSnap.TryGetValue(g, out var c)) g.color = c;

                PrefabUtility.SaveAsPrefabAsset(root, req.prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }

            AssetDatabase.SaveAssets();
            var result = new Result { prefabPath = req.prefabPath, applied = applied.ToArray(), errors = errors.ToArray() };
            return CommandResult.Success(JsonUtility.ToJson(result));
        }

        // Reflection so the bridge stays decoupled from TMP (separate assembly) yet still drives it.
        static readonly System.Type? TmpType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
        static readonly System.Type? TmpFontType = System.Type.GetType("TMPro.TMP_FontAsset, Unity.TextMeshPro");

        // Create a new child object under `parent`. Fills the README-flagged gap: prefab.modify could
        // patch existing nodes but not ADD one (e.g. overlay an Image sprite under a text slot, or add a
        // label). Defaults to a full-stretch RectTransform when no anchors are given.
        static void CreateChild(Transform parent, ChildSpec ch)
        {
            if (string.IsNullOrEmpty(ch.name)) throw new System.Exception("child needs a name");
            var go = new GameObject(ch.name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            if (ch.index >= 0) rt.SetSiblingIndex(ch.index);   // 0 = first child (renders behind) — e.g. a background
            // default: fill the parent
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            if (!string.IsNullOrEmpty(ch.anchorMin)) rt.anchorMin = V2(ch.anchorMin);
            if (!string.IsNullOrEmpty(ch.anchorMax)) rt.anchorMax = V2(ch.anchorMax);
            if (!string.IsNullOrEmpty(ch.pivot)) rt.pivot = V2(ch.pivot);
            if (!string.IsNullOrEmpty(ch.sizeDelta)) rt.sizeDelta = V2(ch.sizeDelta);
            if (!string.IsNullOrEmpty(ch.anchoredPosition)) rt.anchoredPosition = V2(ch.anchoredPosition);

            switch ((ch.type ?? "image").Trim().ToLowerInvariant())
            {
                case "text":
                    if (TmpType == null) throw new System.Exception("TMP not available");
                    var tmp = go.AddComponent(TmpType);
                    SetProp(tmp, "text", ch.text ?? "");
                    SetProp(tmp, "fontSize", (float)ch.fontSize);
                    if (!string.IsNullOrEmpty(ch.color) && TryColor(ch.color, out var tc)) SetProp(tmp, "color", tc);
                    if (TmpFontType != null && !string.IsNullOrEmpty(ch.fontResource))
                    {
                        var fa = Resources.Load(ch.fontResource, TmpFontType);
                        if (fa != null) SetProp(tmp, "font", fa);
                    }
                    break;
                case "empty":
                    break;
                default: // image
                    var img = go.AddComponent<Image>();
                    if (!string.IsNullOrEmpty(ch.sprite))
                    {
                        var sp = AssetDatabase.LoadAssetAtPath<Sprite>(ch.sprite);
                        if (sp == null) throw new System.Exception($"sprite not found: '{ch.sprite}'");
                        img.sprite = sp; img.preserveAspect = ch.preserveAspect;
                    }
                    if (!string.IsNullOrEmpty(ch.color) && TryColor(ch.color, out var ic)) img.color = ic;
                    break;
            }
        }

        static void SetProp(object o, string name, object value)
        {
            var p = o.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (p != null && p.CanWrite) { try { p.SetValue(o, value); } catch { } }
        }

        static bool TryColor(string value, out Color c)
        {
            c = Color.white;
            if (string.IsNullOrEmpty(value)) return false;
            if (value.TrimStart().StartsWith("#")) return ColorUtility.TryParseHtmlString(value.Trim(), out c);
            var p = value.Split(',');
            if (p.Length < 3) return false;
            c = new Color(F(p[0]), F(p[1]), F(p[2]), p.Length > 3 ? F(p[3]) : 1f); return true;
        }

        static void ApplyOp(GameObject go, Op op)
        {
            var t = go.transform;
            var rt = t as RectTransform;
            switch (op.property)
            {
                case "active": go.SetActive(bool.Parse(op.value.Trim())); break;
                case "name": go.name = op.value; break;
                case "addComponent": AddComponentByName(go, op.value); break;
                case "text": SetText(go, op.value); break;
                case "sprite": SetSprite(go, op.value); break;
                case "color": SetColor(go, op.value); break;
                case "anchoredPosition": Req(rt).anchoredPosition = V2(op.value); break;
                case "sizeDelta": Req(rt).sizeDelta = V2(op.value); break;
                case "anchorMin": Req(rt).anchorMin = V2(op.value); break;
                case "anchorMax": Req(rt).anchorMax = V2(op.value); break;
                case "pivot": Req(rt).pivot = V2(op.value); break;
                case "localScale": t.localScale = V3(op.value); break;
                default:
                    // Generic "ComponentType.member" reflection setter, e.g. "VerticalLayoutGroup.childAlignment".
                    if (op.property.Contains(".")) { SetMember(go, op.property, op.value); break; }
                    throw new System.Exception($"unknown property '{op.property}'");
            }
        }

        // Add a component by simple or full type name (resolved across all assemblies). Idempotent:
        // a component of that exact type already present is left as-is, so re-running an edit is safe.
        static void AddComponentByName(GameObject go, string typeName)
        {
            var t = ComponentTypeResolver.Resolve(typeName);
            if (t == null) throw new System.Exception($"unknown component type '{typeName}'");
            if (go.GetComponent(t) != null) return;
            go.AddComponent(t);
        }

        // Reflection so the bridge stays decoupled from TMP (separate assembly) yet still drives it.
        static void SetText(GameObject go, string value)
        {
            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp == null) continue;
                var prop = comp.GetType().GetProperty("text", BindingFlags.Public | BindingFlags.Instance, null, typeof(string), System.Type.EmptyTypes, null);
                if (prop != null && prop.CanWrite) { prop.SetValue(comp, value); return; }
            }
            throw new System.Exception("no component with a writable string 'text'");
        }

        static void SetSprite(GameObject go, string assetPath)
        {
            var img = go.GetComponent<Image>();
            if (img == null) throw new System.Exception("no Image component");
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sp == null) throw new System.Exception($"sprite not found: '{assetPath}'");
            img.sprite = sp;
        }

        static void SetColor(GameObject go, string value)
        {
            Color c;
            if (value.TrimStart().StartsWith("#"))
            {
                if (!ColorUtility.TryParseHtmlString(value.Trim(), out c)) throw new System.Exception($"bad color '{value}'");
            }
            else
            {
                var p = value.Split(',');
                c = new Color(F(p[0]), F(p[1]), F(p[2]), p.Length > 3 ? F(p[3]) : 1f);
            }
            var g = go.GetComponent<Graphic>();
            if (g == null) throw new System.Exception("no Graphic component");
            g.color = c;
        }

        static GameObject? FindChild(Transform root, string path)
        {
            if (string.IsNullOrEmpty(path)) return root.gameObject;
            var t = root.Find(path);
            if (t != null) return t.gameObject;
            // tolerate a leading root-name segment
            int slash = path.IndexOf('/');
            if (slash > 0 && path.Substring(0, slash) == root.name)
            {
                t = root.Find(path.Substring(slash + 1));
                if (t != null) return t.gameObject;
            }
            // last resort: recursive search by leaf name
            var leaf = path.Contains("/") ? path.Substring(path.LastIndexOf('/') + 1) : path;
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == leaf) return child.gameObject;
            return null;
        }

        static RectTransform Req(RectTransform? rt) => rt != null ? rt : throw new System.Exception("not a RectTransform (UI) object");

        static bool IsRectOp(string p) => p is "anchoredPosition" or "sizeDelta" or "anchorMin" or "anchorMax" or "pivot" or "localScale";

        readonly struct RtState
        {
            readonly Vector2 ap, sd, amin, amax, piv; readonly Vector3 ls;
            RtState(Vector2 a, Vector2 s, Vector2 mn, Vector2 mx, Vector2 p, Vector3 l) { ap = a; sd = s; amin = mn; amax = mx; piv = p; ls = l; }
            public static RtState Of(RectTransform r) => new(r.anchoredPosition, r.sizeDelta, r.anchorMin, r.anchorMax, r.pivot, r.localScale);
            public void ApplyTo(RectTransform r)
            {
                r.anchoredPosition = ap; r.sizeDelta = sd; r.anchorMin = amin; r.anchorMax = amax; r.pivot = piv; r.localScale = ls;
            }
        }

        // Set an arbitrary public property/field on a named component via reflection, coercing the
        // string value to the member type (enum / bool / int / float / string / Color).
        static void SetMember(GameObject go, string spec, string value)
        {
            int dot = spec.IndexOf('.');
            string typeName = spec.Substring(0, dot);
            string member = spec.Substring(dot + 1);

            Component? comp = null;
            foreach (var c in go.GetComponents<Component>())
            {
                if (c == null) continue;
                var ct = c.GetType();
                if (ct.Name == typeName || ct.FullName == typeName) { comp = c; break; }
            }
            if (comp == null) throw new System.Exception($"component '{typeName}' not found on object");

            var type = comp.GetType();
            var prop = type.GetProperty(member, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.CanWrite) { prop.SetValue(comp, Coerce(value, prop.PropertyType)); return; }
            var field = type.GetField(member, BindingFlags.Public | BindingFlags.Instance);
            if (field != null) { field.SetValue(comp, Coerce(value, field.FieldType)); return; }
            throw new System.Exception($"member '{member}' not writable on {typeName}");
        }

        static object Coerce(string v, System.Type t)
        {
            v = v.Trim();
            if (t.IsEnum) return System.Enum.Parse(t, v, true);
            if (t == typeof(bool)) return bool.Parse(v);
            if (t == typeof(int)) return int.Parse(v, CultureInfo.InvariantCulture);
            if (t == typeof(float)) return F(v);
            if (t == typeof(string)) return v;
            if (t == typeof(Color)) { ColorUtility.TryParseHtmlString(v, out var c); return c; }
            return System.Convert.ChangeType(v, t, CultureInfo.InvariantCulture);
        }
        static float F(string s) => float.Parse(s.Trim(), CultureInfo.InvariantCulture);
        static Vector2 V2(string s) { var p = s.Split(','); return new Vector2(F(p[0]), F(p[1])); }
        static Vector3 V3(string s) { var p = s.Split(','); return new Vector3(F(p[0]), F(p[1]), p.Length > 2 ? F(p[2]) : 0f); }
    }
}

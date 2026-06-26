#nullable enable
using System;
using System.IO;
using AIBridge.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace AIBridge.Editor.Handlers
{
    /// <summary>
    /// tmp.createMaterialPreset — create a TMP material preset (.mat) from a font asset (Bridge4Unity_개선제안
    /// P1-2). Instances a NEW material off the font's atlas material and sets Outline / Underlay(shadow) /
    /// Glow — so the original font asset is never edited (avoids the "edit TMP color → font breaks" bug).
    /// Uses the plain Material API (SetColor/SetFloat/EnableKeyword) so the package needs no TMPro asmdef ref.
    /// Assign the result to a text's fontSharedMaterial (game code or prefab.modify) to apply.
    ///
    /// Request: { "fontResource":"fonts/KoreanSDF", "name":"mat_dialogue", "outDir":"Assets/Game/Resources/fonts/mat",
    ///            "face":"#F4F1EA",
    ///            "outline":{ "color":"#1A1209", "width":0.18 },
    ///            "underlay":{ "color":"#000000", "alpha":0.6, "offsetX":0.6, "offsetY":-0.6, "dilate":0.1, "softness":0.35 },
    ///            "glow":{ "color":"#FFC83D", "offset":0, "inner":0.1, "outer":0.35, "power":0.8 } }
    /// </summary>
    public class TmpCreateMaterialPresetHandler : ICommandHandler
    {
        public string Command => "tmp.createMaterialPreset";

        [Serializable] class Outline { public string color = ""; public float width; }
        [Serializable] class Underlay { public string color = ""; public float alpha = 1f; public float offsetX, offsetY, dilate, softness; public bool _set; }
        [Serializable] class Glow { public string color = ""; public float offset, inner, outer, power; public bool _set; }
        [Serializable] class Request
        {
            public string fontResource = "fonts/KoreanSDF";
            public string name = "";
            public string outDir = "Assets/Game/Resources/fonts/mat";
            public string face = "";
            public Outline? outline;
            public Underlay? underlay;
            public Glow? glow;
        }
        [Serializable] class Result { public string asset = ""; public string[] applied = Array.Empty<string>(); }

        static readonly Type? TmpFontType = Type.GetType("TMPro.TMP_FontAsset, Unity.TextMeshPro");

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();
            if (string.IsNullOrEmpty(req.name)) return CommandResult.Failure("Required: name.");
            if (TmpFontType == null) return CommandResult.Failure("TMPro not available.");

            var font = Resources.Load(req.fontResource, TmpFontType);
            if (font == null) return CommandResult.Failure($"Font asset not found in Resources: {req.fontResource}");
            var baseMatProp = TmpFontType.GetProperty("material");
            var baseMat = baseMatProp?.GetValue(font) as Material;
            if (baseMat == null) return CommandResult.Failure("Font has no base material.");

            var mat = new Material(baseMat) { name = req.name };
            var applied = new System.Collections.Generic.List<string>();

            if (TryColor(req.face, out var fc)) { mat.SetColor("_FaceColor", fc); applied.Add("face"); }

            if (req.outline != null && req.outline.width > 0f)
            {
                mat.EnableKeyword("OUTLINE_ON");
                if (TryColor(req.outline.color, out var oc)) mat.SetColor("_OutlineColor", oc);
                mat.SetFloat("_OutlineWidth", req.outline.width);
                applied.Add("outline");
            }
            if (req.underlay != null && !string.IsNullOrEmpty(req.underlay.color))
            {
                mat.EnableKeyword("UNDERLAY_ON");
                if (TryColor(req.underlay.color, out var uc)) { uc.a = req.underlay.alpha; mat.SetColor("_UnderlayColor", uc); }
                mat.SetFloat("_UnderlayOffsetX", req.underlay.offsetX);
                mat.SetFloat("_UnderlayOffsetY", req.underlay.offsetY);
                mat.SetFloat("_UnderlayDilate", req.underlay.dilate);
                mat.SetFloat("_UnderlaySoftness", req.underlay.softness);
                applied.Add("underlay");
            }
            if (req.glow != null && !string.IsNullOrEmpty(req.glow.color))
            {
                mat.EnableKeyword("GLOW_ON");
                if (TryColor(req.glow.color, out var gc)) mat.SetColor("_GlowColor", gc);
                mat.SetFloat("_GlowOffset", req.glow.offset);
                mat.SetFloat("_GlowInner", req.glow.inner);
                mat.SetFloat("_GlowOuter", req.glow.outer);
                mat.SetFloat("_GlowPower", req.glow.power);
                applied.Add("glow");
            }

            string projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            Directory.CreateDirectory(Path.Combine(projectRoot, req.outDir));
            string assetPath = req.outDir.TrimEnd('/') + "/" + req.name + ".mat";
            AssetDatabase.CreateAsset(mat, assetPath);
            AssetDatabase.SaveAssets();

            return CommandResult.Success(JsonUtility.ToJson(new Result { asset = assetPath, applied = applied.ToArray() }));
        }

        static bool TryColor(string? s, out Color c)
        {
            c = Color.white;
            return !string.IsNullOrEmpty(s) && ColorUtility.TryParseHtmlString(s, out c);
        }
    }
}

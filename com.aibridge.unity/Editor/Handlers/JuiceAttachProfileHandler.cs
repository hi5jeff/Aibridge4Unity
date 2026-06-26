#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using AIBridge.Editor.Core;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace AIBridge.Editor.Handlers
{
    /// <summary>
    /// juice.attachProfile — give an object life with one call (Bridge4Unity_개선제안 P2-2). Builds a looping
    /// AnimationClip from a named preset + an AnimatorController + an Animator, so the object animates in Play
    /// mode with zero hand-authored keyframes. The opposite end of animation.create (explicit channels): this
    /// is the zero-config "make this breathe / bob / pulse / spin / throb" button.
    ///
    /// Non-destructive: every preset is keyed off the object's CURRENT localScale / localPosition /
    /// localEulerAngles / color at attach time, so it never snaps an authored transform to (1,1,1) or origin.
    ///
    /// Profiles: breathe (gentle scale yoyo) | pulse (sharp scale punch+rest) | bob (vertical position yoyo) |
    /// spin (continuous Z rotation) | throb (color-alpha yoyo; SpriteRenderer or any UGUI Graphic).
    /// Request: { "path":"Square", "profile":"breathe", "amount":0.05, "period":2.0, "name":"juice_breathe" }
    ///   amount — scale: fraction (0.05 = ±5%); bob: world units; spin: ignored (always 360°); throb: alpha drop fraction.
    ///   period — seconds for one full loop.
    /// </summary>
    public class JuiceAttachProfileHandler : ICommandHandler
    {
        public string Command => "juice.attachProfile";

        [Serializable] class Request { public string path = ""; public string profile = "breathe"; public float amount = 0f; public float period = 0f; public string name = ""; }
        [Serializable] class Result { public string target = ""; public string profile = ""; public string clipPath = ""; public string controllerPath = ""; public int curves; }

        const string Dir = "Assets/AIBridge/Animations";

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();
            if (string.IsNullOrEmpty(req.path)) return CommandResult.Failure("Required: path.");
            var go = SceneLookup.FindByPath(req.path, UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            if (go == null) return CommandResult.Failure($"GameObject not found: '{req.path}'.");

            var profile = (req.profile ?? "breathe").Trim().ToLowerInvariant();
            float period = req.period > 0.01f ? req.period : (profile == "pulse" ? 1.2f : profile == "spin" ? 3f : 2f);

            var clip = new AnimationClip { frameRate = 60f };
            int curves;
            try { curves = Build(clip, go, profile, req.amount, period); }
            catch (Exception e) { return CommandResult.Failure(e.Message); }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            Directory.CreateDirectory(Dir);
            var safe = string.IsNullOrEmpty(req.name) ? $"juice_{profile}" : req.name;
            var clipPath = $"{Dir}/{safe}.anim";
            var controllerPath = $"{Dir}/{safe}.controller";
            AssetDatabase.CreateAsset(clip, clipPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPathWithClip(controllerPath, clip);

            // NOTE: `??` does NOT work here — GetComponent returns Unity's "fake null" which the C# ?? operator
            // treats as non-null. Must use an explicit == null check (Unity overloads ==).
            var animator = go.GetComponent<Animator>();
            if (animator == null) animator = Undo.AddComponent<Animator>(go);
            animator.runtimeAnimatorController = controller;

            EditorUtility.SetDirty(go);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = go;

            return CommandResult.Success(JsonUtility.ToJson(new Result
            { target = req.path, profile = profile, clipPath = clipPath, controllerPath = controllerPath, curves = curves }));
        }

        static int Build(AnimationClip clip, GameObject go, string profile, float amount, float period)
        {
            var t = go.transform;
            switch (profile)
            {
                case "breathe":
                {
                    var a = amount > 0f ? amount : 0.05f;
                    var s = t.localScale;
                    Vec3Yoyo(clip, "localScale", s, new Vector3(s.x * (1 + a), s.y * (1 + a), s.z), period);
                    return 3;
                }
                case "pulse":
                {
                    var a = amount > 0f ? amount : 0.12f;
                    var s = t.localScale;
                    var peak = new Vector3(s.x * (1 + a), s.y * (1 + a), s.z);
                    // sharp punch up early, settle back, then rest — distinct from breathe's smooth sine.
                    foreach (var (axis, b, p) in new[] { ("x", s.x, peak.x), ("y", s.y, peak.y), ("z", s.z, s.z) })
                        clip.SetCurve("", typeof(Transform), $"localScale.{axis}", Curve((0, b), (period * 0.18f, p), (period * 0.45f, b), (period, b)));
                    return 3;
                }
                case "bob":
                {
                    var a = amount > 0f ? amount : 0.15f;
                    var p = t.localPosition;
                    clip.SetCurve("", typeof(Transform), "localPosition.x", Const(p.x, period));
                    clip.SetCurve("", typeof(Transform), "localPosition.y", Curve((0, p.y), (period / 2f, p.y + a), (period, p.y)));
                    clip.SetCurve("", typeof(Transform), "localPosition.z", Const(p.z, period));
                    return 3;
                }
                case "spin":
                {
                    var e = t.localEulerAngles;
                    clip.SetCurve("", typeof(Transform), "localEulerAngles.z", Curve((0, e.z), (period, e.z + 360f)));
                    return 1;
                }
                case "throb":
                {
                    var a = amount > 0f ? amount : 0.4f;
                    var (compType, alpha) = ColorTarget(go);
                    if (compType == null) throw new Exception("throb needs a SpriteRenderer or a UGUI Graphic (Image/Text) on the object");
                    clip.SetCurve("", compType, "m_Color.a", Curve((0, alpha), (period / 2f, alpha * (1 - a)), (period, alpha)));
                    return 1;
                }
                default:
                    throw new Exception($"unknown profile '{profile}' (use breathe|pulse|bob|spin|throb)");
            }
        }

        static void Vec3Yoyo(AnimationClip clip, string baseName, Vector3 a, Vector3 b, float period)
        {
            clip.SetCurve("", typeof(Transform), $"{baseName}.x", Curve((0, a.x), (period / 2f, b.x), (period, a.x)));
            clip.SetCurve("", typeof(Transform), $"{baseName}.y", Curve((0, a.y), (period / 2f, b.y), (period, a.y)));
            clip.SetCurve("", typeof(Transform), $"{baseName}.z", Curve((0, a.z), (period / 2f, b.z), (period, a.z)));
        }

        // SpriteRenderer first; else any UGUI Graphic (Image/RawImage/Text/TMP) — animate m_Color.a on its type.
        static (Type? type, float alpha) ColorTarget(GameObject go)
        {
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null) return (typeof(SpriteRenderer), sr.color.a);
            var g = go.GetComponent<UnityEngine.UI.Graphic>();
            if (g != null) return (g.GetType(), g.color.a);
            return (null, 1f);
        }

        static AnimationCurve Const(float v, float period) => Curve((0, v), (period, v));

        static AnimationCurve Curve(params (float t, float v)[] keys)
        {
            var c = new AnimationCurve();
            foreach (var (kt, kv) in keys) c.AddKey(kt, kv);
            for (var i = 0; i < c.length; i++) c.SmoothTangents(i, 0f);
            return c;
        }
    }
}

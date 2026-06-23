#nullable enable
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
    /// Turnkey animation authoring: builds an AnimationClip from keyframe channels (via SetCurve), creates an
    /// AnimatorController with it, and attaches an Animator to the target — so the object animates in Play mode.
    /// Approach (AnimationClip + SetCurve) adapted from Unity-AI-Animation (Ivan Murzak, Apache-2.0).
    /// </summary>
    public class AnimationCreateHandler : ICommandHandler
    {
        public string Command => "animation.create";

        [System.Serializable] class Key { public float t; public string v = ""; }

        [System.Serializable]
        class Channel
        {
            public string componentType = "";  // optional override; aliases provide a default
            public string property = "";        // alias (localPosition/localScale/rotation/color) or exact binding
            public string relativePath = "";    // "" = the target object itself
            public Key[] keys = System.Array.Empty<Key>();
        }

        [System.Serializable]
        class Request
        {
            public string path = "";
            public string name = "anim";
            public bool loop = true;
            public float frameRate = 60f;
            public Channel[] channels = System.Array.Empty<Channel>();
        }

        [System.Serializable]
        class Result
        {
            public string clipPath = "";
            public string controllerPath = "";
            public string target = "";
            public int curves;
            public string[] errors = System.Array.Empty<string>();
        }

        // alias -> (default component, binding base, per-axis suffixes)
        static readonly Dictionary<string, (string comp, string baseName, string[] sfx)> Aliases = new()
        {
            { "localposition", ("Transform", "localPosition", new[] { "x", "y", "z" }) },
            { "position",      ("Transform", "localPosition", new[] { "x", "y", "z" }) },
            { "localscale",    ("Transform", "localScale",    new[] { "x", "y", "z" }) },
            { "scale",         ("Transform", "localScale",    new[] { "x", "y", "z" }) },
            { "localeulerangles", ("Transform", "localEulerAngles", new[] { "x", "y", "z" }) },
            { "rotation",      ("Transform", "localEulerAngles", new[] { "x", "y", "z" }) },
            { "color",         ("SpriteRenderer", "m_Color", new[] { "r", "g", "b", "a" }) },
        };

        const string Dir = "Assets/AIBridge/Animations";

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();
            if (string.IsNullOrEmpty(req.path))
                return CommandResult.Failure("Required: path.");

            var go = SceneLookup.FindByPath(req.path, UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            if (go == null)
                return CommandResult.Failure($"GameObject not found: '{req.path}'.");

            var clip = new AnimationClip { frameRate = req.frameRate };
            var errors = new List<string>();
            var curves = 0;

            foreach (var ch in req.channels)
            {
                try { curves += ApplyChannel(clip, ch); }
                catch (System.Exception e) { errors.Add($"{ch.property}: {e.Message}"); }
            }

            // Loop setting that the Animator honors.
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = req.loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            Directory.CreateDirectory(Dir);
            var safe = string.IsNullOrEmpty(req.name) ? "anim" : req.name;
            var clipPath = $"{Dir}/{safe}.anim";
            var controllerPath = $"{Dir}/{safe}.controller";

            AssetDatabase.CreateAsset(clip, clipPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPathWithClip(controllerPath, clip);

            var animator = go.GetComponent<Animator>();
            if (animator == null)
                animator = Undo.AddComponent<Animator>(go);
            animator.runtimeAnimatorController = controller;

            EditorUtility.SetDirty(go);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = go;

            return CommandResult.Success(JsonUtility.ToJson(new Result
            {
                clipPath = clipPath,
                controllerPath = controllerPath,
                target = req.path,
                curves = curves,
                errors = errors.ToArray()
            }));
        }

        static int ApplyChannel(AnimationClip clip, Channel ch)
        {
            if (ch.keys == null || ch.keys.Length == 0)
                throw new System.Exception("keys required");

            var key = (ch.property ?? "").ToLowerInvariant();
            if (Aliases.TryGetValue(key, out var a))
            {
                var compName = string.IsNullOrEmpty(ch.componentType) ? a.comp : ch.componentType;
                var type = ComponentTypeResolver.Resolve(compName) ?? throw new System.Exception($"type '{compName}' not found");
                for (var axis = 0; axis < a.sfx.Length; axis++)
                {
                    var curve = new AnimationCurve();
                    foreach (var k in ch.keys)
                    {
                        var comps = Floats(k.v);
                        if (axis < comps.Length)
                            curve.AddKey(k.t, comps[axis]);
                    }
                    Smooth(curve);
                    clip.SetCurve(ch.relativePath ?? "", type, $"{a.baseName}.{a.sfx[axis]}", curve);
                }
                return a.sfx.Length;
            }
            else
            {
                // exact binding: property is the full propertyName, v is a scalar
                if (string.IsNullOrEmpty(ch.componentType))
                    throw new System.Exception("componentType required for non-alias property");
                var type = ComponentTypeResolver.Resolve(ch.componentType) ?? throw new System.Exception($"type '{ch.componentType}' not found");
                var curve = new AnimationCurve();
                foreach (var k in ch.keys)
                    curve.AddKey(k.t, F(k.v));
                Smooth(curve);
                clip.SetCurve(ch.relativePath ?? "", type, ch.property, curve);
                return 1;
            }
        }

        static void Smooth(AnimationCurve c)
        {
            for (var i = 0; i < c.length; i++)
                c.SmoothTangents(i, 0f);
        }

        static float F(string s) => float.Parse(s.Trim(), CultureInfo.InvariantCulture);
        static float[] Floats(string s)
        {
            var p = s.Split(',');
            var r = new float[p.Length];
            for (var i = 0; i < p.Length; i++) r[i] = F(p[i]);
            return r;
        }
    }
}

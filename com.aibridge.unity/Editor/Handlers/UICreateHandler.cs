#nullable enable
using System.Globalization;
using AIBridge.Editor.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AIBridge.Editor.Handlers
{
    /// <summary>
    /// Turnkey UGUI builder. Creates an Image / Text / Button (or just ensures a Canvas), auto-creating a
    /// portrait-friendly Canvas (ScreenSpaceOverlay + CanvasScaler) and an EventSystem if missing, and
    /// positions the element with an anchor preset. Removes the UGUI plumbing the AI would otherwise hand-wire.
    /// </summary>
    public class UICreateHandler : ICommandHandler
    {
        public string Command => "ui.create";

        [System.Serializable]
        class Request
        {
            public string kind = "image";     // canvas | image | text | button
            public string name = "";
            public string parentPath = "";    // a Canvas or UI element; empty = the ensured Canvas
            public string anchor = "center";  // center/top/bottom/left/right/top-left/.../stretch
            public float x, y;
            public float width = 200, height = 80;
            public string text = "";
            public int fontSize = 28;
            public string color = "";         // image/button background, or text color
            public string sprite = "";        // optional sprite asset path (image/button)
            public int referenceWidth = 1080, referenceHeight = 1920;
        }

        [System.Serializable]
        class Result { public string path = ""; public string kind = ""; public string canvas = ""; }

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();
            var scene = SceneManager.GetActiveScene();

            var canvas = EnsureCanvas(req);
            EnsureEventSystem();

            var kind = (req.kind ?? "image").ToLowerInvariant();
            if (kind == "canvas")
                return Done(canvas.gameObject, "canvas", canvas);

            var parent = canvas.transform;
            if (!string.IsNullOrEmpty(req.parentPath))
            {
                var p = SceneLookup.FindByPath(req.parentPath, scene);
                if (p != null) parent = p.transform;
            }

            GameObject go;
            switch (kind)
            {
                case "image": go = BuildImage(req, parent); break;
                case "text": go = BuildText(req, parent); break;
                case "button": go = BuildButton(req, parent); break;
                default: return CommandResult.Failure($"Unknown kind '{req.kind}'. Use canvas/image/text/button.");
            }

            ApplyRect(go.GetComponent<RectTransform>(), req);
            Undo.RegisterCreatedObjectUndo(go, "AI Bridge: ui.create");
            EditorUtility.SetDirty(go);
            if (!EditorApplication.isPlaying)
                EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = go;

            return Done(go, kind, canvas);
        }

        // ---- builders ----

        static GameObject BuildImage(Request req, Transform parent)
        {
            var go = NewUI(string.IsNullOrEmpty(req.name) ? "Image" : req.name, parent);
            var img = go.AddComponent<Image>();
            if (TryColor(req.color, out var c)) img.color = c;
            var sprite = LoadSprite(req.sprite);
            if (sprite != null) img.sprite = sprite;
            return go;
        }

        static GameObject BuildText(Request req, Transform parent)
        {
            var go = NewUI(string.IsNullOrEmpty(req.name) ? "Text" : req.name, parent);
            var t = go.AddComponent<Text>();
            t.text = req.text;
            t.font = BuiltinFont();
            t.fontSize = req.fontSize;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = TryColor(req.color, out var c) ? c : Color.black;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return go;
        }

        static GameObject BuildButton(Request req, Transform parent)
        {
            var go = NewUI(string.IsNullOrEmpty(req.name) ? "Button" : req.name, parent);
            var img = go.AddComponent<Image>();
            img.color = TryColor(req.color, out var c) ? c : new Color(0.2f, 0.5f, 1f, 1f);
            var sprite = LoadSprite(req.sprite);
            if (sprite != null) img.sprite = sprite;
            go.AddComponent<Button>();

            if (!string.IsNullOrEmpty(req.text))
            {
                var label = NewUI("Text", go.transform);
                var t = label.AddComponent<Text>();
                t.text = req.text;
                t.font = BuiltinFont();
                t.fontSize = req.fontSize;
                t.alignment = TextAnchor.MiddleCenter;
                t.color = Color.white;
                var rt = label.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
            return go;
        }

        // ---- helpers ----

        static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        Canvas EnsureCanvas(Request req)
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
                return canvas;

            var go = new GameObject("Canvas");
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(req.referenceWidth, req.referenceHeight);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(go, "AI Bridge: create Canvas");
            return canvas;
        }

        static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
                return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            // Prefer the new Input System UI module if present; otherwise the legacy one.
            var moduleType = ComponentTypeResolver.Resolve("InputSystemUIInputModule")
                             ?? ComponentTypeResolver.Resolve("StandaloneInputModule");
            if (moduleType != null)
                go.AddComponent(moduleType);
            Undo.RegisterCreatedObjectUndo(go, "AI Bridge: create EventSystem");
        }

        static void ApplyRect(RectTransform rt, Request req)
        {
            var (min, max, pivot, stretch) = AnchorPreset(req.anchor);
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = pivot;
            if (stretch)
            {
                rt.offsetMin = new Vector2(req.x, req.y);
                rt.offsetMax = new Vector2(-req.x, -req.y);
            }
            else
            {
                rt.sizeDelta = new Vector2(req.width, req.height);
                rt.anchoredPosition = new Vector2(req.x, req.y);
            }
        }

        static (Vector2 min, Vector2 max, Vector2 pivot, bool stretch) AnchorPreset(string a)
        {
            switch ((a ?? "center").ToLowerInvariant())
            {
                case "top": return (new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), false);
                case "bottom": return (new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), false);
                case "left": return (new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), false);
                case "right": return (new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), false);
                case "top-left": return (new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), false);
                case "top-right": return (new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), false);
                case "bottom-left": return (new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), false);
                case "bottom-right": return (new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), false);
                case "stretch": return (Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), true);
                default: return (new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), false);
            }
        }

        static Font BuiltinFont()
        {
            var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return f != null ? f : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        static Sprite? LoadSprite(string assetPath)
            => string.IsNullOrEmpty(assetPath) ? null : AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

        static bool TryColor(string s, out Color c)
        {
            c = Color.white;
            if (string.IsNullOrEmpty(s)) return false;
            var p = s.Split(',');
            if (p.Length < 3) return false;
            float F(int i) => float.Parse(p[i].Trim(), CultureInfo.InvariantCulture);
            c = new Color(F(0), F(1), F(2), p.Length > 3 ? F(3) : 1f);
            return true;
        }

        static CommandResult Done(GameObject go, string kind, Canvas canvas)
            => CommandResult.Success(JsonUtility.ToJson(new Result
            {
                path = SceneObjectDescriber.GetPath(go.transform),
                kind = kind,
                canvas = canvas.name
            }));
    }
}

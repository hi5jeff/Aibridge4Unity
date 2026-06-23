#nullable enable
using System.Collections.Generic;
using System.IO;
using AIBridge.Editor.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace AIBridge.Editor.Handlers
{
    /// <summary>
    /// Renders the camera offscreen (URP render request), draws bounding boxes over visible objects, and
    /// returns the PNG path plus a parallel list of each object's screen rectangle and world position.
    /// The AI gets visual ground truth *and* precise data — no eyeballing pixels.
    /// </summary>
    public class ScreenshotAnnotatedHandler : ICommandHandler
    {
        public string Command => "screenshot.annotated";

        [System.Serializable]
        class Request
        {
            public int width = 540;
            public int height = 960;
            public bool annotate = true;
            public int maxLabels = 50;
        }

        [System.Serializable] class ScreenRect { public float x, y, w, h; }

        [System.Serializable]
        class ObjBox
        {
            public string name = "";
            public string path = "";
            public ScreenRect screenRect = new();   // top-left origin, in image pixels
            public Vec3 worldPosition = new();
        }

        [System.Serializable]
        class Result
        {
            public string imagePath = "";
            public int width;
            public int height;
            public string camera = "";
            public ObjBox[] objects = System.Array.Empty<ObjBox>();
        }

        static readonly Vector3[] Corners =
        {
            new(-1, -1, -1), new(1, -1, -1), new(-1, 1, -1), new(1, 1, -1),
            new(-1, -1, 1),  new(1, -1, 1),  new(-1, 1, 1),  new(1, 1, 1)
        };

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();
            var w = Mathf.Clamp(req.width, 16, 4096);
            var h = Mathf.Clamp(req.height, 16, 4096);

            var cam = Camera.main;
            if (cam == null)
            {
                foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude))
                    if (c.isActiveAndEnabled) { cam = c; break; }
            }
            if (cam == null)
                return CommandResult.Failure("No active camera found.");

            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            var prevTarget = cam.targetTexture;
            var prevActive = RenderTexture.active;
            Texture2D tex;
            try
            {
                cam.aspect = (float)w / h; // keep projection consistent with the output resolution

                var request = new RenderPipeline.StandardRequest { destination = rt };
                if (RenderPipeline.SupportsRenderRequest(cam, request))
                    RenderPipeline.SubmitRenderRequest(cam, request);
                else
                {
                    cam.targetTexture = rt;
                    cam.Render();
                }

                RenderTexture.active = rt;
                tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
            }
            finally
            {
                RenderTexture.active = prevActive;
                cam.targetTexture = prevTarget;
                cam.ResetAspect();
                rt.Release();
                Object.DestroyImmediate(rt);
            }

            var boxes = new List<ObjBox>();
            var scene = SceneManager.GetActiveScene();
            foreach (var renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
            {
                if (boxes.Count >= req.maxLabels) break;
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                if (renderer.gameObject.scene != scene) continue;

                var b = renderer.bounds;
                if (b.size == Vector3.zero) continue;

                var min = new Vector2(float.MaxValue, float.MaxValue);
                var max = new Vector2(float.MinValue, float.MinValue);
                var anyFront = false;
                for (var i = 0; i < 8; i++)
                {
                    var corner = b.center + Vector3.Scale(b.extents, Corners[i]);
                    var vp = cam.WorldToViewportPoint(corner);
                    if (vp.z < 0) continue; // behind the camera
                    anyFront = true;
                    var p = new Vector2(vp.x * w, vp.y * h);
                    min = Vector2.Min(min, p);
                    max = Vector2.Max(max, p);
                }
                if (!anyFront) continue;

                var rx = Mathf.Max(0f, min.x);
                var ry = Mathf.Max(0f, min.y);
                var rmaxx = Mathf.Min(w, max.x);
                var rmaxy = Mathf.Min(h, max.y);
                if (rmaxx <= rx || rmaxy <= ry) continue; // fully offscreen

                if (req.annotate)
                    DrawRect(tex, (int)rx, (int)ry, (int)rmaxx, (int)rmaxy, Color.green);

                boxes.Add(new ObjBox
                {
                    name = renderer.name,
                    path = SceneObjectDescriber.GetPath(renderer.transform),
                    // Texture origin is bottom-left; convert to top-left for the JSON to match how images read.
                    screenRect = new ScreenRect { x = rx, y = h - rmaxy, w = rmaxx - rx, h = rmaxy - ry },
                    worldPosition = new Vec3(renderer.transform.position)
                });
            }

            if (req.annotate)
                tex.Apply();

            var outDir = Path.Combine(BridgeConfig.Load().ResolveChannelRoot(), "out");
            Directory.CreateDirectory(outDir);
            var imgPath = Path.Combine(outDir, "screenshot.png");
            File.WriteAllBytes(imgPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            var result = new Result
            {
                imagePath = imgPath.Replace('\\', '/'),
                width = w,
                height = h,
                camera = cam.name,
                objects = boxes.ToArray()
            };
            return CommandResult.Success(JsonUtility.ToJson(result));
        }

        static void DrawRect(Texture2D tex, int x0, int y0, int x1, int y1, Color col)
        {
            x0 = Mathf.Clamp(x0, 0, tex.width - 1);
            x1 = Mathf.Clamp(x1, 0, tex.width - 1);
            y0 = Mathf.Clamp(y0, 0, tex.height - 1);
            y1 = Mathf.Clamp(y1, 0, tex.height - 1);
            const int t = 2; // outline thickness
            for (var x = x0; x <= x1; x++)
                for (var k = 0; k < t; k++) { SetPx(tex, x, y0 + k, col); SetPx(tex, x, y1 - k, col); }
            for (var y = y0; y <= y1; y++)
                for (var k = 0; k < t; k++) { SetPx(tex, x0 + k, y, col); SetPx(tex, x1 - k, y, col); }
        }

        static void SetPx(Texture2D tex, int x, int y, Color c)
        {
            if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                tex.SetPixel(x, y, c);
        }
    }
}

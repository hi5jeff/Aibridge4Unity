#nullable enable
using System;
using System.IO;
using System.Reflection;
using AIBridge.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace AIBridge.Editor.Handlers
{
    /// <summary>
    /// Captures the actual Game View (including ScreenSpaceOverlay UI, which a camera render misses) by
    /// reading the GameView window's internal render texture. Saves a PNG to the channel `out/` folder.
    /// Technique adapted from Unity-MCP (Ivan Murzak, Apache-2.0). Uses one isolated reflection field read.
    /// </summary>
    public class ScreenshotGameViewHandler : ICommandHandler
    {
        public string Command => "screenshot.gameview";

        [System.Serializable] class Request { public int max = 1080; }
        [System.Serializable] class Result { public string imagePath = ""; public int width; public int height; }

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();

            var gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
            if (gameViewType == null)
                return CommandResult.Failure("UnityEditor.GameView type not found.");

            var gameView = EditorWindow.GetWindow(gameViewType, false, null, true); // focus to force it to render
            if (gameView == null)
                return CommandResult.Failure("No Game View window.");
            gameView.Repaint();
            // Force a synchronous render so the internal RT is populated even if the Game tab isn't focused.
            typeof(EditorWindow).GetMethod("RepaintImmediately", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.Invoke(gameView, null);

            var rtField = gameViewType.GetField("m_RenderTexture", BindingFlags.NonPublic | BindingFlags.Instance);
            var src = rtField?.GetValue(gameView) as RenderTexture;
            if (src == null || !src.IsCreated()) // Unity '== null' also catches a destroyed RT
                return CommandResult.Failure("Game View render texture not ready — open/focus a Game View tab and retry.");

            var maxDim = req.max < 16 ? 1080 : req.max;
            var scale = Mathf.Min(1f, (float)maxDim / Mathf.Max(src.width, src.height));
            var width = Mathf.Max(1, Mathf.RoundToInt(src.width * scale));
            var height = Mathf.Max(1, Mathf.RoundToInt(src.height * scale));

            var prevActive = RenderTexture.active;
            RenderTexture? scaled = null;
            Texture2D? tex = null;
            string imgPath;
            try
            {
                var readSource = src;
                if (scale < 1f)
                {
                    scaled = RenderTexture.GetTemporary(width, height, 0, src.format);
                    Graphics.Blit(src, scaled);
                    readSource = scaled;
                }

                RenderTexture.active = readSource;
                tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);

                // Direct RT reads are vertically flipped on top-left-UV graphics APIs (DirectX/Metal).
                if (SystemInfo.graphicsUVStartsAtTop)
                {
                    var px = tex.GetPixels32();
                    var flipped = new Color32[px.Length];
                    for (var y = 0; y < height; y++)
                        Array.Copy(px, y * width, flipped, (height - 1 - y) * width, width);
                    tex.SetPixels32(flipped);
                }
                tex.Apply();

                var outDir = Path.Combine(BridgeConfig.Load().ResolveChannelRoot(), "out");
                Directory.CreateDirectory(outDir);
                imgPath = Path.Combine(outDir, "gameview.png");
                File.WriteAllBytes(imgPath, tex.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = prevActive;
                if (scaled != null) RenderTexture.ReleaseTemporary(scaled);
                if (tex != null) UnityEngine.Object.DestroyImmediate(tex);
            }

            return CommandResult.Success(JsonUtility.ToJson(new Result
            {
                imagePath = imgPath.Replace('\\', '/'),
                width = width,
                height = height
            }));
        }
    }
}

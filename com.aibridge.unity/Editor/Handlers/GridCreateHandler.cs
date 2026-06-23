#nullable enable
using System.Globalization;
using AIBridge.Editor.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AIBridge.Editor.Handlers
{
    /// <summary>
    /// Builds an N×N grid of square cell tiles in one call (named &lt;prefix&gt;_x_y under a parent), using a
    /// procedural square sprite. Fast way to lay out a board/grid without dozens of channel round-trips.
    /// </summary>
    public class GridCreateHandler : ICommandHandler
    {
        public string Command => "grid.create";

        [System.Serializable]
        class Request
        {
            public int size = 9;
            public float cellSize = 0.55f;
            public float gap = 0.06f;
            public string color = "0.95,0.9,0.75,1";
            public string parentName = "Board";
            public string namePrefix = "Cell";
            public int sortingOrder = 0;
            public float z = 0f;
        }

        [System.Serializable]
        class Result { public string parent = ""; public int count; public float step; }

        static Sprite? _square;

        static Sprite SquareSprite()
        {
            if (_square != null) return _square;
            var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            var px = new Color[8 * 8];
            for (var i = 0; i < px.Length; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            _square = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8f); // 1 world unit at scale 1
            return _square;
        }

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();
            if (req.size < 1 || req.size > 50)
                return CommandResult.Failure("size must be 1..50.");

            var scene = SceneManager.GetActiveScene();
            var color = ParseColor(req.color);
            var step = req.cellSize + req.gap;
            var half = (req.size - 1) * 0.5f;
            var sprite = SquareSprite();

            var board = new GameObject(string.IsNullOrEmpty(req.parentName) ? "Board" : req.parentName);

            for (var y = 0; y < req.size; y++)
            for (var x = 0; x < req.size; x++)
            {
                var cell = new GameObject($"{req.namePrefix}_{x}_{y}");
                cell.transform.SetParent(board.transform, false);
                cell.transform.position = new Vector3((x - half) * step, (y - half) * step, req.z);
                cell.transform.localScale = new Vector3(req.cellSize, req.cellSize, 1f);
                var sr = cell.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = color;
                sr.sortingOrder = req.sortingOrder;
            }

            Undo.RegisterCreatedObjectUndo(board, "AI Bridge: create grid");
            EditorUtility.SetDirty(board);
            if (!EditorApplication.isPlaying)
                EditorSceneManager.MarkSceneDirty(scene);

            return CommandResult.Success(JsonUtility.ToJson(new Result
            {
                parent = board.name,
                count = req.size * req.size,
                step = step
            }));
        }

        static Color ParseColor(string s)
        {
            var p = s.Split(',');
            float F(int i) => float.Parse(p[i].Trim(), CultureInfo.InvariantCulture);
            return new Color(F(0), F(1), F(2), p.Length > 3 ? F(3) : 1f);
        }
    }
}

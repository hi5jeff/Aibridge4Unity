#nullable enable
using System.IO;
using AIBridge.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace AIBridge.Editor.UI
{
    /// <summary>
    /// Dockable panel: shows the current selection and lets the user "pin" it (with an optional note),
    /// or pin a clicked point in the Scene view, into the channel as <c>selection.json</c> — so the AI
    /// knows what "this / here" refers to. The conversation stays in the chat; this is the pointer.
    /// </summary>
    public class AIReferenceWindow : EditorWindow
    {
        string _note = "";
        bool _picking;
        Vector2 _scroll;

        [MenuItem("Tools/AI Bridge/AI Reference")]
        public static void Open()
        {
            var window = GetWindow<AIReferenceWindow>("AI Reference");
            window.minSize = new Vector2(260, 200);
            window.Show();
        }

        void OnEnable()
        {
            LocalizationService.SetLanguage(BridgeConfig.Load().language);
            Selection.selectionChanged += Repaint;
        }

        void OnDisable()
        {
            Selection.selectionChanged -= Repaint;
            StopPicking();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField(L("ref.title"), EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(L("ref.help"), MessageType.None);

            var gameObjects = Selection.gameObjects;
            var assets = AssetDescriber.CurrentSelection();
            EditorGUILayout.LabelField(string.Format(L("ref.selectionCount"), gameObjects.Length));

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MaxHeight(80));
            foreach (var go in gameObjects)
                EditorGUILayout.LabelField("• " + SceneObjectDescriber.GetPath(go.transform));
            foreach (var a in assets)
                EditorGUILayout.LabelField("◇ " + a.assetPath);
            EditorGUILayout.EndScrollView();

            if (assets.Length > 0)
                EditorGUILayout.LabelField(string.Format(L("ref.assetCount"), assets.Length));

            EditorGUILayout.LabelField(L("ref.note"));
            _note = EditorGUILayout.TextArea(_note, GUILayout.MinHeight(40));

            using (new EditorGUI.DisabledScope(gameObjects.Length == 0 && assets.Length == 0))
            {
                if (GUILayout.Button(L("ref.pin")))
                    PinSelection();
            }

            GUILayout.Space(4);
            if (GUILayout.Button(_picking ? L("ref.pickActive") : L("ref.pickStart")))
            {
                if (_picking) StopPicking();
                else StartPicking();
            }
        }

        void PinSelection()
        {
            var gameObjects = Selection.gameObjects;
            var infos = new ObjInfo[gameObjects.Length];
            for (var i = 0; i < gameObjects.Length; i++)
                infos[i] = SceneObjectDescriber.Describe(gameObjects[i].transform);

            WritePin(new Pin
            {
                pinnedAt = System.DateTime.Now.ToString("o"),
                note = _note,
                kind = "selection",
                objects = infos,
                assets = AssetDescriber.CurrentSelection()
            });
        }

        void StartPicking()
        {
            _picking = true;
            SceneView.duringSceneGui += OnSceneGui;
            SceneView.RepaintAll();
        }

        void StopPicking()
        {
            if (!_picking)
                return;
            _picking = false;
            SceneView.duringSceneGui -= OnSceneGui;
            SceneView.RepaintAll();
        }

        void OnSceneGui(SceneView view)
        {
            if (!_picking)
                return;

            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                StopPicking();
                e.Use();
                return;
            }

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                var p = ray.origin;
                // Project the ray onto the z = 0 plane (2D / XY workflow).
                if (Mathf.Abs(ray.direction.z) > 1e-5f)
                    p = ray.origin + ray.direction * (-ray.origin.z / ray.direction.z);
                p.z = 0f;

                WritePin(new Pin
                {
                    pinnedAt = System.DateTime.Now.ToString("o"),
                    note = _note,
                    kind = "point",
                    hasPoint = true,
                    point = new Vec3(p)
                });

                StopPicking();
                e.Use();
            }
        }

        static void WritePin(Pin pin)
        {
            var root = BridgeConfig.Load().ResolveChannelRoot();
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, "selection.json");
            File.WriteAllText(path, JsonUtility.ToJson(pin, true));
            Debug.Log($"[AIBridge] Pinned reference -> {path} (kind={pin.kind}, note=\"{pin.note}\")");
        }

        static string L(string key) => LocalizationService.Get(key);

        [System.Serializable]
        class Pin
        {
            public string pinnedAt = "";
            public string note = "";
            public string kind = "selection"; // "selection" | "point"
            public ObjInfo[] objects = System.Array.Empty<ObjInfo>();
            public AssetInfo[] assets = System.Array.Empty<AssetInfo>();
            public bool hasPoint;
            public Vec3 point = new();
        }
    }
}

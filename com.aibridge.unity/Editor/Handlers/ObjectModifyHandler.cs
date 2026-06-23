#nullable enable
using System.Collections.Generic;
using System.Globalization;
using AIBridge.Editor.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AIBridge.Editor.Handlers
{
    /// <summary>
    /// Modifies an existing object's Transform / RectTransform / name / active state via a list of ops.
    /// Only listed properties change (no "absent vs zero" ambiguity). Pairs with selection to close the
    /// loop on "move this here / make this bigger". Records Undo.
    /// </summary>
    public class ObjectModifyHandler : ICommandHandler
    {
        public string Command => "object.modify";

        [System.Serializable]
        class Op
        {
            public string property = "";
            public string value = "";
        }

        [System.Serializable]
        class Request
        {
            public string path = "";
            public Op[] ops = System.Array.Empty<Op>();
        }

        [System.Serializable]
        class Result
        {
            public string[] applied = System.Array.Empty<string>();
            public string[] errors = System.Array.Empty<string>();
            public ObjInfo obj = new();
        }

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();
            if (string.IsNullOrEmpty(req.path))
                return CommandResult.Failure("Required: path.");

            var scene = SceneManager.GetActiveScene();
            var go = SceneLookup.FindByPath(req.path, scene);
            if (go == null)
                return CommandResult.Failure($"GameObject not found: '{req.path}'.");

            Undo.RecordObject(go, "AI Bridge: modify");
            Undo.RecordObject(go.transform, "AI Bridge: modify");

            var t = go.transform;
            var rt = t as RectTransform;
            var applied = new List<string>();
            var errors = new List<string>();

            foreach (var op in req.ops)
            {
                try
                {
                    switch (op.property)
                    {
                        case "name": go.name = op.value; break;
                        case "active": go.SetActive(bool.Parse(op.value)); break;
                        case "position": t.position = V3(op.value); break;
                        case "localPosition": t.localPosition = V3(op.value); break;
                        case "rotation": t.eulerAngles = V3(op.value); break;
                        case "localRotation": t.localEulerAngles = V3(op.value); break;
                        case "scale":
                        case "localScale": t.localScale = V3(op.value); break;
                        case "anchoredPosition": rt = RequireRect(rt); rt.anchoredPosition = V2(op.value); break;
                        case "sizeDelta": rt = RequireRect(rt); rt.sizeDelta = V2(op.value); break;
                        case "anchorMin": rt = RequireRect(rt); rt.anchorMin = V2(op.value); break;
                        case "anchorMax": rt = RequireRect(rt); rt.anchorMax = V2(op.value); break;
                        case "pivot": rt = RequireRect(rt); rt.pivot = V2(op.value); break;
                        default: errors.Add($"Unknown property '{op.property}'"); continue;
                    }
                    applied.Add($"{op.property}={op.value}");
                }
                catch (System.Exception e)
                {
                    errors.Add($"{op.property}: {e.Message}");
                }
            }

            EditorUtility.SetDirty(go);
            if (!EditorApplication.isPlaying)
                EditorSceneManager.MarkSceneDirty(scene);

            var result = new Result
            {
                applied = applied.ToArray(),
                errors = errors.ToArray(),
                obj = SceneObjectDescriber.Describe(go.transform)
            };
            return CommandResult.Success(JsonUtility.ToJson(result));
        }

        static RectTransform RequireRect(RectTransform? rt)
            => rt != null ? rt : throw new System.Exception("not a RectTransform (UI) object");

        static float F(string s) => float.Parse(s.Trim(), CultureInfo.InvariantCulture);

        static Vector3 V3(string s)
        {
            var p = s.Split(',');
            return new Vector3(F(p[0]), F(p[1]), p.Length > 2 ? F(p[2]) : 0f);
        }

        static Vector2 V2(string s)
        {
            var p = s.Split(',');
            return new Vector2(F(p[0]), F(p[1]));
        }
    }
}

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
    /// Sets serialized value fields on a component (numbers, bools, strings, colors, vectors, enums) via
    /// an ops list. For object-reference fields use <c>reference.wire</c> instead. Records Undo.
    /// </summary>
    public class ComponentSetHandler : ICommandHandler
    {
        public string Command => "component.set";

        [System.Serializable] class Op { public string field = ""; public string value = ""; }

        [System.Serializable]
        class Request
        {
            public string path = "";
            public string component = "";
            public int componentIndex;
            public Op[] ops = System.Array.Empty<Op>();
        }

        [System.Serializable]
        class Result
        {
            public string[] applied = System.Array.Empty<string>();
            public string[] errors = System.Array.Empty<string>();
        }

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();
            if (string.IsNullOrEmpty(req.path) || string.IsNullOrEmpty(req.component))
                return CommandResult.Failure("Required: path, component.");

            var scene = SceneManager.GetActiveScene();
            var go = SceneLookup.FindByPath(req.path, scene);
            if (go == null)
                return CommandResult.Failure($"GameObject not found: '{req.path}'.");

            var comp = SceneLookup.GetComponent(go, req.component, req.componentIndex);
            if (comp == null)
                return CommandResult.Failure($"Component '{req.component}' not found on '{req.path}'.");

            var so = new SerializedObject(comp);
            var applied = new List<string>();
            var errors = new List<string>();
            foreach (var op in req.ops)
            {
                var prop = so.FindProperty(op.field);
                if (prop == null) { errors.Add($"{op.field}: not found"); continue; }
                try { Apply(prop, op.value); applied.Add($"{op.field}={op.value}"); }
                catch (System.Exception e) { errors.Add($"{op.field}: {e.Message}"); }
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(comp);
            if (!EditorApplication.isPlaying)
                EditorSceneManager.MarkSceneDirty(scene);

            return CommandResult.Success(JsonUtility.ToJson(new Result
            {
                applied = applied.ToArray(),
                errors = errors.ToArray()
            }));
        }

        static void Apply(SerializedProperty p, string v)
        {
            switch (p.propertyType)
            {
                case SerializedPropertyType.Boolean: p.boolValue = bool.Parse(v); break;
                case SerializedPropertyType.Integer: p.intValue = int.Parse(v, CultureInfo.InvariantCulture); break;
                case SerializedPropertyType.Float: p.floatValue = F(v); break;
                case SerializedPropertyType.String: p.stringValue = v; break;
                case SerializedPropertyType.Color: p.colorValue = Col(v); break;
                case SerializedPropertyType.Vector2: { var a = Floats(v); p.vector2Value = new Vector2(a[0], a[1]); break; }
                case SerializedPropertyType.Vector3: { var a = Floats(v); p.vector3Value = new Vector3(a[0], a[1], a.Length > 2 ? a[2] : 0); break; }
                case SerializedPropertyType.Vector4: { var a = Floats(v); p.vector4Value = new Vector4(a[0], a[1], a.Length > 2 ? a[2] : 0, a.Length > 3 ? a[3] : 0); break; }
                case SerializedPropertyType.Enum:
                    if (int.TryParse(v, out var iv)) p.intValue = iv;
                    else
                    {
                        var idx = System.Array.IndexOf(p.enumNames, v);
                        if (idx < 0) throw new System.Exception("enum name not found");
                        p.enumValueIndex = idx;
                    }
                    break;
                default:
                    throw new System.Exception($"unsupported type {p.propertyType} (use reference.wire for object refs)");
            }
        }

        static float F(string s) => float.Parse(s.Trim(), CultureInfo.InvariantCulture);
        static float[] Floats(string s) { var parts = s.Split(','); var r = new float[parts.Length]; for (var i = 0; i < parts.Length; i++) r[i] = F(parts[i]); return r; }
        static Color Col(string s) { var a = Floats(s); return new Color(a[0], a[1], a[2], a.Length > 3 ? a[3] : 1f); }
    }
}

#nullable enable
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AIBridge.Editor.Core
{
    [System.Serializable]
    public class Vec3
    {
        public float x, y, z;
        public Vec3() { }
        public Vec3(Vector3 v) { x = v.x; y = v.y; z = v.z; }
    }

    [System.Serializable]
    public class Vec2
    {
        public float x, y;
        public Vec2() { }
        public Vec2(Vector2 v) { x = v.x; y = v.y; }
    }

    [System.Serializable]
    public class RectInfo
    {
        public Vec2 anchoredPosition = new();
        public Vec2 sizeDelta = new();
        public Vec2 anchorMin = new();
        public Vec2 anchorMax = new();
        public Vec2 pivot = new();
    }

    [System.Serializable]
    public class ObjInfo
    {
        public string name = "";
        public string path = "";
        public string entityId = "";
        public bool active;
        public Vec3 worldPosition = new();
        public bool isUI;
        public RectInfo? rect;
        public string[] components = System.Array.Empty<string>();
    }

    /// <summary>
    /// Shared GameObject → data conversion. Used by both scene.dump and selection.get so the
    /// object shape stays identical everywhere (no duplicated describe logic).
    /// </summary>
    public static class SceneObjectDescriber
    {
        public static ObjInfo Describe(Transform t)
        {
            var go = t.gameObject;
            var info = new ObjInfo
            {
                name = go.name,
                path = GetPath(t),
                entityId = go.GetEntityId().ToString(), // GetInstanceID() is obsolete in Unity 6.5+
                active = go.activeInHierarchy,
                worldPosition = new Vec3(t.position),
                components = go.GetComponents<Component>()
                    .Where(c => c != null)
                    .Select(c => c.GetType().Name)
                    .ToArray()
            };

            if (t is RectTransform rt)
            {
                info.isUI = true;
                info.rect = new RectInfo
                {
                    anchoredPosition = new Vec2(rt.anchoredPosition),
                    sizeDelta = new Vec2(rt.sizeDelta),
                    anchorMin = new Vec2(rt.anchorMin),
                    anchorMax = new Vec2(rt.anchorMax),
                    pivot = new Vec2(rt.pivot)
                };
            }

            return info;
        }

        public static string GetPath(Transform t)
        {
            var stack = new Stack<string>();
            for (var c = t; c != null; c = c.parent)
                stack.Push(c.name);
            return string.Join("/", stack);
        }
    }
}

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace AIBridge.Editor.Core
{
    /// <summary>
    /// Resolves a component <see cref="Type"/> from a simple name ("Rigidbody2D") or full name
    /// ("UnityEngine.UI.Image") across all loaded assemblies. Prefers UnityEngine types on ambiguity.
    /// </summary>
    public static class ComponentTypeResolver
    {
        static List<Type>? _cache;

        static List<Type> AllComponentTypes()
        {
            if (_cache != null)
                return _cache;

            var list = new List<Type>();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray()!; }

                foreach (var t in types)
                    if (t != null && !t.IsAbstract && typeof(Component).IsAssignableFrom(t))
                        list.Add(t);
            }
            _cache = list;
            return list;
        }

        public static Type? Resolve(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            var all = AllComponentTypes();

            var byFull = all.FirstOrDefault(t => t.FullName == name);
            if (byFull != null)
                return byFull;

            var byName = all.Where(t => t.Name == name).ToList();
            if (byName.Count == 0)
                return null;
            if (byName.Count == 1)
                return byName[0];

            return byName.FirstOrDefault(t => t.Namespace != null && t.Namespace.StartsWith("UnityEngine"))
                   ?? byName[0];
        }
    }
}

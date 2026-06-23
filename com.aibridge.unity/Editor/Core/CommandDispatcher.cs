#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace AIBridge.Editor.Core
{
    /// <summary>
    /// Routes a request to the matching <see cref="ICommandHandler"/>. No giant switch:
    /// handlers self-register by simply implementing the interface (discovered via reflection).
    /// </summary>
    public class CommandDispatcher
    {
        readonly Dictionary<string, ICommandHandler> _handlers = new();

        public CommandDispatcher() => Collect();

        public IReadOnlyCollection<string> Commands => _handlers.Keys;

        void Collect()
        {
            var contract = typeof(ICommandHandler);
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = asm.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types.Where(t => t != null).ToArray()!;
                }

                foreach (var t in types)
                {
                    if (t == null || t.IsAbstract || t.IsInterface)
                        continue;
                    if (!contract.IsAssignableFrom(t))
                        continue;
                    if (t.GetConstructor(Type.EmptyTypes) == null)
                        continue;

                    var handler = (ICommandHandler)Activator.CreateInstance(t)!;
                    _handlers[handler.Command] = handler;
                }
            }
        }

        /// <summary>Parse the envelope, route to a handler, and build the full response JSON string.</summary>
        public string Handle(string rawJson)
        {
            RequestEnvelope env;
            try
            {
                env = JsonUtility.FromJson<RequestEnvelope>(rawJson) ?? new RequestEnvelope();
            }
            catch (Exception e)
            {
                return Build("unknown", "unknown", CommandResult.Failure("Malformed request JSON: " + e.Message));
            }

            if (string.IsNullOrEmpty(env.command))
                return Build(env.id, env.command, CommandResult.Failure("Missing 'command'."));

            if (!_handlers.TryGetValue(env.command, out var handler))
                return Build(env.id, env.command,
                    CommandResult.Failure($"Unknown command '{env.command}'. Known: {string.Join(", ", _handlers.Keys)}"));

            CommandResult result;
            try
            {
                result = handler.Execute(rawJson);
            }
            catch (Exception e)
            {
                result = CommandResult.Failure(e.GetType().Name + ": " + e.Message);
            }

            return Build(env.id, env.command, result);
        }

        static string Build(string id, string command, CommandResult r)
        {
            var resultPart = r.ResultJson ?? "null";
            var errorPart = r.Error == null ? "null" : "\"" + Json.Escape(r.Error) + "\"";
            return "{"
                 + "\"id\":\"" + Json.Escape(id) + "\","
                 + "\"command\":\"" + Json.Escape(command) + "\","
                 + "\"ok\":" + (r.Ok ? "true" : "false") + ","
                 + "\"error\":" + errorPart + ","
                 + "\"result\":" + resultPart
                 + "}";
        }
    }
}

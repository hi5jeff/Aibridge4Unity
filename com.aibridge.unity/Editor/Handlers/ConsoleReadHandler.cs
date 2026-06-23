#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using AIBridge.Editor.Core;
using UnityEngine;

namespace AIBridge.Editor.Handlers
{
    /// <summary>
    /// Reads the Editor's actual Console window via the internal <c>LogEntries</c> API. Unlike the
    /// callback-based <c>console.get</c>, this survives domain reloads, so it reliably returns the compile
    /// errors/warnings shown in the Console after a recompile.
    /// NOTE: this is a deliberate, isolated use of reflection against an internal API — the only reliable
    /// way to read the persistent Console (the same approach Unity's own tooling and MCP plugins use).
    /// </summary>
    public class ConsoleReadHandler : ICommandHandler
    {
        public string Command => "console.read";

        [Serializable] class Request { public int max = 100; public string typeFilter = ""; } // Error/Warning/Log/""
        [Serializable] class Entry { public string type = ""; public string message = ""; }
        [Serializable] class Result { public int count; public int errors; public int warnings; public Entry[] entries = Array.Empty<Entry>(); }

        // UnityEditor ConsoleWindow.Mode bit flags used to classify an entry.
        const int ErrorBits = (1 << 0) | (1 << 1) | (1 << 4) | (1 << 6) | (1 << 8) | (1 << 11) | (1 << 13) | (1 << 21);
        const int WarningBits = (1 << 7) | (1 << 9) | (1 << 12);

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();
            try
            {
                var asm = typeof(UnityEditor.Editor).Assembly;
                var logEntries = asm.GetType("UnityEditor.LogEntries") ?? asm.GetType("UnityEditorInternal.LogEntries");
                var logEntry = asm.GetType("UnityEditor.LogEntry") ?? asm.GetType("UnityEditorInternal.LogEntry");
                if (logEntries == null || logEntry == null)
                    return CommandResult.Failure("LogEntries API not found on this Unity version.");

                const BindingFlags sFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                const BindingFlags iFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                var start = logEntries.GetMethod("StartGettingEntries", sFlags);
                var endM = logEntries.GetMethod("EndGettingEntries", sFlags);
                var getEntry = logEntries.GetMethod("GetEntryInternal", sFlags);
                var msgField = logEntry.GetField("message", iFlags);
                var modeField = logEntry.GetField("mode", iFlags);
                if (start == null || endM == null || getEntry == null || msgField == null || modeField == null)
                    return CommandResult.Failure("LogEntries reflection members missing.");

                var count = (int)(start.Invoke(null, null) ?? 0);
                var entryObj = Activator.CreateInstance(logEntry);
                var all = new List<Entry>();
                int errors = 0, warnings = 0;
                try
                {
                    var args = new object[2];
                    for (var i = 0; i < count; i++)
                    {
                        args[0] = i; args[1] = entryObj!;
                        getEntry.Invoke(null, args);
                        var msg = (string)(msgField.GetValue(entryObj) ?? "");
                        var mode = (int)(modeField.GetValue(entryObj) ?? 0);
                        var type = (mode & ErrorBits) != 0 ? "Error" : (mode & WarningBits) != 0 ? "Warning" : "Log";
                        if (type == "Error") errors++; else if (type == "Warning") warnings++;
                        all.Add(new Entry { type = type, message = msg });
                    }
                }
                finally { endM.Invoke(null, null); }

                var filtered = new List<Entry>();
                foreach (var e in all)
                    if (string.IsNullOrEmpty(req.typeFilter) || string.Equals(e.type, req.typeFilter, StringComparison.OrdinalIgnoreCase))
                        filtered.Add(e);
                var max = req.max < 1 ? 100 : req.max;
                if (filtered.Count > max)
                    filtered = filtered.GetRange(filtered.Count - max, max);

                return CommandResult.Success(JsonUtility.ToJson(new Result
                {
                    count = filtered.Count,
                    errors = errors,
                    warnings = warnings,
                    entries = filtered.ToArray()
                }));
            }
            catch (Exception e)
            {
                return CommandResult.Failure("console.read failed: " + e.Message);
            }
        }
    }
}

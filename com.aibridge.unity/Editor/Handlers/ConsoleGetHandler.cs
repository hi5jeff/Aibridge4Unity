#nullable enable
using System;
using System.Collections.Generic;
using AIBridge.Editor.Core;
using UnityEngine;

namespace AIBridge.Editor.Handlers
{
    /// <summary>Returns recent Unity Console logs (script/runtime), so the AI can debug without the user
    /// reading them out. Filter by type, age, and count.</summary>
    public class ConsoleGetHandler : ICommandHandler
    {
        public string Command => "console.get";

        [System.Serializable]
        class Request
        {
            public int maxEntries = 100;
            public string logTypeFilter = "";   // "", Log, Warning, Error, Exception, Assert
            public int lastMinutes = 0;          // 0 = no time filter
            public bool includeStackTrace = false;
        }

        [System.Serializable]
        class Item
        {
            public string type = "";
            public string message = "";
            public string stackTrace = "";
            public string time = "";
        }

        [System.Serializable]
        class Result
        {
            public int count;
            public Item[] entries = System.Array.Empty<Item>();
        }

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();
            var max = Mathf.Max(1, req.maxEntries);
            var all = LogCollector.Snapshot();

            var cutoff = req.lastMinutes > 0 ? DateTime.Now.AddMinutes(-req.lastMinutes) : DateTime.MinValue;
            var typeFilter = req.logTypeFilter?.Trim() ?? "";

            var filtered = new List<Item>();
            foreach (var e in all)
            {
                if (req.lastMinutes > 0 && e.time < cutoff) continue;
                if (!string.IsNullOrEmpty(typeFilter) && !string.Equals(e.type, typeFilter, StringComparison.OrdinalIgnoreCase)) continue;
                filtered.Add(new Item
                {
                    type = e.type,
                    message = e.message,
                    stackTrace = req.includeStackTrace ? e.stackTrace : "",
                    time = e.time.ToString("o")
                });
            }

            // Keep the most recent `max`.
            if (filtered.Count > max)
                filtered.RemoveRange(0, filtered.Count - max);

            return CommandResult.Success(JsonUtility.ToJson(new Result
            {
                count = filtered.Count,
                entries = filtered.ToArray()
            }));
        }
    }
}

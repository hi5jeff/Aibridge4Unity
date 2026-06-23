#nullable enable
using System;
using System.Reflection;
using AIBridge.Editor.Core;

namespace AIBridge.Editor.Handlers
{
    /// <summary>Clears the Editor Console (via internal LogEntries) and the local log buffer — handy to wipe
    /// stale errors before an operation so the next console.read/console.get is clean.</summary>
    public class ConsoleClearHandler : ICommandHandler
    {
        public string Command => "console.clear";

        public CommandResult Execute(string rawJson)
        {
            try
            {
                var asm = typeof(UnityEditor.Editor).Assembly;
                var logEntries = asm.GetType("UnityEditor.LogEntries") ?? asm.GetType("UnityEditorInternal.LogEntries");
                var clear = logEntries?.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (clear == null) return CommandResult.Failure("LogEntries.Clear not found.");
                clear.Invoke(null, null);

                LogCollector.Clear();
                return CommandResult.Success("{\"cleared\":true}");
            }
            catch (Exception e)
            {
                return CommandResult.Failure("console.clear failed: " + e.Message);
            }
        }
    }
}

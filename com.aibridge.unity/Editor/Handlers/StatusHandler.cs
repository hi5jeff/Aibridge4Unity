#nullable enable
using AIBridge.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace AIBridge.Editor.Handlers
{
    /// <summary>Reports editor busy-state and the last compile's messages — poll this after `refresh`.</summary>
    public class StatusHandler : ICommandHandler
    {
        public string Command => "status";

        [System.Serializable]
        class Msg { public string type = ""; public string assembly = ""; public string message = ""; public string file = ""; public int line; }

        [System.Serializable]
        class Result
        {
            public bool isCompiling;
            public bool isUpdating;
            public bool isPlaying;
            public bool isPaused;
            public bool lastCompileHadErrors;
            public int messageCount;
            public Msg[] compileMessages = System.Array.Empty<Msg>();
        }

        public CommandResult Execute(string rawJson)
        {
            var raw = EditorStatus.Messages();
            var msgs = new Msg[raw.Length];
            for (var i = 0; i < raw.Length; i++)
                msgs[i] = new Msg { type = raw[i].type, assembly = raw[i].assembly, message = raw[i].message, file = raw[i].file, line = raw[i].line };

            var result = new Result
            {
                isCompiling = EditorApplication.isCompiling,
                isUpdating = EditorApplication.isUpdating,
                isPlaying = EditorApplication.isPlaying,
                isPaused = EditorApplication.isPaused,
                lastCompileHadErrors = EditorStatus.LastCompileHadErrors,
                messageCount = msgs.Length,
                compileMessages = msgs
            };
            return CommandResult.Success(JsonUtility.ToJson(result));
        }
    }
}

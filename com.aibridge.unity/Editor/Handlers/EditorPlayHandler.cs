#nullable enable
using AIBridge.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace AIBridge.Editor.Handlers
{
    /// <summary>
    /// Controls Play mode (enter / exit / toggle / pause / resume / step), so the AI can run the game and
    /// observe it (e.g. with screenshot.gameview). Transitions are async — poll `status` for `isPlaying`.
    /// </summary>
    public class EditorPlayHandler : ICommandHandler
    {
        public string Command => "editor.play";

        [System.Serializable] class Request { public string action = "toggle"; } // enter|exit|toggle|pause|resume|step
        [System.Serializable] class Result { public bool isPlaying; public bool isPaused; public string action = ""; public string note = ""; }

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();
            var a = (req.action ?? "toggle").ToLowerInvariant();
            var note = "";

            switch (a)
            {
                case "enter":
                    if (!EditorApplication.isPlaying) EditorApplication.EnterPlaymode();
                    note = "Entering Play mode (async — poll 'status' until isPlaying=true).";
                    break;
                case "exit":
                    if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
                    note = "Exiting Play mode (async).";
                    break;
                case "toggle":
                    EditorApplication.isPlaying = !EditorApplication.isPlaying;
                    note = "Toggled Play mode (async).";
                    break;
                case "pause":
                    EditorApplication.isPaused = true;
                    break;
                case "resume":
                case "unpause":
                    EditorApplication.isPaused = false;
                    break;
                case "step":
                    EditorApplication.Step();
                    break;
                default:
                    return CommandResult.Failure($"Unknown action '{req.action}'. Use enter/exit/toggle/pause/resume/step.");
            }

            return CommandResult.Success(JsonUtility.ToJson(new Result
            {
                isPlaying = EditorApplication.isPlaying,
                isPaused = EditorApplication.isPaused,
                action = a,
                note = note
            }));
        }
    }
}

#nullable enable
using AIBridge.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace AIBridge.Editor.Handlers
{
    /// <summary>Executes a Unity Editor menu item by path (e.g. "Tools/AI Bridge/Configure Claude Code").</summary>
    public class MenuExecuteHandler : ICommandHandler
    {
        public string Command => "menu.execute";

        [System.Serializable] class Request { public string menuPath = ""; }
        [System.Serializable] class Result { public string menuPath = ""; public bool executed; }

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();
            if (string.IsNullOrEmpty(req.menuPath))
                return CommandResult.Failure("Required: menuPath.");

            var ok = EditorApplication.ExecuteMenuItem(req.menuPath);
            return CommandResult.Success(JsonUtility.ToJson(new Result { menuPath = req.menuPath, executed = ok }));
        }
    }
}

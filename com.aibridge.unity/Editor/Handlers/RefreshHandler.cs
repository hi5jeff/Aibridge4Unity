#nullable enable
using AIBridge.Editor.Core;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace AIBridge.Editor.Handlers
{
    /// <summary>
    /// Triggers an AssetDatabase refresh (imports new/changed files and compiles changed scripts) and
    /// optionally forces a full recompile. Lets the AI compile its own script edits — no need for the
    /// user to focus Unity. Poll `status` afterwards until `isCompiling` is false.
    /// </summary>
    public class RefreshHandler : ICommandHandler
    {
        public string Command => "refresh";

        [System.Serializable]
        class Request
        {
            public bool save;          // save dirty assets/scenes first
            public bool recompile;     // force a full script recompile (Refresh already recompiles changes)
        }

        [System.Serializable]
        class Result
        {
            public bool wasCompiling;
            public string note = "";
        }

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();

            if (req.save)
                AssetDatabase.SaveAssets();

            AssetDatabase.Refresh();

            if (req.recompile)
                CompilationPipeline.RequestScriptCompilation();

            return CommandResult.Success(JsonUtility.ToJson(new Result
            {
                wasCompiling = EditorApplication.isCompiling,
                note = "Refresh triggered. Poll 'status' until isCompiling=false, then check compileMessages."
            }));
        }
    }
}

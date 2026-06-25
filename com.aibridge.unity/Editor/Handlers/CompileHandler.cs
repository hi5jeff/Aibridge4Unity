#nullable enable
using AIBridge.Editor.Core;
using UnityEditor;
using UnityEditor.Compilation;

namespace AIBridge.Editor.Handlers
{
    /// <summary>
    /// Forces a script recompile. Unlike an asset `refresh` (which Unity often defers until the Editor
    /// window regains focus), this calls <see cref="CompilationPipeline.RequestScriptCompilation"/>
    /// directly, so compilation proceeds even while the Editor is unfocused (e.g. driven head-lessly via
    /// the bridge). Returns immediately — poll <c>status</c> for <c>isCompiling</c>/<c>lastCompileHadErrors</c>
    /// and read <c>console.read</c> for errors.
    /// </summary>
    public class CompileHandler : ICommandHandler
    {
        public string Command => "compile";

        public CommandResult Execute(string rawJson)
        {
            // Pick up any changed/added scripts on disk, then explicitly request compilation.
            AssetDatabase.Refresh();
            CompilationPipeline.RequestScriptCompilation();
            return CommandResult.Success("{\"requested\":true}");
        }
    }
}

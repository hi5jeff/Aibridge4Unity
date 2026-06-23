#nullable enable
namespace AIBridge.Editor.Core
{
    /// <summary>
    /// One capability = one handler = one file. Implementations are auto-discovered by
    /// <see cref="CommandDispatcher"/> via reflection, so adding a tool never touches a central switch.
    /// </summary>
    public interface ICommandHandler
    {
        /// <summary>Unique command id, e.g. "scene.dump". Matched against the request envelope's `command`.</summary>
        string Command { get; }

        /// <summary>
        /// Runs on the Unity main thread. <paramref name="rawJson"/> is the full request JSON;
        /// parse your own typed args from it (request fields live at the top level).
        /// </summary>
        CommandResult Execute(string rawJson);
    }
}

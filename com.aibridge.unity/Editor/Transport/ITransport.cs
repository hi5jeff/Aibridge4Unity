#nullable enable
using AIBridge.Editor.Core;

namespace AIBridge.Editor.Transport
{
    /// <summary>
    /// Abstracts the request/response channel. File-based today; could become HTTP, a named pipe,
    /// or a socket later without touching handlers or the dispatcher.
    /// </summary>
    public interface ITransport
    {
        /// <summary>Pick up any pending requests and dispatch them. Always called on the main thread.</summary>
        void Poll(CommandDispatcher dispatcher);
    }
}

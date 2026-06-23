#nullable enable
using AIBridge.Editor.Core;
using UnityEngine;

namespace AIBridge.Editor.Handlers
{
    /// <summary>Trivial liveness check — proves the full request → dispatch → response loop works.</summary>
    public class PingHandler : ICommandHandler
    {
        public string Command => "ping";

        [System.Serializable]
        class Result
        {
            public string message = "";
            public string unityVersion = "";
        }

        public CommandResult Execute(string rawJson)
        {
            var r = new Result
            {
                message = LocalizationService.Get("ping.ok"),
                unityVersion = Application.unityVersion
            };
            return CommandResult.Success(JsonUtility.ToJson(r));
        }
    }
}

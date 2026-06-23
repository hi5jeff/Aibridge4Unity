#nullable enable
using System.Collections.Generic;
using AIBridge.Editor.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AIBridge.Editor.Handlers
{
    /// <summary>
    /// Dumps the active scene as structured data with precise coordinates and UI anchors, so the AI
    /// reads where things are instead of guessing from pixels. The core fix for "explain the position N times".
    /// </summary>
    public class SceneDumpHandler : ICommandHandler
    {
        public string Command => "scene.dump";

        // Request fields live at the top level of the request JSON.
        [System.Serializable]
        class Request
        {
            public bool includeInactive = true;
            public int maxObjects = 1000;
        }

        [System.Serializable]
        class Result
        {
            public string scene = "";
            public int count;
            public bool truncated;
            public ObjInfo[] objects = System.Array.Empty<ObjInfo>();
        }

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
                return CommandResult.Failure("No valid active scene.");

            var list = new List<ObjInfo>();
            var truncated = false;

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(req.includeInactive))
                {
                    if (list.Count >= req.maxObjects)
                    {
                        truncated = true;
                        break;
                    }
                    list.Add(SceneObjectDescriber.Describe(t));
                }
                if (truncated)
                    break;
            }

            var result = new Result
            {
                scene = scene.name,
                count = list.Count,
                truncated = truncated,
                objects = list.ToArray()
            };
            return CommandResult.Success(JsonUtility.ToJson(result));
        }
    }
}

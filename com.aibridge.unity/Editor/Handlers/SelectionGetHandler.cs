#nullable enable
using System.Collections.Generic;
using AIBridge.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace AIBridge.Editor.Handlers
{
    /// <summary>
    /// Returns the user's current Editor selection as data, so "this / these / the selected one" in chat
    /// resolves to concrete objects. The user points by selecting in Unity; the AI reads what was picked.
    /// </summary>
    public class SelectionGetHandler : ICommandHandler
    {
        public string Command => "selection.get";

        [System.Serializable]
        class Result
        {
            public int count;
            public ObjInfo[] objects = System.Array.Empty<ObjInfo>();
            public AssetInfo[] assets = System.Array.Empty<AssetInfo>();
            public string activeType = "";      // type of the active selection (e.g. GameObject, Material)
            public string activeAssetPath = "";  // asset path when an asset is selected; empty for scene objects
        }

        public CommandResult Execute(string rawJson)
        {
            var gameObjects = Selection.gameObjects;
            var list = new List<ObjInfo>(gameObjects.Length);
            foreach (var go in gameObjects)
                list.Add(SceneObjectDescriber.Describe(go.transform));

            var active = Selection.activeObject;
            var result = new Result
            {
                count = list.Count,
                objects = list.ToArray(),
                assets = AssetDescriber.CurrentSelection(),
                activeType = active != null ? active.GetType().Name : "",
                activeAssetPath = active != null ? AssetDatabase.GetAssetPath(active) : ""
            };
            return CommandResult.Success(JsonUtility.ToJson(result));
        }
    }
}

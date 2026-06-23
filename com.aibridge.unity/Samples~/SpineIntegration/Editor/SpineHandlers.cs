#nullable enable
using System.Collections.Generic;
using AIBridge.Editor.Core;
using Spine.Unity;
using Spine.Unity.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AIBridge.SpineIntegration
{
    /// <summary>Lists the animations and skins in a Spine SkeletonDataAsset.</summary>
    public class SpineListHandler : ICommandHandler
    {
        public string Command => "spine.list";

        [System.Serializable] class Request { public string assetPath = ""; }
        [System.Serializable] class Result { public string asset = ""; public string[] animations = System.Array.Empty<string>(); public string[] skins = System.Array.Empty<string>(); }

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();
            var sda = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(req.assetPath);
            if (sda == null) return CommandResult.Failure($"SkeletonDataAsset not found: '{req.assetPath}'.");

            var data = sda.GetSkeletonData(true);
            if (data == null) return CommandResult.Failure("Could not load skeleton data (likely a Spine version mismatch — re-export to match the runtime).");

            var anims = new List<string>();
            foreach (var a in data.Animations) anims.Add(a.Name);
            var skins = new List<string>();
            foreach (var s in data.Skins) skins.Add(s.Name);

            return CommandResult.Success(JsonUtility.ToJson(new Result { asset = req.assetPath, animations = anims.ToArray(), skins = skins.ToArray() }));
        }
    }

    /// <summary>Instantiates a Spine SkeletonAnimation from a SkeletonDataAsset, optionally playing an animation.</summary>
    public class SpineInstantiateHandler : ICommandHandler
    {
        public string Command => "spine.instantiate";

        [System.Serializable]
        class Request
        {
            public string assetPath = "";
            public string name = "";
            public string animation = "";
            public bool loop = true;
            public bool setPosition;
            public Vec3 position = new();
        }

        [System.Serializable] class Result { public ObjInfo created = new(); }

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();
            var sda = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(req.assetPath);
            if (sda == null) return CommandResult.Failure($"SkeletonDataAsset not found: '{req.assetPath}'.");

            var comps = EditorInstantiation.InstantiateSkeletonAnimation(sda);
            var skel = comps;
            if (skel == null) return CommandResult.Failure("Failed to create SkeletonAnimation.");

            var go = skel.gameObject;
            if (!string.IsNullOrEmpty(req.name)) go.name = req.name;

            var scene = SceneManager.GetActiveScene();
            if (req.setPosition)
                go.transform.position = new Vector3(req.position.x, req.position.y, req.position.z);

            if (!string.IsNullOrEmpty(req.animation))
            {
                skel.loop = req.loop;
                skel.AnimationName = req.animation;
            }

            Undo.RegisterCreatedObjectUndo(go, "AI Bridge: spine instantiate");
            EditorUtility.SetDirty(go);
            if (!EditorApplication.isPlaying) EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = go;

            return CommandResult.Success(JsonUtility.ToJson(new Result { created = SceneObjectDescriber.Describe(go.transform) }));
        }
    }

    /// <summary>Plays an animation on an existing Spine SkeletonAnimation in the scene.</summary>
    public class SpinePlayHandler : ICommandHandler
    {
        public string Command => "spine.play";

        [System.Serializable]
        class Request
        {
            public string path = "";
            public string animation = "";
            public bool loop = true;
            public int track;
        }

        [System.Serializable] class Result { public string path = ""; public string animation = ""; }

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();
            if (string.IsNullOrEmpty(req.path) || string.IsNullOrEmpty(req.animation))
                return CommandResult.Failure("Required: path, animation.");

            var go = SceneLookup.FindByPath(req.path, SceneManager.GetActiveScene());
            if (go == null) return CommandResult.Failure($"GameObject not found: '{req.path}'.");

            var skel = go.GetComponent<SkeletonAnimation>();
            if (skel == null) return CommandResult.Failure($"No SkeletonAnimation on '{req.path}'.");

            skel.loop = req.loop;
            skel.AnimationName = req.animation; // drives the edit-mode preview
            if (skel.AnimationState != null)
                skel.AnimationState.SetAnimation(req.track, req.animation, req.loop);

            EditorUtility.SetDirty(skel);
            return CommandResult.Success(JsonUtility.ToJson(new Result { path = req.path, animation = req.animation }));
        }
    }
}

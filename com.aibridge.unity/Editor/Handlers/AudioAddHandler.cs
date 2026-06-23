#nullable enable
using AIBridge.Editor.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AIBridge.Editor.Handlers
{
    /// <summary>Adds/configures an AudioSource on a GameObject and assigns an AudioClip by asset path.</summary>
    public class AudioAddHandler : ICommandHandler
    {
        public string Command => "audio.add";

        [System.Serializable]
        class Request
        {
            public string path = "";
            public string clip = "";          // AudioClip asset path (optional)
            public bool loop;
            public float volume = 1f;
            public bool playOnAwake = true;
            public float spatialBlend;         // 0 = 2D (default), 1 = 3D
        }

        [System.Serializable] class Result { public string path = ""; public string clip = ""; public bool loop; public float volume; }

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();
            if (string.IsNullOrEmpty(req.path))
                return CommandResult.Failure("Required: path.");

            var scene = SceneManager.GetActiveScene();
            var go = SceneLookup.FindByPath(req.path, scene);
            if (go == null) return CommandResult.Failure($"GameObject not found: '{req.path}'.");

            var src = go.GetComponent<AudioSource>();
            if (src == null) src = Undo.AddComponent<AudioSource>(go);

            if (!string.IsNullOrEmpty(req.clip))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(req.clip);
                if (clip == null) return CommandResult.Failure($"AudioClip not found: '{req.clip}'.");
                src.clip = clip;
            }

            src.loop = req.loop;
            src.volume = req.volume;
            src.playOnAwake = req.playOnAwake;
            src.spatialBlend = req.spatialBlend;

            EditorUtility.SetDirty(go);
            if (!EditorApplication.isPlaying)
                EditorSceneManager.MarkSceneDirty(scene);

            return CommandResult.Success(JsonUtility.ToJson(new Result
            {
                path = req.path,
                clip = src.clip != null ? src.clip.name : "",
                loop = src.loop,
                volume = src.volume
            }));
        }
    }
}

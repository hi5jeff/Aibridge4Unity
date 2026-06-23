#nullable enable
using System.Collections.Generic;
using System.IO;
using AIBridge.Editor.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AIBridge.Editor.Handlers
{
    static class SceneUtil
    {
        public static bool AnyDirty()
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) return true;
            return false;
        }

        public static void EnsureFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder)) return;
            var parent = Path.GetDirectoryName(folder)!.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        public static string? NormalizeScenePath(string path)
        {
            var p = path.Replace('\\', '/');
            if (!p.StartsWith("Assets/")) return null;
            if (!p.EndsWith(".unity")) p += ".unity";
            return p;
        }
    }

    /// <summary>Creates a new scene asset (single or additive) and saves it.</summary>
    public class SceneCreateHandler : ICommandHandler
    {
        public string Command => "scene.create";

        [System.Serializable] class Request { public string path = ""; public bool empty; public bool additive; public bool force; }
        [System.Serializable] class Result { public string path = ""; public bool opened; }

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();
            var path = SceneUtil.NormalizeScenePath(req.path);
            if (path == null) return CommandResult.Failure("path must be under 'Assets/' and end with '.unity'.");
            if (!req.additive && !req.force && SceneUtil.AnyDirty())
                return CommandResult.Failure("Open scene(s) have unsaved changes. Save first (scene.save) or pass force:true.");

            SceneUtil.EnsureFolder(Path.GetDirectoryName(path)!.Replace('\\', '/'));
            var setup = req.empty ? NewSceneSetup.EmptyScene : NewSceneSetup.DefaultGameObjects;
            var mode = req.additive ? NewSceneMode.Additive : NewSceneMode.Single;
            var scene = EditorSceneManager.NewScene(setup, mode);
            EditorSceneManager.SaveScene(scene, path);

            return CommandResult.Success(JsonUtility.ToJson(new Result { path = path, opened = true }));
        }
    }

    /// <summary>Opens an existing scene (single replaces the current scene; additive adds it).</summary>
    public class SceneOpenHandler : ICommandHandler
    {
        public string Command => "scene.open";

        [System.Serializable] class Request { public string path = ""; public bool additive; public bool force; }
        [System.Serializable] class Result { public string path = ""; public bool opened; }

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();
            var path = req.path.Replace('\\', '/');
            if (AssetDatabase.GetMainAssetTypeAtPath(path) == null)
                return CommandResult.Failure($"Scene not found: '{path}'.");
            if (!req.additive && !req.force && SceneUtil.AnyDirty())
                return CommandResult.Failure("Open scene(s) have unsaved changes. Save first (scene.save) or pass force:true.");

            EditorSceneManager.OpenScene(path, req.additive ? OpenSceneMode.Additive : OpenSceneMode.Single);
            return CommandResult.Success(JsonUtility.ToJson(new Result { path = path, opened = true }));
        }
    }

    /// <summary>Saves the active scene (to its path, or save-as to a new path).</summary>
    public class SceneSaveHandler : ICommandHandler
    {
        public string Command => "scene.save";

        [System.Serializable] class Request { public string path = ""; }
        [System.Serializable] class Result { public string savedTo = ""; }

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();
            var scene = SceneManager.GetActiveScene();

            if (!string.IsNullOrEmpty(req.path))
            {
                var path = SceneUtil.NormalizeScenePath(req.path);
                if (path == null) return CommandResult.Failure("path must be under 'Assets/' and end with '.unity'.");
                SceneUtil.EnsureFolder(Path.GetDirectoryName(path)!.Replace('\\', '/'));
                EditorSceneManager.SaveScene(scene, path);
                return CommandResult.Success(JsonUtility.ToJson(new Result { savedTo = path }));
            }

            if (string.IsNullOrEmpty(scene.path))
                return CommandResult.Failure("Active scene has no path yet — provide 'path' to save it.");
            EditorSceneManager.SaveScene(scene);
            return CommandResult.Success(JsonUtility.ToJson(new Result { savedTo = scene.path }));
        }
    }

    /// <summary>Lists the open scenes and all scenes in the project.</summary>
    public class SceneListHandler : ICommandHandler
    {
        public string Command => "scene.list";

        [System.Serializable] class Open { public string name = ""; public string path = ""; public bool active; public bool dirty; public bool loaded; }
        [System.Serializable] class Result { public Open[] open = System.Array.Empty<Open>(); public string[] project = System.Array.Empty<string>(); }

        public CommandResult Execute(string rawJson)
        {
            var open = new List<Open>();
            var active = SceneManager.GetActiveScene();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                open.Add(new Open { name = s.name, path = s.path, active = s == active, dirty = s.isDirty, loaded = s.isLoaded });
            }

            var project = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Scene"))
                project.Add(AssetDatabase.GUIDToAssetPath(guid));

            return CommandResult.Success(JsonUtility.ToJson(new Result { open = open.ToArray(), project = project.ToArray() }));
        }
    }

    /// <summary>Closes (unloads) an additively-loaded scene by path.</summary>
    public class SceneCloseHandler : ICommandHandler
    {
        public string Command => "scene.close";

        [System.Serializable] class Request { public string path = ""; }
        [System.Serializable] class Result { public string closed = ""; }

        public CommandResult Execute(string rawJson)
        {
            var req = JsonUtility.FromJson<Request>(rawJson) ?? new Request();
            if (SceneManager.sceneCount <= 1)
                return CommandResult.Failure("Cannot close the only open scene.");

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s.path == req.path || s.name == req.path)
                {
                    EditorSceneManager.CloseScene(s, true);
                    return CommandResult.Success(JsonUtility.ToJson(new Result { closed = req.path }));
                }
            }
            return CommandResult.Failure($"Open scene not found: '{req.path}'.");
        }
    }
}

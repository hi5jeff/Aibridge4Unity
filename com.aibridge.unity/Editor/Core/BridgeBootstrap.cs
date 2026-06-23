#nullable enable
using AIBridge.Editor.Core;
using AIBridge.Editor.Transport;
using UnityEditor;
using UnityEngine;

namespace AIBridge.Editor
{
    /// <summary>
    /// Entry point. Starts on Editor load, then pumps the transport from EditorApplication.update
    /// (the Unity main thread) at the configured interval.
    /// </summary>
    [InitializeOnLoad]
    public static class BridgeBootstrap
    {
        static CommandDispatcher? _dispatcher;
        static ITransport? _transport;
        static BridgeConfig? _config;
        static double _nextPoll;

        static BridgeBootstrap()
        {
            EditorApplication.update += Update;
        }

        static void EnsureInit()
        {
            if (_dispatcher != null)
                return;

            _config = BridgeConfig.Load();
            LocalizationService.SetLanguage(_config.language);
            _dispatcher = new CommandDispatcher();
            _transport = new FileTransport(_config.ResolveChannelRoot());

            Debug.Log($"[AIBridge] {LocalizationService.Get("bridge.ready")}\n" +
                      $"channel: {_config.ResolveChannelRoot()}\n" +
                      $"commands: {string.Join(", ", _dispatcher.Commands)}");
        }

        static void Update()
        {
            EnsureInit();

            if (_config is { enabled: false })
                return;

            if (EditorApplication.timeSinceStartup < _nextPoll)
                return;

            _nextPoll = EditorApplication.timeSinceStartup + Mathf.Max(0.05f, _config!.pollIntervalSeconds);
            _transport!.Poll(_dispatcher!);
        }

        [MenuItem("Tools/AI Bridge/Create Config Asset")]
        static void CreateConfig()
        {
            const string dir = "Assets/AIBridge";
            const string assetPath = dir + "/BridgeConfig.asset";

            if (AssetDatabase.LoadAssetAtPath<BridgeConfig>(assetPath) != null)
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<BridgeConfig>(assetPath);
                return;
            }

            System.IO.Directory.CreateDirectory(dir);
            var asset = ScriptableObject.CreateInstance<BridgeConfig>();
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            BridgeConfig.ClearCache();
            Selection.activeObject = asset;
        }
    }
}

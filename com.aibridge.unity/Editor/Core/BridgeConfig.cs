#nullable enable
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AIBridge.Editor.Core
{
    /// <summary>
    /// Every tunable parameter lives here — logic code reads from this, nothing is hardcoded.
    /// Create an asset via "Tools/AI Bridge/Create Config Asset", or the bridge runs on in-memory defaults.
    /// </summary>
    [CreateAssetMenu(fileName = "BridgeConfig", menuName = "AI Bridge/Config", order = 0)]
    public class BridgeConfig : ScriptableObject
    {
        [Tooltip("Channel root folder, relative to the project root (the folder that contains 'Assets'). " +
                 "Agents write requests to <root>/in and read responses from <root>/out.")]
        public string channelFolder = ".aibridge";

        [Tooltip("How often (seconds) the bridge scans for new requests.")]
        public float pollIntervalSeconds = 0.5f;

        [Tooltip("UI language for bridge messages. Must match a file in the package Localization folder (e.g. en, zh-CN).")]
        public string language = "en";

        [Tooltip("Master switch. When off, the bridge ignores all requests.")]
        public bool enabled = true;

        /// <summary>Absolute path of the channel root, derived from the project root + <see cref="channelFolder"/>.</summary>
        public string ResolveChannelRoot()
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.GetFullPath(Path.Combine(projectRoot!, channelFolder));
        }

        static BridgeConfig? _cached;

        /// <summary>Loads the first BridgeConfig asset in the project, or falls back to in-memory defaults.</summary>
        public static BridgeConfig Load()
        {
            if (_cached != null)
                return _cached;

            var guids = AssetDatabase.FindAssets("t:BridgeConfig");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _cached = AssetDatabase.LoadAssetAtPath<BridgeConfig>(path);
            }

            if (_cached == null)
                _cached = CreateInstance<BridgeConfig>(); // zero-config: works out of the box on defaults

            return _cached;
        }

        public static void ClearCache() => _cached = null;
    }
}

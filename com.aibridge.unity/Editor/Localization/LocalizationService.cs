#nullable enable
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AIBridge.Editor
{
    /// <summary>
    /// Loads localized bridge messages from the package's Localization folder. Code references keys only;
    /// all user-facing text lives in &lt;lang&gt;.json (add a language = add a file).
    /// </summary>
    public static class LocalizationService
    {
        [System.Serializable] class Entry { public string key = ""; public string value = ""; }
        [System.Serializable] class LocaleFile { public Entry[] entries = System.Array.Empty<Entry>(); }

        // Embedded-package virtual path; resolves to &lt;project&gt;/Packages/... during development.
        const string PackageLocaleDir = "Packages/com.aibridge.unity/Localization";

        static readonly Dictionary<string, string> _map = new();
        static string _lang = "en";

        public static void SetLanguage(string lang)
        {
            _lang = string.IsNullOrEmpty(lang) ? "en" : lang;
            _map.Clear();
            Load(_lang);
        }

        static void Load(string lang)
        {
            var path = Path.GetFullPath($"{PackageLocaleDir}/{lang}.json");
            if (!File.Exists(path))
            {
                path = Path.GetFullPath($"{PackageLocaleDir}/en.json"); // fall back to English
                if (!File.Exists(path))
                    return;
            }

            try
            {
                var file = JsonUtility.FromJson<LocaleFile>(File.ReadAllText(path));
                if (file?.entries == null)
                    return;
                foreach (var e in file.entries)
                    _map[e.key] = e.value;
            }
            catch
            {
                // Leave the map empty; Get() then returns the key itself.
            }
        }

        /// <summary>Returns the localized string for <paramref name="key"/>, or the key itself if missing.</summary>
        public static string Get(string key)
            => _map.TryGetValue(key, out var v) ? v : key;
    }
}

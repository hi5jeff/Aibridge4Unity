#nullable enable
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AIBridge.Editor
{
    /// <summary>Editor menu items for AI Bridge — one-click setup of the local AI agent.</summary>
    static class BridgeMenu
    {
        /// <summary>Copies the bundled unity-bridge skill into the project's .claude/skills so Claude Code
        /// knows how to drive the bridge — no manual file copying.</summary>
        [MenuItem("Tools/AI Bridge/Configure Claude Code")]
        static void ConfigureClaudeCode()
        {
            var src = ResolveSkillSource();
            if (src == null || !File.Exists(src))
            {
                Debug.LogError("[AIBridge] Could not locate the bundled SKILL.md (Documentation~/SKILL.md).");
                return;
            }

            var projectRoot = Path.GetDirectoryName(Application.dataPath)!;
            var dstDir = Path.Combine(projectRoot, ".claude", "skills", "unity-bridge");
            Directory.CreateDirectory(dstDir);
            var dst = Path.Combine(dstDir, "SKILL.md");
            File.Copy(src, dst, overwrite: true);

            Debug.Log($"[AIBridge] Claude Code configured — installed the unity-bridge skill to:\n{dst}\n" +
                      "Point Claude Code at this project and just ask it to inspect or change the scene.");
        }

        static string? ResolveSkillSource()
        {
            var pkg = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(BridgeMenu).Assembly);
            if (pkg != null)
            {
                var p = Path.Combine(pkg.resolvedPath, "Documentation~", "SKILL.md");
                if (File.Exists(p)) return p;
            }
            var fallback = Path.GetFullPath("Packages/com.aibridge.unity/Documentation~/SKILL.md");
            return File.Exists(fallback) ? fallback : null;
        }
    }
}

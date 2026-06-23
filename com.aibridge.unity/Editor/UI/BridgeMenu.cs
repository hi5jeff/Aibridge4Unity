#nullable enable
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AIBridge.Editor
{
    /// <summary>Editor menu items for AI Bridge — one-click setup of the local AI agent.</summary>
    static class BridgeMenu
    {
        const string BeginMarker = "<!-- AI-BRIDGE:BEGIN (managed by Tools > AI Bridge > Configure Claude Code) -->";
        const string EndMarker = "<!-- AI-BRIDGE:END -->";

        // Always-on instruction injected into the project's CLAUDE.md. Claude Code loads CLAUDE.md every
        // session, whereas skills are only auto-discovered heuristically (and not at all if the session
        // started before the skill was installed). This pointer guarantees the agent knows to drive Unity
        // through the file channel instead of reaching for screenshots / computer-use / an MCP server.
        const string Instructions =
            "## Unity — drive the Editor via AI Bridge\n\n" +
            "This project has the **AI Bridge** package installed. Whenever a task involves the Unity Editor —\n" +
            "inspecting or changing the scene, GameObjects, selection, components, UI, prefabs, sprites,\n" +
            "animation, the Console, or Play mode — drive Unity through the **file channel** at `.aibridge/`\n" +
            "as documented in the `unity-bridge` skill (`.claude/skills/unity-bridge/SKILL.md`).\n\n" +
            "Do **not** screenshot the screen, use computer-use, or search for a Unity MCP server — read and\n" +
            "control the real, open Editor through the `.aibridge` channel (write `in/<id>.json`, read `out/<id>.json`).";

        /// <summary>Copies the bundled unity-bridge skill into the project's .claude/skills AND writes an
        /// always-on pointer into CLAUDE.md, so Claude Code reliably drives the bridge — no manual setup.</summary>
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

            var claudeMd = EnsureBridgeInstructions(projectRoot);

            Debug.Log("[AIBridge] Claude Code configured:\n" +
                      $"  - skill              -> {dst}\n" +
                      $"  - CLAUDE.md pointer  -> {claudeMd}\n" +
                      "Restart (or start) Claude Code in this project so it picks up the skill, then just ask " +
                      "it to inspect or change the scene — it will use the .aibridge channel, not screenshots.");
        }

        /// <summary>Inserts (or refreshes) the AI Bridge instruction block in the project's CLAUDE.md.
        /// Idempotent: rewrites the content between the markers; appends the block if absent; creates the
        /// file if missing. User content outside the markers is never touched.</summary>
        static string EnsureBridgeInstructions(string projectRoot)
        {
            var path = Path.Combine(projectRoot, "CLAUDE.md");
            var block = BeginMarker + "\n" + Instructions + "\n" + EndMarker;

            var existing = File.Exists(path) ? File.ReadAllText(path) : "";

            var begin = existing.IndexOf(BeginMarker, System.StringComparison.Ordinal);
            var end = existing.IndexOf(EndMarker, System.StringComparison.Ordinal);
            string updated;
            if (begin >= 0 && end > begin)
            {
                // Replace the existing managed block in place — user edits elsewhere are preserved.
                updated = existing.Substring(0, begin) + block + existing.Substring(end + EndMarker.Length);
            }
            else if (existing.Length == 0)
            {
                updated = block + "\n";
            }
            else
            {
                var sep = existing.EndsWith("\n") ? "\n" : "\n\n";
                updated = existing + sep + block + "\n";
            }

            File.WriteAllText(path, updated);
            return path;
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

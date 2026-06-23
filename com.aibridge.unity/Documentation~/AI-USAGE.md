# Using AI Bridge with an AI agent

This is the agent-facing guide for driving Unity through AI Bridge. To make it a first-class skill
for a Claude session working in a project, copy it to `<project>/.claude/skills/unity-bridge/SKILL.md`
and add YAML frontmatter:

```yaml
---
name: unity-bridge
description: Drive the open Unity Editor via the AI Bridge file channel.
---
```

## Channel protocol

- Root: `<project>/.aibridge/`.
- **Send:** write `in/<id>.json` — `{ "id": "<unique>", "command": "<name>", ...fields }` (fields flat).
- **Receive:** read `out/<id>.json` — `{ "id", "command", "ok", "error", "result" }`.
- The bridge polls ~2×/sec. Write, wait ~1–2s, then read. Use a unique `id` per request.
- Editing project C# requires the user to focus Unity so it recompiles — the agent cannot trigger it.

## The user's pointer — `selection.json`

For "this / here / the selected one": read `.aibridge/selection.json` (pinned via the *AI Reference*
window — scene objects and/or Project assets, or a clicked point + note), or call `selection.get` for
the live selection. Selected assets appear under `assets[]` (with `assetPath`, usable as
`reference.wire` `targetAssetPath`).

## Commands

| Command | Fields | Returns |
|---|---|---|
| `ping` | — | `message`, `unityVersion` |
| `scene.dump` | `includeInactive?`, `maxObjects?` | scene objects: `path`, `worldPosition`, `rect` (if UI), `components` |
| `selection.get` | — | currently selected objects |
| `reference.wire` | `sourcePath`, `sourceComponent`, `field`, then ONE of: `targetPath`(+`targetComponent?`) / `targetAssetPath` | what was wired |
| `gameobject.create` | `name`, `parentPath?`, `primitive?`, `position?{x,y,z}`, `components?[]` | created object |
| `component.add` | `path`, `component` | updated component list |
| `object.modify` | `path`, `ops:[{property,value}]` | `applied`, `errors`, object state |
| `gameobject.delete` | `path` | deleted path |
| `screenshot.annotated` | `width?`, `height?`, `annotate?`, `maxLabels?` | `imagePath` (read it!), `objects[{screenRect, worldPosition}]` |

- **Paths** are `Root/Child/Leaf` within the active scene.
- `object.modify` properties: `name`, `active`, `position`, `localPosition`, `rotation` (euler),
  `localRotation`, `scale`; UI: `anchoredPosition`, `sizeDelta`, `anchorMin`, `anchorMax`, `pivot`.
  Vector values comma-separated (`"1,2,0"`).
- All edits register Undo.

## Recommended loop

1. **Orient:** `scene.dump` and/or `screenshot.annotated` (then read the PNG at `result.imagePath`).
2. **Resolve "this":** `selection.get` or read `selection.json`.
3. **Act:** create / add / wire / modify / delete.
4. **Verify:** `screenshot.annotated` again and read the PNG — look, don't assume.

Full field reference: the package `README.md`.

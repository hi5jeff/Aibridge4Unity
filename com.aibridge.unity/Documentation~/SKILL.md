---
name: unity-bridge
description: Drive the open Unity Editor from this session via the AI Bridge file channel (.aibridge/in,out) — read scene/selection as data, capture screenshots, build/modify/wire objects, lay out UGUI, animate, write & compile C#, run Play mode, and more. Use this WHENEVER the task touches Unity (scene, GameObjects, selection, components, UI, prefabs, sprites, animation, Console, Play mode) — do NOT take screenshots of the screen, use computer-use, or look for a Unity MCP server; control the real Editor through the .aibridge channel.
---

# Unity Bridge — driving the Unity Editor

> **⚠️ Building UI / presentation / animation / effects? FIRST read [`PRESENTATION_RULES.md`](./PRESENTATION_RULES.md)
> (next to this file) — hard rules: editable prefabs (no hardcoded coords), non-destructive prefab edits,
> native-size sprites, no git on the user's project, real Unity game-feel. Violating them costs rework.**

The **AI Bridge** package runs inside the open Unity Editor and exposes commands over a plain file
channel. You operate Unity by writing a request file and reading the reply. No MCP, no ports.

## Channel protocol

- Root: `<project>/.aibridge/` (here: `D:/AI/unity_test/testgame/.aibridge/`).
- **Send:** write `in/<id>.json` — `{ "id": "<unique>", "command": "<name>", ...fields }`.
- **Receive:** read `out/<id>.json` — `{ "id", "command", "ok", "error", "result" }`.
- Request fields are **flat** (top level), not nested under "args".
- The bridge polls ~2×/sec. After writing, wait ~1–2s, then read `out/<id>.json`.
- Use a unique `id` per request so replies never collide.

Bash idiom:
```bash
cd <project>/.aibridge
printf '{ "id":"x1", "command":"scene.dump" }' > in/x1.json
sleep 2 && cat out/x1.json
```

If `out/<id>.json` never appears: Unity may be recompiling or unfocused. Ask the user to focus
Unity, check the Console for `[AIBridge]`, and retry. **You cannot trigger a recompile yourself** —
after editing any C# in the project, ask the user to focus Unity so it compiles.

## The user's pointer — `selection.json`

When the user says "this / here / the selected one", they mean what they pointed at in Unity:
- **Pinned:** read `.aibridge/selection.json` (written by the *AI Reference* window — scene objects
  and/or Project-window assets, or a clicked point, with an optional `note`).
- **Live:** call `selection.get` for whatever is selected right now.

Selected **assets** (sprites, prefabs, materials…) appear under `assets[]` (with `assetPath`) in both
`selection.json` and `selection.get` — feed `assetPath` into `reference.wire` `targetAssetPath`.

## Commands

| Command | Fields | Returns |
|---|---|---|
| `ping` | — | `message`, `unityVersion` |
| `scene.dump` | `includeInactive?`, `maxObjects?` | scene objects: `path`, `worldPosition`, `rect` (if UI), `components` |
| `selection.get` | — | currently selected objects (same shape) |
| `reference.wire` | `sourcePath`, `sourceComponent`, `field`, then ONE of: `targetPath`(+`targetComponent?`) / `targetAssetPath` | what was wired |
| `gameobject.create` | `name`, `parentPath?`, `primitive?`, `position?{x,y,z}`, `components?[]` | created object |
| `component.add` | `path`, `component` | updated component list |
| `object.modify` | `path`, `ops:[{property,value}]` | `applied`, `errors`, object state |
| `gameobject.delete` | `path` | deleted path |
| `screenshot.annotated` | `width?`, `height?`, `annotate?`, `maxLabels?` | `imagePath` (read it!), `objects[{screenRect, worldPosition}]` |
| `screenshot.gameview` | `max?` | real Game View PNG incl. overlay UI (`imagePath` — read it). Use to *see* UI. |
| `console.get` | `maxEntries?`, `logTypeFilter?`, `lastMinutes?`, `includeStackTrace?` | recent Console logs (script/runtime; not compiler errors) |
| `console.read` | `max?`, `typeFilter?` | the real Console (errors/warnings), survives reloads — use for compile diagnostics |
| `console.clear` | — | clear the Console + log buffer (wipe stale errors before a fresh check) |
| `component.set` | `path`, `component`, `componentIndex?`, `ops:[{field,value}]` | set value fields (color/number/bool/string/vector/enum) |
| `gameobject.duplicate` | `path`, `name?`, `setPosition?`, `position?` | clone a GameObject (with components) |
| `prefab.create` | `path`, `assetPath` | save a GameObject as a prefab asset |
| `prefab.instantiate` | `assetPath`, `name?`, `parentPath?`, `setPosition?`, `position?` | spawn a prefab instance |
| `refresh` | `save?`, `recompile?` | import + recompile changed scripts (you can compile your own edits) |
| `editor.play` | `action` (enter/exit/toggle/pause/resume/step) | run the game; poll `status` for isPlaying, then `screenshot.gameview` |
| `scene.create` / `scene.open` / `scene.save` / `scene.list` / `scene.close` | `path`, `additive?`, `force?` (open/create); `path?` (save) | multi-scene management (single/additive; force discards unsaved) |
| `status` | — | `isCompiling`/`isPlaying`/… + last compile `compileMessages` (poll after refresh) |
| `menu.execute` | `menuPath` | run any Editor menu item by path (e.g. `Tools/AI Bridge/Configure Claude Code`) |
| `asset.find` | `filter`, `folder?`, `max?` | find assets by Unity search (`t:Sprite`, `t:Prefab`, …) |
| `asset.reimport` | `path` | force-reimport an asset/folder (regenerate importer-derived assets) |
| `sprite.set` | `path`, `assetPath`, `component?` | assign a sprite to a SpriteRenderer/Image (auto-imports as Sprite) |
| `audio.add` | `path`, `clip?`, `loop?`, `volume?`, `playOnAwake?`, `spatialBlend?` | add/configure an AudioSource + assign a clip |
| `particle.create` | `name?`, `parentPath?`, `position?`, `preset` (burst/explosion/stream/sparkle), `color?`, `size?`, `lifetime?`, `count?` | create a particle effect (plays in Play mode) |
| `ui.create` | `kind` (image/text/button/canvas), `name?`, `parentPath?`, `anchor?`, `x/y/width/height?`, `text?`, `fontSize?`, `color?`, `sprite?` | turnkey UGUI (auto Canvas + EventSystem) |

Notes:
- **Paths** are `Root/Child/Leaf` within the active scene (the shape `scene.dump` emits).
- `object.modify` properties: `name`, `active`, `position`, `localPosition`, `rotation` (euler),
  `localRotation`, `scale`; UI: `anchoredPosition`, `sizeDelta`, `anchorMin`, `anchorMax`, `pivot`.
  Vector values are comma-separated, e.g. `"1,2,0"`.
- `reference.wire` is for object-reference fields only; for primitive values use the Inspector/scripts.
- All edits register **Undo** (the user can Ctrl+Z).

## Recommended loop

1. **Orient** — `scene.dump` for data, and/or `screenshot.annotated` then **read the PNG** at
   `result.imagePath` to see the scene.
2. **Resolve "this"** — `selection.get` or read `selection.json`.
3. **Act** — `gameobject.create` / `component.add` / `reference.wire` / `object.modify` / `gameobject.delete`.
4. **Verify** — `screenshot.annotated` again and read the PNG to confirm the change looks right; don't
   assume — look.

Full field reference: `Packages/com.aibridge.unity/README.md`.

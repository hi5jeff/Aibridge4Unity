# AI Bridge for Unity

Gives AI agents **precise eyes and hands into the Unity presentation layer**: read scene/UI
state as exact data, capture annotated screenshots, and wire references programmatically —
the parts that are slow and error-prone to communicate in natural language.

It talks over a **plain file channel** (a watched folder). No MCP, no server binary, no open
ports, no network. If your agent can read and write files, it can drive Unity.

> Status: **0.8.0 — walking skeleton.** Commands: `ping`, `scene.dump`, `selection.get`,
> `reference.wire`, `gameobject.create`, `component.add`, `gameobject.delete`, `object.modify`,
> `screenshot.annotated`, `console.get`, plus a user-driven `selection.json` reference channel.

## Install

This is a local (embedded) package. With the project open in Unity 6 (6000.x), it is detected
automatically at `Packages/com.aibridge.unity/`. To reuse it in another project, copy the
folder into that project's `Packages/`, or reference it by git URL once published.

## How it works

```
AI agent (any tool that can read/write files)
   │  writes <project>/.aibridge/in/<id>.json
   │  reads  <project>/.aibridge/out/<id>.json
   ▼
AI Bridge (this package, runs inside the Unity Editor)
   • polls the channel on the main thread (EditorApplication.update)
   • routes each request to a handler, runs Unity API safely, writes the reply
```

The active build target, scene, and editor are the live ones — the bridge reads and edits the
real project.

## Channel protocol

**Request** — a `*.json` file written into `.aibridge/in/`. Fields are flat (top level):

```json
{ "id": "001", "command": "scene.dump", "includeInactive": true, "maxObjects": 500 }
```

**Response** — written to `.aibridge/out/<id>.json`:

```json
{ "id": "001", "command": "scene.dump", "ok": true, "error": null, "result": { } }
```

On failure, `ok` is `false` and `error` holds the message.

## Commands (0.4.0)

| Command | Purpose | Request fields |
|---|---|---|
| `ping` | Liveness / loop check | — |
| `scene.dump` | Active scene as data: paths, world positions, RectTransform anchors, component lists | `includeInactive` (bool), `maxObjects` (int) |
| `selection.get` | The user's current Editor selection as data — resolves "this / these / the selected one" | — |
| `reference.wire` | Set an object-reference field (auto-link), replacing manual Inspector drag-drop | see below |
| `gameobject.create` | Create a GameObject (empty or primitive), parented/positioned, with components | `name`, `parentPath`, `primitive`, `position`, `components[]` |
| `component.add` | Add a component by type name — the auto "Add Component" | `path`, `component` |
| `gameobject.delete` | Delete a GameObject (Undo-able) | `path` |
| `object.modify` | Move / rotate / scale / rename / (de)activate an existing object via an ops list | `path`, `ops[]` |
| `screenshot.annotated` | Offscreen render of the camera with bounding boxes + per-object screen rects | `width`, `height`, `annotate`, `maxLabels` |
| `console.get` | Recent Console logs (script/runtime) for debugging | `maxEntries`, `logTypeFilter`, `lastMinutes`, `includeStackTrace` |
| `component.set` | Set value fields (numbers, bools, strings, colors, vectors, enums) via an ops list | `path`, `component`, `componentIndex`, `ops[]` |
| `gameobject.duplicate` | Clone a GameObject (with components); optional rename/reposition | `path`, `name`, `setPosition`, `position` |
| `grid.create` | Build an N×N grid of square cell tiles in one call (`<prefix>_x_y`) | `size`, `cellSize`, `gap`, `color`, `parentName`, `namePrefix`, `sortingOrder`, `z` |
| `animation.create` | Keyframe animation: build clip + AnimatorController + attach Animator (plays in Play mode) | `path`, `name`, `loop`, `frameRate`, `channels[]` |
| `refresh` | Import new/changed files and recompile scripts (no need to focus Unity) | `save`, `recompile` |
| `status` | Editor busy-state (`isCompiling`/`isPlaying`/…) + last compile's messages — poll after `refresh` | — |

### `object.modify`

Only the properties you list change. Vectors are comma-separated; `ops` is `{property, value}` pairs:

```json
{ "command": "object.modify", "path": "Square",
  "ops": [ {"property":"position","value":"1,2,0"}, {"property":"scale","value":"1.5,1.5,1"} ] }
```

| `property` | Value | Applies to |
|---|---|---|
| `name` | string | any |
| `active` | `true`/`false` | any |
| `position`, `localPosition`, `rotation` (euler), `localRotation`, `scale`/`localScale` | `x,y,z` | any |
| `anchoredPosition`, `sizeDelta`, `anchorMin`, `anchorMax`, `pivot` | `x,y` | UI (RectTransform) |

The response reports `applied`, `errors`, and the object's resulting state.

### `reference.wire`

Sets `sourcePath.sourceComponent.field` to a target object, component, or asset — with Undo and
prefab-override handling (via `SerializedObject`). Provide **one** target form (or none to clear):

```json
{ "command": "reference.wire",
  "sourcePath": "Canvas/PlayerHUD", "sourceComponent": "PlayerHUD", "field": "healthBar",
  "targetPath": "Canvas/HealthBar", "targetComponent": "Slider" }
```

| Field | Meaning |
|---|---|
| `sourcePath`, `sourceComponent`, `field` | the field to set (required); `sourceComponentIndex` disambiguates duplicates |
| `targetPath` (+ optional `targetComponent`) | assign a scene GameObject, or a specific component on it |
| `targetAssetPath` | assign an asset instead (e.g. a Sprite/Material) |

Type mismatches are reported as errors (Unity rejects incompatible references).

## User references (the pointer channel)

The conversation with the AI stays in your normal chat window — this package does **not** rebuild
chat inside Unity. Instead it gives the user a way to *point*:

- **Live:** select object(s) in the Hierarchy/Scene, then ask the AI to call `selection.get`.
- **Pinned:** open **Tools ▸ AI Bridge ▸ AI Reference**, select your target (scene objects **and/or
  Project-window assets** like a sprite or prefab — or click a point in the Scene view), optionally
  type a note, and **Pin**. That writes `.aibridge/selection.json`:

  ```json
  { "pinnedAt": "2026-06-23T12:00:00.0000000+08:00", "note": "use this sprite",
    "kind": "selection", "objects": [ /* scene object(s) */ ],
    "assets": [ { "name": "hero", "assetPath": "Assets/Art/hero.png", "type": "Texture2D", "guid": "…" } ] }
  ```

  `kind` is `"selection"` (objects and/or assets) or `"point"` (a clicked Scene position under
  `point`). `selection.get` likewise reports both `objects` and `assets`. The AI — this session *and*
  the game-dev session — reads the same file. Asset paths feed straight into `reference.wire`
  (`targetAssetPath`).

## Configuration

Everything tunable lives on a `BridgeConfig` ScriptableObject — nothing is hardcoded.
Create one via **Tools ▸ AI Bridge ▸ Create Config Asset** (otherwise sensible defaults apply):

| Field | Default | Meaning |
|---|---|---|
| `channelFolder` | `.aibridge` | Channel root, relative to the project root |
| `pollIntervalSeconds` | `0.5` | How often the bridge scans for requests |
| `language` | `en` | Message language (`en`, `zh-CN`, …) |
| `enabled` | `true` | Master on/off switch |

## Design rules

- **No hardcoded parameters** — all in `BridgeConfig`.
- **No hardcoded strings** — UI text is in `Localization/<lang>.json`; code uses keys.
- **Modular** — one capability = one `ICommandHandler` in one file, auto-discovered. Adding a
  tool never edits a central switch.

## Extending

Add a file under `Editor/Handlers/` implementing `ICommandHandler`:

```csharp
public class MyHandler : ICommandHandler
{
    public string Command => "my.command";
    public CommandResult Execute(string rawJson) => CommandResult.Success("{}");
}
```

It registers itself on the next compile.

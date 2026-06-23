# Changelog

All notable changes to **AI Bridge for Unity** are documented here.
This project follows [Semantic Versioning](https://semver.org/).

## [0.14.0] — UGUI builder

### Added
- `ui.create`: turnkey UGUI — create an `image` / `text` / `button` (or just ensure a `canvas`).
  Auto-creates a portrait-friendly Canvas (ScreenSpaceOverlay + CanvasScaler) and an EventSystem
  (with the new Input System UI module when present), positions via anchor presets, and builds a
  button as Image + Button + child Text.

## [0.13.0] — art / sprites

### Added
- `asset.find`: find project assets by Unity search filter (e.g. `t:Sprite`, `t:Prefab`) — so the AI
  can discover available art before using it.
- `sprite.set`: assign a sprite (by asset path) to a SpriteRenderer or UI Image (via `m_Sprite`, no UI
  dependency). Auto-switches a texture's import type to Sprite and reimports if needed.

## [0.12.0] — self-compile

### Added
- `refresh`: triggers `AssetDatabase.Refresh` (imports new/changed files and recompiles changed
  scripts) and optionally a full recompile — so the AI can compile its own script edits without the
  user focusing Unity.
- `status`: editor busy-state (`isCompiling`/`isUpdating`/`isPlaying`/`isPaused`) plus the last
  compile's `compileMessages` (errors/warnings via `CompilationPipeline`). Poll after `refresh`.

## [0.11.0] — animation

### Added
- `animation.create`: turnkey keyframe animation — builds an AnimationClip (via `SetCurve`) from channels
  (friendly aliases `localPosition`/`localScale`/`rotation`/`color`, or exact bindings), creates an
  AnimatorController, and attaches an Animator so the object animates in Play mode. Approach adapted from
  Unity-AI-Animation (Apache-2.0).

## [0.10.0] — grid builder

### Added
- `grid.create`: builds an N×N grid of square cell tiles in one call (named `<prefix>_x_y` under a
  parent), using a procedural square sprite. Fast board/grid layout without many round-trips.

## [0.9.0] — values and cloning

### Added
- `component.set`: set value fields (bool/int/float/string/Color/Vector2-4/enum) via an ops list.
- `gameobject.duplicate`: clone a GameObject with its components (optional rename/reposition).

## [0.8.0] — console logs

### Added
- `console.get`: recent Console logs (filter by type / age / count, optional stack traces), captured via
  the supported `Application.logMessageReceivedThreaded` callback. Covers script/runtime logs; compiler
  errors use a different path (read `Editor.log`).
- `LogCollector` (ring buffer of the last 500 log entries).

## [0.7.0] — asset references

### Added
- Project-window **asset selections** are now captured: `selection.get` and the AI Reference pin
  include an `assets[]` array (name, assetPath, type, guid). Pin a sprite/prefab/material and wire it
  via `reference.wire` `targetAssetPath`.
- Shared `AssetDescriber`.

### Changed
- `selection.json` pin `kind` for object/asset pins is now `"selection"` (was `"objects"`).

## [0.6.0] — annotated screenshot

### Added
- `screenshot.annotated`: offscreen URP render of the active camera to a PNG (saved under the channel
  `out/`), with green bounding boxes over visible renderers and a parallel JSON list of each object's
  screen rectangle (top-left origin) and world position. Visual ground truth + precise data together.

## [0.5.0] — object modify

### Added
- `object.modify`: move / rotate / scale / rename / (de)activate an object, and set RectTransform
  anchors/size/pivot, via an ops list (only listed properties change). Records Undo.

## [0.4.0] — scene authoring

### Added
- `gameobject.create` (empty or primitive; optional parent, position, and components in one call).
- `component.add` (add a component by simple or full type name).
- `gameobject.delete` (Undo-able).
- `ComponentTypeResolver` — resolves a component type from a name across all loaded assemblies.

All scene edits register Undo and mark the scene dirty.

## [0.3.0] — auto-wiring

### Added
- `reference.wire` command: sets an object-reference field (GameObject, component, or asset) via
  `SerializedObject` — with Undo and prefab-override handling, and a type-compatibility check.
  Replaces manual Inspector drag-drop.
- Shared `SceneLookup` (find GameObject by path / component by type name).

## [0.2.0] — user reference channel

### Added
- `selection.get` command: returns the user's current Editor selection as structured data.
- **AI Reference** window (`Tools ▸ AI Bridge ▸ AI Reference`): pin the current selection — or a
  clicked Scene point — plus an optional note into `.aibridge/selection.json` for the AI to read.
- Shared `SceneObjectDescriber` so `scene.dump` and `selection.get` produce identical object shapes.
- Localized strings for the AI Reference window (`en`, `zh-CN`).

## [0.1.0] — walking skeleton

### Added
- File-channel transport (`.aibridge/in` → `.aibridge/out`), main-thread polling, survives domain reloads.
- `ICommandHandler` + reflection-based `CommandDispatcher` (modular, no central switch).
- `BridgeConfig` ScriptableObject for all parameters (channel folder, poll interval, language, enable).
- Localization service with `en` and `zh-CN` packs.
- Commands: `ping` (loop liveness), `scene.dump` (active scene as structured data with world positions and RectTransform anchors).

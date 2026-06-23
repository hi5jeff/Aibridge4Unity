# Changelog

All notable changes to **AI Bridge for Unity** are documented here.
This project follows [Semantic Versioning](https://semver.org/).

## [0.22.0] — particles

### Added
- `particle.create`: create a ParticleSystem from a preset (burst / explosion / stream / sparkle) with a
  2D-friendly Sprites/Default material, color, size, lifetime, and count. Plays in Play mode.

## [0.21.0] — audio

### Added
- `audio.add`: add/configure an AudioSource on a GameObject and assign an AudioClip (loop, volume,
  playOnAwake, 2D/3D spatialBlend).

## [0.20.0] — clear console

### Added
- `console.clear`: clears the Editor Console and the local log buffer (wipe stale errors before an
  operation so the next `console.read`/`console.get` is clean).

## [0.19.0] — reimport + Spine

### Added
- `asset.reimport`: force-reimport an asset/folder (recursive) — e.g. to make an importer like
  spine-unity regenerate its derived assets for files imported before the plugin was present.
- **Spine Integration** sample (`Samples~/SpineIntegration`): `spine.list`, `spine.instantiate`,
  `spine.play`. Optional, dependency-gated — only compiles when the spine-unity runtime is installed.

## [0.18.0] — run the game

### Added
- `editor.play`: enter / exit / toggle / pause / resume / step Play mode. The bridge keeps responding
  in Play mode, so the AI can run the game and capture it with `screenshot.gameview`. Transitions are
  async — poll `status` for `isPlaying`.

## [0.17.0] — prefabs

### Added
- `prefab.create`: save a scene GameObject as a reusable prefab asset (creating folders as needed) and
  connect the scene object to it.
- `prefab.instantiate`: instantiate a prefab into the active scene (optional name / parent / position).

## [0.16.0] — see the UI

### Added
- `screenshot.gameview`: captures the **actual Game View** (reading its internal render texture), so it
  includes ScreenSpaceOverlay UI that a camera render misses. Focuses the Game tab to force a render.
  Technique adapted from Unity-MCP (Apache-2.0).

## [0.15.0] — reliable console

### Added
- `console.read`: reads the Editor's actual Console window (via internal `LogEntries`), surviving domain
  reloads — so compile errors/warnings are reliably readable after a `refresh`, unlike the callback
  buffer which resets on reload.

### Fixed
- `ui.create`: replaced the obsolete `FindFirstObjectByType` calls (Unity 6000.5) with `FindObjectsByType`.

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

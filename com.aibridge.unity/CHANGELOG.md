# Changelog

All notable changes to **AI Bridge for Unity** are documented here.
This project follows [Semantic Versioning](https://semver.org/).

## [0.31.0] — spec→prefab: sliders & toggles

### Added
- **`ui.buildPrefabFromSpec`** now builds `Slider` (track + fill from `gauge_*`) and `Toggle` (bg + check)
  node types, not just Image/Text/Button/Container — so settings-style screens come out interactive.

## [0.30.0] — TMP material presets

### Added
- **`tmp.createMaterialPreset`** — create a TMP material preset `.mat` from a font asset
  (Bridge4Unity_개선제안 P1-2): instances a NEW material off the font's atlas material and sets
  Outline / Underlay(shadow) / Glow via the Material API (`EnableKeyword` + `SetColor`/`SetFloat`),
  so the original font is never edited (avoids the "edit TMP color → font breaks" bug). No TMPro asmdef
  reference needed. Assign the result to a text's `fontSharedMaterial`. Request: `{ fontResource, name,
  outDir, face, outline{color,width}, underlay{color,alpha,offsetX,offsetY,dilate,softness}, glow{…} }`.

## [0.29.0] — spec→prefab

### Added
- **`ui.buildPrefabFromSpec`** — turn a `.ui.json` node tree into a UGUI prefab (Bridge4Unity_개선제안
  P0-3). Builds a Canvas root (ScaleWithScreenSize, match 0.5) + each node: Image / Button (Image+Button) /
  Text (TMP via reflection — no TMPro asmdef ref needed) / Container (GridLayoutGroup / Vertical / Horizontal
  parsed from the `layout` string). Resolves `sprite` paths against configurable Resources roots, sets
  RectTransform anchors from the `anchor` token (`top-stretch`/`top-center`/`center`/…), font size/color/
  align, 9-slice. Tune in place afterwards with `prefab.modify`. Removes the hand-coding-coordinates
  pressure → `ui-prefab-not-code` becomes the default, not a rule. Request: `{ specPath, outPrefab (under
  Assets/), spriteRoots[], fontResource }`. Verified: generated Dex(13)/Rank(19)/Settings(22) node prefabs.

## [0.28.0] — data→asset binding (text images + batch import)

### Added
- **`asset.importBatch`** — copy an external folder of files into the project's `Assets/` and apply
  importer settings in one command (Bridge4Unity_개선제안 P0-2). Closes the "delivered ≠ imported" gap:
  art handed over in `shared/` (cutouts, text PNGs) gets copied in + set to Sprite (textureType/spriteMode/
  pixelsPerUnit) without manual dragging. Supports `dryRun`. Request: `{ srcDir, destDir (under Assets/),
  pattern, textureType, spriteMode, pixelsPerUnit, overwrite, dryRun }`.
- **`ui.bindFromManifest`** — auto-place static-text image sprites into a prefab's text slots from a
  `text→file` manifest. Walks every Graphic exposing a writable string `text` (TMP_Text or UI.Text),
  looks its current string up in the manifest, and for each match adds a child `~img` Image (stretched,
  `preserveAspect`) with the mapped sprite and blanks the glyphs. Dynamic slots (numbers, dialogue —
  no match) are left as TMP. Non-destructive: only matched slots gain a `~img`; the user's tuned layout
  is untouched. Supports **`dryRun`** (returns the would-bind/skipped diff without writing) and per-slot
  **`overrides`** for slots whose prefab placeholder differs from the manifest string. First of the
  Bridge4Unity_개선제안_v1 "data→asset" commands (productionizes the per-game StaticTextBaker).
  Request: `{ prefabPath, manifestResource, spriteRoot, dryRun, overrides:[{slot,text}] }`.

## [0.27.0] — force-compile

### Added
- **`compile`** — force a script recompile via `CompilationPipeline.RequestScriptCompilation()`.
  Unlike an asset `refresh` (which Unity often defers until the Editor window regains focus), this
  triggers compilation even while the Editor is **unfocused** — so an agent driving the Editor
  headlessly via the bridge no longer stalls waiting for focus. Returns immediately; poll `status` for
  `isCompiling`/`lastCompileHadErrors` and `console.read` for errors.

## [0.26.0] — non-destructive prefab editing + presentation rules

### Added
- **`prefab.modify`** — edit an existing prefab in place without rewriting it. Applies only the listed
  ops to named child objects (text / sprite / color / active / rect / `Component.member` via reflection,
  and `duplicateAs` to clone a child), then saves. Snapshots every RectTransform and Graphic and
  restores the ones not explicitly targeted, so a layout/canvas rebuild can never move or recolour
  objects you didn't touch — the user's hand-tuned layout is preserved.
- **`Documentation~/PRESENTATION_RULES.md`** — hard rules for the agent when building a project's
  presentation layer (editable prefabs, no hardcoded coords, non-destructive edits, native-size sprites,
  no git on the user's project, real Unity game-feel). A project's `CLAUDE.md` points to this file
  instead of inlining the rules, so they're maintained in one place and ship with the tool. SKILL.md
  links to it.

## [0.25.1] — docs: manual skill kick-start

### Changed
- Install docs note the reliable fallback: if the agent doesn't pick up the skill on its own, tell it
  once *"Read `.claude/skills/unity-bridge/SKILL.md` and use it to drive Unity."* (works in Claude
  Desktop and Claude Code).

## [0.25.0] — reliable auto-use by Claude Code

### Changed
- **Configure Claude Code** now also writes a small always-on pointer block into the project's
  `CLAUDE.md` (which Claude Code reads every session, unlike skills, which only auto-trigger
  heuristically). This stops the agent from falling back to screenshots / computer-use / hunting for
  a Unity MCP server — it drives the real Editor through the `.aibridge` channel. The block is an
  idempotent, clearly-marked managed section; user content elsewhere in `CLAUDE.md` is untouched.
- Strengthened the `unity-bridge` skill `description` so it auto-triggers on any Unity-related task.

### Note
- Skills and `CLAUDE.md` are read at session start — **restart Claude Code** in the project after
  running Configure.

## [0.24.0] — one-click Claude Code setup

### Added
- **Tools ▸ AI Bridge ▸ Configure Claude Code** menu: installs the bundled `unity-bridge` skill into
  `<project>/.claude/skills/unity-bridge/SKILL.md` so the local AI agent learns the commands — no
  manual file copying.
- `menu.execute` command: run any Editor menu item by path (e.g. drive the setup menu itself).

## [0.23.0] — scene management

### Added
- `scene.create` / `scene.open` / `scene.save` / `scene.list` / `scene.close`: full multi-scene support
  (single & additive). Single-mode create/open is guarded against discarding unsaved changes unless
  `force` is passed.

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

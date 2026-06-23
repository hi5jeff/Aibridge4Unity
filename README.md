# AI Bridge for Unity

**Let a local AI agent drive your Unity Editor — over a plain folder. No MCP, no server, no ports.**

AI Bridge gives an AI assistant (Claude Code, or any local agent that can read/write files)
precise **eyes and hands** inside the Unity Editor: read the scene and the user's selection as
structured data, capture annotated screenshots, create/modify/wire objects, build grids, and author
keyframe animations — all by exchanging small JSON files in a watched folder.

> Status: **early / evolving (0.11.x).** 14 commands. Built and validated live while playing a full
> game of Gomoku on the Unity scene and authoring animations. APIs may still change.

---

## Why another one?

Most AI-to-Unity bridges need a Unity package **+ a background server process + MCP client config**
(three moving parts, a port, a protocol). AI Bridge needs **two things and zero processes**:

| | Typical MCP bridge | **AI Bridge** |
|---|---|---|
| Install | Unity package + Python server + MCP config | Unity package + a skill for your local agent |
| Running processes | background server + open port | **none** |
| Transport | SignalR / HTTP | **a watched folder** |

If your agent can read and write files in your project, it can drive Unity.

## How it works

```
Local AI agent (e.g. Claude Code)         ← reads/writes the channel, no Unity knowledge needed
        │  writes  <project>/.aibridge/in/<id>.json
        │  reads   <project>/.aibridge/out/<id>.json
        ▼
AI Bridge package (this repo)             ← runs inside the Unity Editor (InitializeOnLoad)
        • polls the channel on the main thread
        • routes each request to a handler, runs the Unity API, writes the reply
        ▼
Unity Editor (the real, open project)
```

There are **two components**:

1. **The Unity package** (`com.aibridge.unity`) — you install this into your Unity project. It
   auto-starts when the Editor loads.
2. **A local AI agent** — *not* a Unity plugin. It’s whatever agent runs on your machine with access
   to the project folder (e.g. Claude Code). It learns the channel protocol from the bundled skill.

## Install

In Unity: **Window → Package Manager → + → Add package from git URL**, paste:

```text
https://github.com/hi5jeff/Aibridge4Unity.git?path=/com.aibridge.unity
```

Requires **Unity 6 (6000.x)**. Open your project — the bridge starts automatically; you’ll see a
`[AIBridge] Bridge ready …` line in the Console and a **Tools ▸ AI Bridge** menu.

### Set up the AI side

Point your local agent at the project and give it the channel protocol. For **Claude Code**, copy the
bundled skill into your project so the agent can use it:

```text
com.aibridge.unity/Documentation~/AI-USAGE.md   →   .claude/skills/unity-bridge/SKILL.md
```

(add the YAML frontmatter shown at the top of that file). Then just ask your agent to inspect or
change the scene — it will drive Unity through `.aibridge/`.

## Quickstart (verify the loop)

With the Editor open, from the project root:

```bash
cd .aibridge
printf '{ "id":"1", "command":"ping" }' > in/1.json
sleep 2 && cat out/1.json
# → { "id":"1", "command":"ping", "ok":true, "error":null,
#     "result":{ "message":"pong", "unityVersion":"6000.x" } }
```

## Channel protocol

- **Send:** write `in/<id>.json` — `{ "id": "<unique>", "command": "<name>", ...fields }` (fields flat).
- **Receive:** read `out/<id>.json` — `{ "id", "command", "ok", "error", "result" }`.
- The bridge polls ~2×/sec. Use a unique `id` per request.

## Commands (0.11.x)

| Command | Purpose |
|---|---|
| `ping` | Liveness / loop check |
| `scene.dump` | Active scene as data: paths, world positions, UI anchors, components |
| `selection.get` | The user's current Editor selection (objects + Project assets) |
| `reference.wire` | Set an object-reference field (auto-link), replacing Inspector drag-drop |
| `gameobject.create` | Create a GameObject (empty or primitive), parented, positioned, with components |
| `gameobject.duplicate` | Clone a GameObject (with components) |
| `gameobject.delete` | Delete a GameObject (Undo-able) |
| `object.modify` | Move / rotate / scale / rename / (de)activate via an ops list |
| `component.add` | Add a component by type name |
| `component.set` | Set value fields (numbers, bools, strings, colors, vectors, enums) |
| `grid.create` | Build an N×N grid of square tiles in one call |
| `screenshot.annotated` | Offscreen camera render + bounding boxes + per-object screen rects |
| `console.get` | Recent Console logs (script/runtime) for debugging |
| `animation.create` | Keyframe animation: build clip + AnimatorController + attach Animator |

Full field reference: [`com.aibridge.unity/README.md`](com.aibridge.unity/README.md).

## The user as a pointer

The conversation with the AI stays in your normal chat window — this package does **not** rebuild
chat inside Unity. Instead it lets the user *point*:

- **Select** object(s) or Project assets → the AI calls `selection.get`.
- **Pin** a target (or click a point in the Scene view) via **Tools ▸ AI Bridge ▸ AI Reference** →
  writes `.aibridge/selection.json`, which the AI reads to resolve “this / here”.

So the user communicates with the AI by **talking in chat + pointing in Unity** — no more describing
coordinates ten times.

## Configuration

All tunable parameters live on a `BridgeConfig` ScriptableObject (nothing hardcoded). Create one via
**Tools ▸ AI Bridge ▸ Create Config Asset**, or run on sensible defaults: channel folder, poll
interval, language (`en` / `zh-CN`), enable switch.

## Design principles

- **No hardcoded parameters** — all in `BridgeConfig`.
- **No hardcoded strings** — UI text lives in `Localization/<lang>.json`; code uses keys.
- **Modular** — one capability = one `ICommandHandler` in one file, auto-discovered. Adding a tool
  never edits a central switch.
- **Pluggable transport** — `ITransport` abstracts the channel; a file folder today, HTTP/pipe later
  (e.g. to support a remote agent) without touching handlers.

## Roadmap

- `refresh` — trigger recompile / asset refresh from the bridge and report editor busy-state, so the
  AI no longer needs the user to focus Unity.
- Script authoring loop (write C# → compile → read errors).
- Scene management (new / open / save), prefabs, Play-mode control, test running.

## Attribution

- The `animation.create` approach (`AnimationClip` + `SetCurve`) is adapted from
  [Unity-AI-Animation](https://github.com/IvanMurzak/Unity-AI-Animation) by Ivan Murzak (Apache-2.0).
- Capability ideas surveyed from [Unity-MCP](https://github.com/IvanMurzak/Unity-MCP) and
  [MCP for Unity](https://github.com/CoplayDev/unity-mcp).

## License

[MIT](LICENSE).

# AI Bridge for Unity

**Build your Unity game by talking to an AI.** AI Bridge connects a local AI assistant (like
[Claude Code](https://claude.com/claude-code)) to your open Unity Editor — so it can see your scene,
create and edit objects, lay out UI, use your art, make animations, write game logic, and even run the
game, all from a chat. No server, no ports, no MCP setup — just install a package.

> Status: early and evolving, but already capable. Made for **2D mobile** workflows on **Unity 6**.

## What it can do

- 👀 **See** your scene as data *and* as real screenshots — including the UI and the game while it runs.
- 🧱 **Build** GameObjects, UGUI (canvas/buttons/text), grids, and prefabs.
- 🎨 **Use your art** — import images as sprites and put them on objects.
- 🎞️ **Animate** — author keyframe animations (move, scale, color) in one step.
- 🧠 **Write game logic** — it writes C#, compiles it itself, and reads its own errors.
- ▶️ **Run the game** — enter Play mode and watch it.
- 👉 **Understand "this / here"** — you click an object or a spot in Unity, and the AI knows exactly what you mean.

## Requirements

- **Unity 6** (6000.x).
- A **local AI agent** that can read/write files in your project — e.g. **Claude Code**.
  It must run on the same machine as Unity (the two talk through a folder in your project).

## How it works

```
You + a local AI agent (e.g. Claude Code)
        │  the agent reads/writes a folder in your project (.aibridge)
        ▼
AI Bridge package  ──  runs inside the Unity Editor, executes the requests
        ▼
Your Unity project (the real, open Editor)
```

Two pieces: **(1)** this Unity package, installed in your project (auto-starts with the Editor), and
**(2)** your local AI agent, which learns how to drive it from the bundled skill. No background process
or network — the bridge just watches a folder.

## Install

**1. Add the Unity package.** In Unity: **Window → Package Manager → + → Add package from git URL**, paste:

```text
https://github.com/hi5jeff/Aibridge4Unity.git?path=/com.aibridge.unity
```

Open your project — you'll see `[AIBridge] Bridge ready …` in the Console and a **Tools ▸ AI Bridge** menu.

**2. Teach your AI agent.** Give your local agent the bundled skill so it knows the commands. For
**Claude Code**, copy:

```text
com.aibridge.unity/Documentation~/AI-USAGE.md  →  <your project>/.claude/skills/unity-bridge/SKILL.md
```

That's it. Now just ask.

## Use it — examples

Talk to your AI agent normally; it drives Unity for you. For example:

| You say… | The AI does… |
|---|---|
| *"What's in the scene?"* | reads every object's position, components and UI anchors |
| *"Show me the game"* | captures the real Game View (UI included) and looks at it |
| *"Add a blue **Play** button in the center"* | creates a Canvas + button + label, anchored |
| *"Use `hero.png` as the player's sprite"* | imports the image as a sprite and assigns it |
| *"Make the coin bob up and down"* | builds a looping keyframe animation and attaches it |
| *"Turn this into a prefab and spawn 5 of them"* | saves a prefab and instantiates copies |
| *"Add a `PlayerHealth` script and put it on the player"* | writes the C#, compiles it, attaches it |
| *"Move **this** here"* (you click an object, then a spot) | reads your selection/point and moves it |
| *"Run it"* | enters Play mode so you can watch the game |

### Pointing instead of describing

Open **Tools ▸ AI Bridge ▸ AI Reference** to *point*: select an object (or click a point in the Scene
view) and pin it — the AI reads exactly what you meant, so you never describe coordinates ten times.

## Configuration

Everything tunable lives on a `BridgeConfig` asset (channel folder, language `en`/`zh-CN`, on/off…).
Create one via **Tools ▸ AI Bridge ▸ Create Config Asset**, or just use the defaults.

## License

[MIT](LICENSE).

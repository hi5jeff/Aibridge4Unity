# AI Bridge for Unity

[English](README.md) | **简体中文**

**用对话的方式做你的 Unity 游戏。** AI Bridge 把本地 AI 助手（比如
[Claude Code](https://claude.com/claude-code)）连到你正打开的 Unity 编辑器——它能看你的场景、
创建和修改对象、排版 UI、用你的美术资源、做动画、写游戏逻辑，甚至直接运行游戏，全程在对话里完成。
无需服务器、无需端口、无需配置 MCP——装个包就行。

> 状态：早期、持续演进，但已经能干活了。面向 **Unity 6** 上的 **2D 手游** 工作流。

## 它能做什么

- 👀 **看**你的场景——既能读成数据，也能拍真实截图（包含 UI，以及游戏运行时的画面）。
- 🐛 **读控制台**——日志、警告、编译错误——它能自己调试、修自己的错，不用你念给它听。
- 🧱 **搭建** GameObject、UGUI（画布/按钮/文字）、网格、预制体。
- 🎨 **用你的美术**——把图片导入成精灵并挂到对象上。
- 🎞️ **做动画**——关键帧动画（移动/缩放/颜色），以及可选的 **Spine** 骨骼动画。
- 🔊 **声音 & ✨ 特效**——添加音频源和粒子特效。
- 🧠 **写游戏逻辑**——它写 C#、自己编译、自己读错误。
- ▶️ **运行游戏**——进入 Play 模式并观察。
- 👉 **听懂「这个 / 这里」**——你在 Unity 里点一个对象或一个位置，AI 就准确知道你指的是什么。

## 环境要求

- **Unity 6**（6000.x）。
- 一个能读写你项目文件的**本地 AI 代理**——例如 **Claude Code**。
  它必须和 Unity 跑在同一台机器上（两者通过你项目里的一个文件夹通信）。

## 工作原理

```
你 + 本地 AI 代理（如 Claude Code）
        │  代理读写你项目里的一个文件夹（.aibridge）
        ▼
AI Bridge 包  ──  运行在 Unity 编辑器内部，执行这些请求
        ▼
你的 Unity 项目（真实、正打开的编辑器）
```

两个部件：**(1)** 这个 Unity 包，装在你的项目里（随编辑器自动启动）；**(2)** 你的本地 AI 代理，
通过随包提供的 skill 学会如何驱动它。没有后台进程、没有网络——桥只是在盯着一个文件夹。

## 安装

**1. 添加 Unity 包。** 在 Unity 里：**Window → Package Manager → + → Add package from git URL**，粘贴：

```text
https://github.com/hi5jeff/Aibridge4Unity.git?path=/com.aibridge.unity
```

打开项目——你会在 Console 看到 `[AIBridge] Bridge ready …`，并多出一个 **Tools ▸ AI Bridge** 菜单。

**2. 配置你的 AI 代理——一键完成。** 在 Unity 里：**Tools ▸ AI Bridge ▸ Configure Claude Code**。
它会把 `unity-bridge` skill 装到 `.claude/skills/unity-bridge/SKILL.md`，**并**在你项目的 `CLAUDE.md`
里加一段常驻提示，让 Claude Code 稳定地通过桥来驱动 Unity，而不是去截屏或到处找 MCP 服务器。

然后**重启项目里的 Claude Code**（skill 和 `CLAUDE.md` 都在会话启动时读取）。之后直接问就行。

> **如果代理没有自动用上**，跟它说一句即可：
> *“读取 `.claude/skills/unity-bridge/SKILL.md` 并用它来驱动 Unity。”*
> 它从这个文件学会文件通道后，本次会话剩下的时间就一直能用了。（Claude Desktop 和 Claude Code 都适用。）

## 怎么用——示例

像平常一样跟你的 AI 代理对话，它替你操作 Unity。例如：

| 你说… | AI 做… |
|---|---|
| *“场景里有什么？”* | 读出每个对象的位置、组件和 UI 锚点 |
| *“给我看看游戏画面”* | 抓取真实 Game 视图（含 UI）并查看 |
| *“在正中间加一个蓝色 **Play** 按钮”* | 创建画布 + 按钮 + 文字，并设好锚定 |
| *“把 `hero.png` 用作玩家的精灵”* | 把图片导入成精灵并赋值 |
| *“让金币上下浮动”* | 生成一个循环关键帧动画并挂上 |
| *“把这个做成预制体，生成 5 个”* | 保存预制体并实例化多份 |
| *“加一个 `PlayerHealth` 脚本挂到玩家上”* | 写 C#、编译、挂载 |
| *“为什么报错了？修一下。”* | 读 Console 错误、定位原因并修复 |
| *“加一个金币拾取音效”* / *“在这里加一束火花”* | 添加 AudioSource / 粒子特效 |
| *“生成 spineboy 角色并播放 'walk'”* | 实例化一个 Spine 骨骼并播放该动画 |
| *“把**这个**移到这里”*（你先点对象，再点位置） | 读取你的选择/点位并移动 |
| *“跑起来”* | 进入 Play 模式让你观看游戏 |

### 用「指」代替「描述」

打开 **Tools ▸ AI Bridge ▸ AI Reference** 来*指物*：选中一个对象（或在 Scene 视图里点一个位置）
并钉住——AI 就能准确读到你的意思，省得你把坐标描述十遍。

## 配置

所有可调项都放在一个 `BridgeConfig` 资源上（通道文件夹、语言 `en`/`zh-CN`、开关…）。
通过 **Tools ▸ AI Bridge ▸ Create Config Asset** 创建一个，或直接用默认值。

## 许可

[MIT](LICENSE)。

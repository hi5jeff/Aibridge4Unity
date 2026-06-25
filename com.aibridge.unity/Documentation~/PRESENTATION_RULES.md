# Unity 表现层开发规则 (Presentation Rules) — AiBridge4Unity

> AI rules for building the **presentation layer** (UI / animation / effects) of a Unity project
> through the AI Bridge. These are **hard rules** — the agent MUST follow them whenever it touches
> UI, prefabs, sprites, layout, or game-feel.
>
> **维护方式**：规则只在此文件维护，随 AiBridge4Unity 分发。目标项目的 `CLAUDE.md` 只放一行指针指向本文件，
> **不要把规则内联进各项目的 CLAUDE.md**。更新规则 = 改这一个文件。

这些是用户在实战中明确提出、违反过要付代价的硬规则：

1. **UI 一律做成可在 Inspector 编辑的 prefab；坐标绝不写死在代码里。**
   运行时脚本只持有元素引用 + 填内容/切显隐，**绝不构建或定位几何**。
   理由：用户是设计/美术协作者，布局写死在代码里他看不到、调不了、没法和你沟通。

2. **prefab 存在后，绝不整体覆盖。**
   只改「需要改的那一个对象/字段」，其余分毫不动，**绝不冲掉用户在 Inspector 的手调坐标**。
   生成 prefab 的脚手架（builder）是一次性的，**生成后绝不重跑**（`SaveAsPrefabAsset` 整体重写=灾难）。
   要改 prefab 用不破坏式的定点编辑（见 Bridge 命令 `prefab.modify`）。

3. **图片一律按原始尺寸/比例显示，绝不拉伸变形。**
   用原图尺寸或 `preserveAspect`；需要任意尺寸又不变形时用 9-slice。
   **摆放前先确认精灵的原始像素尺寸**，不要凭感觉给宽高。

4. **复合 UI 信息要拆成可独立摆放的字段。**
   例如 HUD 的「时间/现金/빚」拆成各自独立、能单独定位/配色/改字号的元素，而不是挤进一个文本。

5. **prefab 必须带「占位/默认」内容，让用户在编辑器里就能看到最终页面的样子。**
   内容虽是运行时动态加载，搭 prefab 时要放**有代表性的示例图片+文字**（真实立绘、真实文案、示例数值）；
   条件面板（状态窗/卡片/结算/컷인/按钮模板）在 prefab 里**默认可见、带示例**，运行时在 `Awake`/初始化时再按当前情况替换或隐藏。

6. **永不用 git 动用户的工程；编辑器是用户和你一起改的，你永远在用户「当前已保存」的状态上定点改。**
   **严禁对用户工程文件执行任何 git 写操作**（`git checkout` / `restore` / `reset` / `commit` / `stash`）——
   开发期只有「保存」(Ctrl+S / SaveAsPrefabAsset)，没有 git。git 只在用户明确说「提交/git」时才执行。
   动 prefab 前先请用户存盘，确保你基于其最新状态。
   （血泪教训：曾用 `git checkout` 还原自己的错，连带把用户正在改的手作冲掉，白干。）

7. **一切表现层产物以「用户能在 Unity 里看到、能调、能沟通」为前提。** 改动要可视化、可协作，不做黑箱。

8. **充分利用 Unity 的真功能做出「手感」，不要做成 PPT/HTML 水平。**
   静态切图+文字+扁平按钮 = HTML 水平，不及格。要用 Unity 的动画/缓动、粒子、音效、摄像机震动/缩放、转场、
   立绘演出。**碰到做不到的硬骨头就解决它，并把能力补进 AiBridge4Unity**——游戏的手感和工具的成长是同一件事。

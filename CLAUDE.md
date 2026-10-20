# 山海驭灵录 M1｜Claude Code 工作说明

> 这份文件给 Claude Code 看，帮助它快速了解本工程。内容根据 2026-10-20 电脑上的项目文档和目录整理，未经过完整试玩验证。

## 项目概况

- 游戏：《山海驭灵录》，第三人称开放世界原型，玩家收服、培养灵兽并探索大地图。
- 引擎：Unity 6000.6.6f1（Unity 6），Windows 平台。
- 工程位置（本工程根目录）：`D:\游戏开发\项目\山海驭灵录_M1\UnityProject_M1_v0.7.5`
- 当前可见的工程目录：`Assets`、`Packages`、`ProjectSettings`，以及 Unity 生成的 `Library`、`Logs`、`Temp` 等缓存（不要提交或复制缓存）。
- 外层目录 `D:\游戏开发\项目\山海驭灵录_M1\` 还包含项目文档、版本说明、成品交付、验证记录。

## 版本与文件约定

- **旧版本保留**：每个新版本都放在新目录里，不覆盖旧版本。成品在 `05_成品交付\Windows\<版本号>\`。
- 项目管理文档在 `00_项目管理\`，验证记录在 `04_验证记录\`。
- 删除文件、覆盖文件、安装软件、修改系统设置前，先征得用户明确同意。
- 注意：`README_启动与内容说明.md` 还是 v0.3 的内容，已过时；以 `00_项目管理\项目总览.md` 和最新版本说明为准。

## 版本历史（关键节点）

| 版本 | 主要内容 |
|---|---|
| v0.7.5 | A/B 两种瞄准方式（A 固定中心准星，B 鼠标移动准星），Shift+W 加速，蓄力攻击，三个世界 |
| v0.7.6 / v0.7.7 | 3D 骨骼动画主角与异兽，640 米大地图，风格化植被，流畅度优化 |
| v0.7.7_1020_0945 | 能正常启动的稳定版（回退参考），瞄准方式为鼠标（B 版） |
| v0.7.9 | 800 米地图；100 米刷新圈，圈内定时刷野生灵兽；44 个采集资源点（按住 F 读条）；防穿模摆放、模型贴地、中文层级命名 |

v0.7.9 改动文件已经写入工程的 `Assets\Scripts` 和 `Assets\Editor`，并且文件大小与改动包一致。Unity 已编译通过，**但还没有构建试玩**。

## 构建与运行

- Unity 菜单：**Shanhai → Build v0.7.7 · 3D 角色 + 大地图（B 版）**，构建完成后会自动启动游戏。
- 构建命令由 `Assets\Editor\M1WindowsBuildV077Command.cs` 提供；早期版本各有对应的 `M1WindowsBuildV0xxCommand.cs`。
- 构建出错时，把 Unity Console 里的红色报错发给 Claude 来定位。
- Unity 日志位于 `Logs\Editor.log`。

## 关键脚本（`Assets\Scripts`）

- `M1GameManager.cs`：游戏主流程管理（v0.7.9 有改动）。
- `M1WildSpawner.cs`：野生灵兽刷新逻辑，v0.7.9 新增，包括 100 米刷新圈和刷新概率。
- `M1HarvestNode.cs`：采集资源点（v0.7.9 有改动，按住 F 读条）。
- `M1ResourceCrafting.cs`：资源合成（v0.7.9 有改动）。
- `M1Actor.cs`、`M1PlayerController.cs`、`M1CameraFollow.cs`：角色与镜头控制。
- `M1ChargeWeaponSystem.cs`、`M1SpellProjectile.cs`：蓄力攻击与法术弹道。
- `M1PetSystem.cs`、`M1SpiritBeastMotion.cs`、`M1CreatureAnimator.cs`：灵兽队伍与动画。
- `M1SpeciesDatabase.cs`：灵兽数据库。
- `M1TamingGuide.cs`、`M1PetNameplates.cs`：收服引导与灵兽头顶名牌。
- `M1UISkin.cs`：UI 样式。
- `M1Portal.cs`：世界之间的传送门。
- `M1AudioDirector.cs`、`M1CreatureAudio.cs`：音效。

`Assets\Editor` 下有大量历史构建与世界生成脚本（`M1WorldBuilderV03` 到 `V077`、`M1WindowsBuildV02` 到 `V077` 等）。**当前主要使用最新版本**（`M1WorldBuilderV077.cs`、`M1WindowsBuildV077Command.cs`、`M1AssetSetupV077.cs`），旧脚本保留作参考，不要随意删除。

## 美术资源（`Assets\Art`）

- 模型：`Models\v077\Characters`（法师等）、`Models\v077\Creatures`（生物）、`Models\v077\Nature`（树木等）、`Models\External_Quaternius`（外部 CC0 模型，如 BlueDemon）。
- 贴图：`Models\v077\Textures`（树皮、叶子、岩石等，多为 2048 分辨率 PNG）。
- 地形网格：`Meshes\v0.7.7\Terrain_0/1/2.asset`，每个约 13MB。
- 其他：`Art\Materials`、`Art\Animators`、`Art\Textures`。
- 素材来源：Quaternius（CC0）、Kenney（CC0）。

## 已知问题与待确认事项

- **灵兽数量不一致**：v0.7.5 文档写的是 16 种，v0.7.9 文档写的是 20 种。以 `M1SpeciesDatabase.cs` 中的实际数据为准。
- **世界尺寸不一致**：v0.7.5 文档写三张 320 米地图，v0.7.7 为 640 米，v0.7.9 为 800 米。
- Unity 控制台有警告：工程使用旧版 Input Manager，建议迁移到 Input System 包。
- Shift 连续快速点击会触发 Windows 粘滞键提示，属于系统快捷键，不在游戏内处理。
- 当前没有存档功能。宠物队伍在三个世界之间保留，关闭程序后会重置。

## 进行中的任务

- **模型与贴图优化**：计划新建副本 `UnityProject_M1_v0.7.9_优化`，只修改导入设置（贴图最大尺寸、压缩方式，FBX 的网格压缩、Read/Write），不改动模型外形，原工程不动。
- 地形网格（约 39MB）和生物减面，等导入设置优化完成、画面确认后再做，减面会改变外观。
- 副本的复制由用户在文件管理器中完成，然后再由 Claude 在 Unity 里修改导入设置。

## 给 Claude Code 的工作建议

1. 先读本文件，再读 `00_项目管理\项目总览.md` 和最新版本说明，确认当前版本。
2. 修改脚本前，先确认是哪个版本的功能，避免误改历史版本。
3. 不要提交 `Library`、`Temp`、`Logs`、`obj` 等缓存目录。
4. 构建试玩后，记录在 `04_验证记录\<版本号>\` 中。

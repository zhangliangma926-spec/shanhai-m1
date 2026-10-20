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
- 注意：`README_启动与内容说明.md` 位于仓库外层目录，还是 v0.3 的内容，已过时，不再维护。当前启动方式以本文件的「构建与运行」为准。该旧文件已移入外层目录 `_归档\`。以 `00_项目管理\项目总览.md` 和最新版本说明为准。

## 版本历史（关键节点）

| 版本 | 主要内容 |
|---|---|
| v0.7.5 | A/B 两种瞄准方式（A 固定中心准星，B 鼠标移动准星），Shift+W 加速，蓄力攻击，三个世界 |
| v0.7.6 / v0.7.7 | 3D 骨骼动画主角与异兽，640 米大地图，风格化植被，流畅度优化 |
| v0.7.7_1020_0945 | 能正常启动的稳定版（回退参考），瞄准方式为鼠标（B 版） |
| v0.7.9 | 800 米地图；100 米刷新圈，圈内定时刷野生灵兽；44 个采集资源点（按住 F 读条）；防穿模摆放、模型贴地、中文层级命名 |
| v0.8.0 | 新动物模型（狐、雄鹿、哈士奇、羊驼、林鹿、柴犬，Quaternius CC0）；步频随速度同步；飞行类异兽离地 2.6–3.4 米飞行（`M1FlyerMotion`）；贴地倾斜（`M1GroundAlign`）；牧群避障、刷新避开实体、草不长进石头；牧群、灌木、灵晶、路灯加实体碰撞；树叶补色（之前发白）；无缝地面贴图与双尺度地面 shader（`Shanhai/VertexColorLitV080`）；草坪加密；动物中文命名；版本号统一取 `M1GameManager.Version`；Blender 绑骨与补动作（岚角灵兽、岩甲灵龟、猫、狼、猪、羊，见 `Models\v080\Beasts`）；猫/狗/狼与岚角灵兽朝向修正；主角冲刺步频同步与倾身；**无限地图**（`M1InfiniteWorld` + `M1TerrainNoise`，40 米区块、100 米渲染距离、去掉边界墙） |
| v0.8.1 | 路线图阶段 A：瞄准不再随镜头远近变化（跳过镜头与角色之间的物体、伙伴不挡枪、按屏幕约 40 像素放宽命中）；战斗反馈（`M1CombatFeedback` 飘字、守卫血条、可结契提示）；树干碰撞放在实际树干位置、石头凸包碰撞、高植物加碰撞；窗口标题随版本号 |
| v0.8.2 | 路线图阶段 B：7 步新手引导与右上角“当前目标”面板（`M1Onboarding`，F2 跳过、F3 隐藏）；开场页改为三步上手；HUD 重排不截字；画面压暗、阴影加深、雾分层、程序化天空 |
| v0.8.3 | 路线图阶段 C：`Models\v083` 下 11 个 Blender 模型与动作（主角走跑重做、月羽灵蝶、灵雀/朱雀/仙鹤、修正朝向的苍鹰、新灵龟、水貂/雷豹/山猫、猪冲撞）；灵鸟起飞 / 降落；灵兽伤害按攻击命中帧结算（`M1CreatureAnimator.attackHitDelay`） |

后续优化按 `00_项目管理\优化路线图_v0.8.1起.md`（任务 T7.1–T7.19，同步在 `docs/plan/tasks.yaml`）逐项推进。

v0.7.9 改动文件已经写入工程的 `Assets\Scripts` 和 `Assets\Editor`，并且文件大小与改动包一致。2026-10-20 已用命令行构建成功并能进入世界，成品在 `05_成品交付\Windows\v0.7.9\`，交付包在 `05_成品交付\山海驭灵录_v0.7.9_可直接传输\`，验证记录在 `04_验证记录\v0.7.9\`。刷新圈和采集**还没有完整手动试玩**。

## 构建与运行

- Unity 菜单：**Shanhai → Build v0.7.7 · 3D 角色 + 大地图（B 版）**，构建完成后会自动启动游戏。菜单名仍写 v0.7.7，但它调用的是 `M1WorldBuilderV077.BuildAll()`，生成的是当前版本内容（800 米地图）。输出目录和版本号取自 `M1GameManager.Version`。
- 命令行构建：`Unity.exe -batchmode -quit -projectPath <工程目录> -executeMethod M1WindowsBuildV077Command.Build -logFile <日志>`。本机 Unity 在 `D:\游戏开发\Unity\Unity\Hub\Editor\6000.6.6f1\Editor\Unity.exe`。构建会重新生成场景和动画控制器（内部 ID 变化），这些变化不必提交。
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
- `M1PetSystem.cs`、`M1SpiritBeastMotion.cs`、`M1CreatureAnimator.cs`：灵兽队伍与动画（v0.8.0 起动画步频随速度同步）。
- `M1FlyerMotion.cs`：飞行类异兽离地飞行、起伏、侧倾（v0.8.0 新增）。
- `M1GroundAlign.cs`：四足动物随坡面倾斜贴地（v0.8.0 新增）。
- `M1AmbientCritter.cs`：牧群与盘旋飞鸟等氛围动物。
- `M1TerrainNoise.cs`：地形高度 / 颜色函数（v0.8.0 起编辑器和运行时共用）。
- `M1InfiniteWorld.cs`、`M1GroundSurface.cs`：无限区块地图与 100 米渲染距离（v0.8.0 新增）。
- `M1SpeciesDatabase.cs`：灵兽数据库。
- `M1TamingGuide.cs`、`M1PetNameplates.cs`：收服引导与灵兽头顶名牌。
- `M1UISkin.cs`：UI 样式。
- `M1Portal.cs`：世界之间的传送门。
- `M1AudioDirector.cs`、`M1CreatureAudio.cs`：音效。

`Assets\Editor` 下有大量历史构建与世界生成脚本（`M1WorldBuilderV03` 到 `V077`、`M1WindowsBuildV02` 到 `V077` 等）。**当前主要使用最新版本**（`M1WorldBuilderV077.cs`、`M1WindowsBuildV077Command.cs`、`M1AssetSetupV077.cs`），旧脚本保留作参考，不要随意删除。

## 美术资源（`Assets\Art`）

- 模型：`Models\v077\Characters`（法师等）、`Models\v077\Creatures`（生物）、`Models\v077\Nature`（树木等）、`Models\External_Quaternius`（外部 CC0 模型，如 BlueDemon）、`Models\v080\Animals`（v0.8.0 新增的 Quaternius 动物包，来源见其中 `ATTRIBUTION.md`）。
- 贴图：`Models\v077\Textures`（树皮、叶子、岩石等，多为 2048 分辨率 PNG）。
- 地形网格：`Meshes\v0.7.7\Terrain_0/1/2.asset`，每个约 13MB。
- 其他：`Art\Materials`、`Art\Animators`、`Art\Textures`。
- 素材来源：Quaternius（CC0）、Kenney（CC0）。

## 已知问题与待确认事项

- **灵兽数量（已核对）**：`M1SpeciesDatabase.cs` 中共 20 种，即 v0.7.5 的 16 种加上 v0.7.8 新增的 4 种。以 20 种为准，旧文档中的"16 种"已过时。
- **世界尺寸**：v0.8.0 起为无限地图（800 米手工核心区 + 运行时生成的外围区块）。v0.7.9 及之前：当前生效为 800 米。`M1WorldBuilderV077.cs` 中 `Size = 800f`，`M1WildSpawner.cs` 的兜底范围同为 800。历史版本：v0.7.5 为三张 320 米地图，v0.7.6 / v0.7.7 为 640 米，v0.7.9 为 800 米。
- 已知残留：`M1WindowsBuildV077Command.cs` 的进度提示文字仍写"生成 640 米世界"，实际生成的是 800 米，仅影响提示文字，未改动。
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

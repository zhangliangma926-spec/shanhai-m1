# v0.8.3 模型与动作产出清单（路线图阶段 C：T7.8–T7.13）

> 只产出文件，**尚未接入 Unity 工程**。所有文件由 `脚本\` 下的 Blender 5.2 脚本生成，可重复运行：
> `D:\游戏开发\Blener\blender.exe --background --python 脚本\<脚本名>.py`
> 原有 .fbx / .png / .blend 均未修改，新文件全部另存在本目录。

## 总览（2026-10-20）

| 文件（`FBX\`） | 对应 | 动作片段 | 正面朝向 | 来源 |
|---|---|---|---|---|
| Wizard_v083.fbx | 主角 | Walk、Run 重做；其余 13 段原样 | -Y（同原文件） | Quaternius CC0 + 脚本 |
| Butterfly_v083.fbx | 月羽灵蝶 | Flying、Idle、Attack、Hit | -Y | 原创 |
| Eagle_v083.fbx | 苍鹰（氛围飞鸟） | Idle（重做）、Flying、Glide、TakeOff、Land、Attack、Hit | -Y（已从 +Y 转过来） | Quaternius CC0 + 脚本 |
| SpiritSparrow_v083.fbx | 青羽灵雀 | Idle、Flying、Glide、TakeOff、Land、Attack、Hit | -Y | 原创 |
| Vermilion_v083.fbx | 赤霄朱雀 | 同上 | -Y | 原创 |
| Crane_v083.fbx | 苍羽仙鹤 | 同上 | -Y | 原创 |
| RockTurtle_v083.fbx | 岩甲灵龟 | Idle、Walk、Run、Attack、Hit | -Y | 原创 |
| Mink_v083.fbx | 琉璃水貂 | Idle、Walk、Run、Attack、Hit | -Y（已从 +X 转过来） | Quaternius CC0 改体型 + 脚本 |
| Leopard_v083.fbx | 琥珀雷豹 | 同上 | -Y | 同上 |
| Lynx_v083.fbx | 赤焰山猫 | 同上 | -Y | 同上 |
| Pig_v083.fbx | 玄岩獠猪 | 新增 Attack、Hit；原 Idle/Walk/Run/Jump | -Y（同 Pig_v080） | Quaternius CC0 + 脚本 |

全部 11 个 FBX 已用 Blender 重新导入核对：网格已蒙皮、头部在 -Y 一侧、片段名正确。所有预览图在 `预览图\`。

**没有使用 AI 生成平台或下载任何新素材**——新模型都由 Blender 脚本生成，Quaternius 素材来自工程里已有的文件。

### 接入清单（给负责 Unity 的会话）

1. 把 FBX 放进 `Assets\Art\Models\v083\`（或同类目录），`M1WorldBuilderV077` 的 `Species` 表改用新路径：`moon_moth → Butterfly_v083`、`sky_bird → SpiritSparrow_v083`、`red_xiao → Vermilion_v083`、`azure_crane → Crane_v083`、`stone_turtle → RockTurtle_v083`、`glass_marten → Mink_v083`、`amber_leopard → Leopard_v083`、`ember_cat → Lynx_v083`、`rock_boar → Pig_v083`；主角改用 `Wizard_v083`；氛围苍鹰改用 `Eagle_v083`。
2. `ModelYaw`：新文件全部返回 0。注意现有规则按文件名前缀匹配（`Cat*` 返回 90、`StoneTurtle*` 返回 180），新文件名都不会命中，无需改代码。
3. 原 `Eagle.fbx` 头朝 Blender +Y，按同一换算在游戏里是倒着飞的；替换成 `Eagle_v083` 后即修正。
4. 飞行类新增的 `TakeOff / Land / Glide` 需要在控制器和 `M1FlyerMotion` 里接线（见 T7.9）。
5. 攻击伤害可按 T7.13 列出的命中时间延迟结算。
6. `Species` 尺寸参数建议按新模型重新调（仙鹤、灵蝶尤其需要），试玩后记录到 `04_验证记录\v0.8.3\`。

## 通用约定

- **朝向**：除特别说明外，新 FBX 在 Blender 里正面朝 **-Y**，用默认 FBX 导出设置导出 → 到 Unity 里正面朝 **+Z**，`ModelYaw` 应返回 **0**。
- **片段名**：导出后的片段名形如 `<骨架名>|Idle`，`M1AssetSetupV077.Short()` 取最后一个 `|` 之后的部分，即 `Idle / Walk / Run / Attack / Hit`（飞行类另有 `Flying / TakeOff / Land / Glide`）。
- **尺寸**：绝对尺寸不重要，Unity 端按包围盒自动缩放。
- **帧率**：24 fps；循环动作首尾帧相同。
- **步速**：四足 Walk / Run 的支撑脚后移速度按 `M1CreatureAnimator` 的“身高/秒”设计（Walk 0.8、Run 3.0，身高 = 静止姿势包围盒高度）；主角按 1.78 米身高、Walk 1.6 m/s、Run 5 m/s 设计。脚本逐帧检查，接入后不改阈值即可贴地不打滑。
- **材质名**：Unity 端 `Tint()` 会给灵兽材质染色，名字里含 `eye / black / claw / beak / nose / pink` 的材质保持原色。新模型的眼睛、花纹等需要保留颜色的材质都按此命名。
- **公用工具**：`脚本\m1lib.py`（导入导出、骨骼旋转、动作采样、联系表预览）。

---

## T7.8 主角巫师

| 项 | 内容 |
|---|---|
| 文件 | `FBX\Wizard_v083.fbx`（3.7 MB）；源文件 `Blender源文件\Wizard_v083.blend`；脚本 `脚本\t7_8_wizard.py` |
| 对应 | 主角「山海旅者」，替换 `Assets\Art\Models\v077\Characters\Wizard.fbx` |
| 动作片段 | **新做**：`Walk`（15 帧，0.625 s）、`Run`（12 帧，0.5 s）。**原样保留**：Death、Idle、Idle_Attacking、Idle_Weapon、PickUp、Punch、RecieveHit、RecieveHit_Attacking、Roll、Run_Weapon、Spell1、Spell2、Staff_Attack |
| 朝向 | 与原 Wizard.fbx 相同（Blender 里正面朝 -Y），Unity 里不需要改朝向 |
| 骨骼 / 材质 | 与原文件完全相同（CharacterArmature，材质 Wizard_Texture / Wizard_Staff_Texture），`ApplyTextures` 照常生效，法杖灵晶仍挂在 `Weapon.R_end` |
| 来源与授权 | Quaternius RPG Characters（CC0）；新动作为脚本生成，同属 CC0 可商用 |
| 预览图 | `预览图\T7.8_Wizard_Walk_侧面.png`、`T7.8_Wizard_Walk_前斜.png`、`T7.8_Wizard_Run_侧面.png`、`T7.8_Wizard_Run_前斜.png` |

**改了什么**

- 原 Walk / Run 的支撑脚后移速度只有约 1.0 m/s 和 2.3 m/s，游戏却按 1.6 m/s、5 m/s 播放，脚在地上明显打滑；原 Run 里法杖大幅甩动。
- 新动作用 IK 驱动脚步：支撑脚相对身体的后移速度换算到游戏尺度后**正好是 1.60 m/s（Walk）和 5.00 m/s（Run）**，与混合树阈值一致；冲刺 8 m/s 时原有的 1.6 倍加速播放仍然贴地。
- 脚跟着地 → 放平 → 绕脚尖抬脚跟蹬地；摆动腿抬脚、跑步时后踢；骨盆随步伐上下起伏（走路最高点在单脚支撑中段，跑步最低点在落地缓冲）、左右扭转与侧倾，上身反向扭转，头部稳住视线；跑步身体前倾约 11°。
- 左臂与左腿反向摆动（走 ±24°、跑 ±42°，跑步时屈肘约 60–90°）；右手持杖保持原 Walk / Run_Weapon 的握杖姿势，只随身体轻摆 3–5°，法杖始终竖直在身前。
- 烘焙为普通骨骼关键帧（不依赖 IK 约束），其它动作不受影响。

**已知问题 / 接入时注意**

- 步频：走 3.2 步/秒、跑 4 步/秒（Q 版短腿角色要贴地只能步子快）。冲刺时 ×1.6 为 6.4 步/秒，偏快但不打滑；如觉得太快，可把 `M1CharacterAnimator` 的加速上限从 1.6 调低，代价是冲刺时轻微打滑。
- 走路时骨盆比站立低约 6 厘米（短腿迈步需要），看起来略有屈膝。
- 跑步摆动腿后踢最高时 IK 够不到目标约 3 厘米（脚在空中，看不出来）。
- `Run_Weapon`（蓄力移动）未改动：游戏中蓄力移动速度 3 m/s、阈值 2.5 m/s，原动作的脚速约 2.3 m/s，基本吻合。

---

## T7.10 月羽灵蝶

| 项 | 内容 |
|---|---|
| 文件 | `FBX\Butterfly_v083.fbx`（0.3 MB，766 顶点）；源文件 `Blender源文件\Butterfly_v083.blend`；脚本 `脚本\t7_10_butterfly.py` |
| 对应 | 月羽灵蝶 `moon_moth`，替换现在的 `Creatures\Bat.fbx` |
| 动作片段 | `Flying`（10 帧循环，2.4 次/秒扑翼）、`Idle`（48 帧循环，停落时翅膀合拢在背上、缓慢张开晒翅再合上）、`Attack`（18 帧：举翅蓄力 → 连扑两下向前冲 0.55 个身长，腹部向前下方刺出）、`Hit`（12 帧：后仰侧翻、翅膀乱拍后恢复） |
| 朝向 | Blender 里头朝 **-Y**，默认设置导出 → Unity 里朝 +Z，`ModelYaw` 返回 0 |
| 骨骼 | Root → Body → Head / Abdomen → Abdomen2；四片翅膀各一根骨：Wing_FL、Wing_FR、Wing_HL、Wing_HR（绕身体纵轴扑动）。所有顶点都是刚性权重（翅膀 100% 跟随各自骨骼） |
| 材质 | `MoonMoth_WingInner`（翅芯，浅）、`MoonMoth_Wing`（翅面）、`MoonMoth_Body` 会被 Unity 端染成灵兽色；`MoonMoth_WingEdge_black`（紫褐色翅缘与尾突）、`MoonMoth_Spot_eye`（金色眼斑，带微弱自发光）、`MoonMoth_SpotCore_black`、`MoonMoth_Eye_black`、`MoonMoth_Leg_claw`、`MoonMoth_Antenna_claw` 保持原色 |
| 来源与授权 | 本项目用 Blender 脚本原创建模与动作，无外部素材，可商用 |
| 预览图 | `预览图\T7.10_Butterfly_Flying_前斜.png`、`T7.10_Butterfly_Flying_侧面.png`、`T7.10_Butterfly_Idle_停落扇翅.png`、`T7.10_Butterfly_Attack.png`、`T7.10_Butterfly_Hit.png` |

**造型**：参考月形天蚕蛾（luna moth）——浅色翅面、深色翅缘、每片翅一个金色眼斑，后翅拖出长尾突，呼应「月羽」。翅膀是加厚 0.014 单位（约为身长的 1.5%）的双面网格（Unity 默认单面渲染也不会从背面消失）。

**已知问题 / 接入时注意**

- 灵蝶是 `Flight.Hover`，在空中时 `M1FlyerMotion` 让混合树保持在移动段，播放的是 `Flying`（控制器按名字找 Walk 找不到时会用 Flying）。`Idle` 只在落地时可见。
- 下拍最低时翅尖比腿低约 0.5 个身长：只在空中播放没问题；预览图里贴着地面渲染，所以看起来翅膀插进地面。
- `Flying` 自带 ±0.07 的身体起伏，和 `M1FlyerMotion` 的 bob 叠加；如果显得太晃，可把灵蝶的 `bobAmplitude` 调小。
- 包围盒按翅膀平展计算（翼展约为身长的 2.3 倍），Unity 端按尺寸自动缩放后，身体会比蝙蝠模型小一些，可适当调大 `Species` 的尺寸参数（现在是 0.9）。

---

## T7.9 鸟类

### 评估结论

- 原 `Eagle.fbx` 有三个问题：①头朝 Blender +Y，按本项目的轴换算（Blender +Y → Unity -Z，同岚角灵兽、岩甲灵龟），`ModelYaw` 返回 0 时**在游戏里是倒着飞的**；②原 `Idle` 不是站姿，而是“身体水平、两翅笔直垂下”，Perch 类灵兽落地时翅膀会插进地面；③翅膀网格非常宽大（单翅长约身长 1.2 倍、羽面宽），在 Blender 里试过把它收拢到身侧，会折成一大块“披风”，效果不好。
- 因此：**苍鹰氛围飞鸟**继续用改进后的 `Eagle_v083.fbx`（主要在空中）；**灵雀、朱雀、仙鹤**各做一只专用低多边形鸟，翅膀分内外两段刚性羽板，能干净地收到身侧，体型差异一眼可分。三只共用一套骨骼命名与动作逻辑。

### Eagle_v083（苍鹰 / 过渡用）

| 项 | 内容 |
|---|---|
| 文件 | `FBX\Eagle_v083.fbx`（2.4 MB）；`Blender源文件\Eagle_v083.blend`；脚本 `脚本\t7_9_eagle.py` |
| 对应 | 氛围飞鸟「苍鹰」；三种灵鸟接入新模型前也可临时替换 `Eagle.fbx` |
| 动作片段 | `Idle`（72 帧，站立、半张翅护身、四处张望、中途抖翅；**重做**）、`Flying`（29 帧，原扑翼 + 下拍时身体上升、头部稳定）、`Glide`（48 帧循环滑翔，翼尖随气流弯曲、身体轻微侧倾）、`TakeOff`（26 帧：下蹲 → 蹬地扬翅 → 两次下拍，末帧接 Flying 第 1 帧）、`Land`（28 帧：滑翔 → 抬头张翅减速、双腿前伸 → 两次刹车扇翅 → 触地缓冲 → 站稳，末帧接 Idle）、`Attack`（22 帧：扬翅 → 收翅俯冲 → 伸爪伸颈啄击 → 扇翅拉起）、`Hit`（12 帧：被击退后仰侧翻、翅膀上扬） |
| 朝向 | **已转为朝 -Y**（整套绕 Z 转 180° 后应用变换），Unity 里朝 +Z，`ModelYaw` 返回 0 |
| 骨骼 / 材质 | 与原文件相同（EagleArmature，材质 Wings / Beak / Head / Claws，`ApplyTextures` 照常生效） |
| 来源与授权 | Quaternius Animal Pack Vol.2（CC0）；新动作为脚本生成 |
| 预览图 | `预览图\T7.9_Eagle_Idle_站立.png`、`T7.9_Eagle_TakeOff.png`、`T7.9_Eagle_Land.png`、`T7.9_Eagle_Attack_俯冲.png`、`T7.9_Eagle_Glide_Hit.png` |

已知问题：站立时两翅半张、向后拖得较长（原网格翅膀太大，收不拢；脚本自动选了不插地的最大下垂角）；TakeOff 前几帧和 Land 张翅时翅尖低于地面——这两段本来就在 `M1FlyerMotion` 抬升/下降的过程中，问题不大，接入后需实际看一下。

### 三只专用灵鸟

| 文件 | 对应灵兽 | 顶点 | 造型要点 |
|---|---|---|---|
| `FBX\SpiritSparrow_v083.fbx` | 青羽灵雀 `sky_bird` | 505 | 圆滚身体、大头短喙、头顶三根冠羽、短尾扇、粉色细腿；扑翼最快（8 帧一次） |
| `FBX\Vermilion_v083.fbx` | 赤霄朱雀 `red_xiao` | 696 | 修长身体、五根火焰冠羽、三条长尾羽（各带青色“眼斑”，三节骨骼可摆动）、金色腿 |
| `FBX\Crane_v083.fbx` | 苍羽仙鹤 `azure_crane` | 478 | 长腿长颈（4 节颈骨）、长直喙、红顶、黑颈、白身、黑色飞羽；飞行时颈伸直、双腿拖在身后（鹤的典型飞姿）；扑翼最慢（18 帧一次） |

| 项 | 内容 |
|---|---|
| 源文件 / 脚本 | `Blender源文件\SpiritSparrow_v083.blend`、`Vermilion_v083.blend`、`Crane_v083.blend`；脚本 `脚本\t7_9_spirit_birds.py`（物种参数表在文件开头，可改比例重新生成）+ `脚本\m1build.py` |
| 动作片段（三只相同） | `Idle`（72 帧，站立收翅、呼吸、张望、中途抖翅、翘尾）、`Flying`（循环扑翼：下挥快、上挥慢且外段飞羽内收；身体随下挥上升）、`Glide`（48 帧）、`TakeOff`（24 帧，末帧 = Flying 第 1 帧）、`Land`（26 帧，末帧 = Idle 第 1 帧）、`Attack`（20 帧，俯冲啄击）、`Hit`（12 帧） |
| 朝向 | Blender 里朝 **-Y**，Unity 里朝 +Z，`ModelYaw` 返回 0 |
| 骨骼 | Root → Body → Neck1..n → Head；Body → Tail1（朱雀另有 Tail2、Tail3）；Body → Wing1.L/R → Wing2.L/R；Body → Thigh.L/R → Shank.L/R → Toe.L/R |
| 静止姿势 | 双腿站立、翅膀水平展开。Unity 端按静止姿势包围盒最低点贴地，Idle 时脚趾正好在这个高度（脚本已检查：Idle 最低点 = 静止姿势最低点） |
| 材质 | `<前缀>_Body / _Belly / _Wing / _WingTip / _Crest / _Plume` 会被 Unity 端染成灵兽色（深浅关系保留）；`_Beak`、`_Leg_claw`、`_Eye_black`、朱雀 `_PlumeEye_eye`、仙鹤 `_WingTip_black` / `_Tail_black` / `_Neck_black` / `_Crest_pink`（红顶）保持原色。仙鹤的染色是白色，`Tint()` 遇到白色直接跳过，所以仙鹤保持作者配色 |
| 来源与授权 | 本项目用 Blender 脚本原创建模与动作，无外部素材，可商用 |
| 预览图 | 每只 3 张：`预览图\T7.9_<名字>_Idle站立.png`、`_Flying扑翼.png`、`_TakeOff_Land.png`（上排起飞、下排降落），另有 `_Attack_Glide_Hit.png` |

**接入时注意**

- 现有动画控制器只认 Idle / Walk(→Flying) / Attack / Hit；`TakeOff`、`Land`、`Glide` 需要在 `M1AssetSetupV077.Controller` / `M1FlyerMotion` 里接线（例如：Perch 类从地面起飞时触发 TakeOff，`lift` 降到 0 前触发 Land，高速直线飞行时混合到 Glide）。
- 体型差异主要靠模型本身；`Species` 的尺寸参数（灵雀 .85、朱雀 1.15、仙鹤 1.0）建议按新模型重新调：仙鹤站高约为身长 2 倍，按包围盒缩放后会显得身体偏小，可以调大到 1.4 左右。
- 鸟的 Attack 是“空中俯冲”：根骨向前下方位移约 1–1.5 个身长，再回位。如果攻击时鸟在地面，会先扬翅再前扑，看起来也说得通。
- Vermilion 的 Land 张翅刹车阶段，长尾羽最低到地面以下约 0.37 个单位（仍在空中阶段）。

---

## T7.11 岩甲灵龟

| 项 | 内容 |
|---|---|
| 文件 | `FBX\RockTurtle_v083.fbx`（0.7 MB，848 顶点）；`Blender源文件\RockTurtle_v083.blend`；脚本 `脚本\t7_11_turtle.py` |
| 对应 | 岩甲灵龟 `stone_turtle`，替换 `Beasts\StoneTurtle_Rigged.fbx` |
| 动作片段 | `Idle`（72 帧：呼吸、缓慢张望、末段打哈欠张嘴）、`Walk`（18 帧，四足依次迈步：左后→左前→右后→右前，身体向抬脚对侧侧倾、头随步伐前探、尾巴摆动）、`Run`（9 帧，对角小跑、身体抬高前倾）、`Attack`（18 帧：缩颈张口蓄力 → 猛地伸颈 0.48 个单位前咬、合口 → 收回）、`Hit`（12 帧：龟壳一震后仰，头尾缩进壳里） |
| 朝向 | Blender 里朝 **-Y**，Unity 里朝 +Z。**注意**：旧灵龟朝 +Y，`ModelYaw` 对 `StoneTurtle*` 返回 180；新文件名是 `RockTurtle_v083`，不匹配那条规则，返回 0 正好 |
| 骨骼 | Root → Body → Neck → Head → Jaw（下颌可张合）、Body → Tail、Body → Up.XX → Low.XX；Foot.XX 挂在 Root 下（XX = FL/FR/HL/HR）。导出时只含变形骨（IK 辅助骨 Pole.* 不导出） |
| 造型改进 | 龟甲由车削穹顶构成，按环和扇区交错上色形成盾片花纹，下缘一圈浅色甲缘；背上五块岩石 + 正中三枚发光玉晶；粗壮柱状四肢、扁脚掌带三只爪；头部有吻部、眼、岩质眉骨和独立下颌；颈部是可伸缩的平滑管 |
| 材质 | 物种染色为白色，`Tint()` 跳过，保持作者配色。`RockTurtle_Jade` 带自发光 |
| 来源与授权 | 本项目用 Blender 脚本原创建模与动作（参考旧版比例），可商用 |
| 预览图 | `预览图\T7.11_RockTurtle_Walk侧面.png`、`T7.11_RockTurtle_Idle_Run俯视.png`、`T7.11_RockTurtle_Attack伸颈咬_Hit.png` |

**步速**：支撑脚后移速度按 `M1CreatureAnimator` 的身高归一速度设计——Walk 正好 0.8、Run 正好 3.0 个身高/秒（脚本逐帧检查），与混合树阈值一致，不打滑。

**已知问题**：Run 迈步最远时 IK 够不到目标约 0.1 个单位（脚抬在空中时，看不出来）；静止姿势下肘部与 IK 求解结果有约 0.05 的偏差（IK 的肘朝向由辅助骨决定）。

---

## T7.12 猫系三种灵兽

| 文件 | 对应灵兽 | 体型改动 |
|---|---|---|
| `FBX\Mink_v083.fbx` | 琉璃水貂 `glass_marten` | 身体加长约 35%、变细，四肢缩短到 62%，小头，尾巴加粗并放平拖在身后 |
| `FBX\Leopard_v083.fbx` | 琥珀雷豹 `amber_leopard` | 躯干、颈、四肢加粗 20–28%，四肢略长，头大 14%，背部和体侧加深色斑点（网格先平直细分一级，1611 面中 134 面为斑点） |
| `FBX\Lynx_v083.fbx` | 赤焰山猫 `ember_cat` | 腿长 16%、爪子大 30%，耳尖黑色簇毛，两颊各三束须毛，短尾（约原长 30%） |

| 项 | 内容 |
|---|---|
| 源文件 / 脚本 | `Blender源文件\Mink_v083.blend`、`Leopard_v083.blend`、`Lynx_v083.blend`；脚本 `脚本\t7_12_cats.py`（体型参数表在文件开头） |
| 做法 | 按骨骼缩放摆姿势 → 烘进网格 → 设为新的静止姿势，所以原动作（骨骼旋转）照常可用；IK 目标骨同步移到新爪位 |
| 动作片段 | `Idle`（原 Quaternius 动作，保留）、`Walk`（**新做**，16 帧，四拍步态：左后→左前→右后→右前）、`Run`（**新做**，10 帧，旋转式奔跑：前腿一前一后着地、后腿蹬地，脊柱一屈一伸，尾巴压低）、`Attack`（**新增**，18 帧：伏低蓄力、尾尖抖动 → 前扑，右前爪高举挥下 → 回位）、`Hit`（**新增**，12 帧：向后缩、低头、尾巴下压）。原 `Walking` 已由 `Walk` 取代 |
| 步速 | 与 `M1CreatureAnimator` 一致：Walk 支撑脚后移正好 0.8、Run 正好 3.0 个身高/秒（脚本逐帧检查），每种体型按各自身高单独计算 |
| 朝向 | **已转为朝 -Y**（原猫模型身体沿 X、头朝 +X，所以现在 `ModelYaw` 对 `Cat*` 返回 90）。新文件名不以 Cat 开头，`ModelYaw` 返回 0 即可 |
| 材质 | 原有 Grey / White / Pink；新增 `Leopard_Spot_black`、`Lynx_Tuft_black`（保持深色，不被染色）。Grey、White 会被 Unity 端染成各自灵兽色 |
| 来源与授权 | Quaternius Animal Pack Vol.2（CC0）改体型；新增网格与动作为脚本生成 |
| 预览图 | 每只 3 张：`预览图\T7.12_<名字>_体型.png`（侧面 + 前斜）、`_Walk_Run.png`（前 4 帧走、后 5 帧跑）、`_Attack_Hit.png` |

**已知问题**

- 雷豹的斑点是按面上色，近看是方块状（低多边形风格，远看是斑点）。若想要更圆的斑，可以把细分改为两级（面数约 6400）。
- Walk 中脚抬到最高时 IK 偶尔差 0.1–0.14 个单位（脚在空中）。
- 原 Idle 动作是按原比例做的，水貂腿变短后站立时身体略低于静止姿势，看不出问题。
- 原 `Cat_v080.fbx`（三只共用、带 Run）仍可作为回退。

---

## T7.13 灵兽攻击动作

| 灵兽 | 文件 | Attack 内容 |
|---|---|---|
| 玄岩獠猪 `rock_boar` | `FBX\Pig_v083.fbx`（1.8 MB；`Blender源文件\Pig_v083.blend`；脚本 `脚本\t7_13_pig.py`） | **冲撞**（22 帧）：后坐低头、右前蹄刨地两下 → 四腿快速交替、身体压低前冲约 0.28 个身长 → 撞击瞬间猛地抬头、獠牙上挑 → 收势回位。另加 `Hit`（12 帧：后缩、抬头、歪头） |
| 岩甲灵龟 | `FBX\RockTurtle_v083.fbx` | **伸颈咬**：见 T7.11 |
| 灵雀 / 朱雀 / 仙鹤 / 苍鹰 | 见 T7.9 | **俯冲啄击**：扬翅 → 收翅俯冲 → 伸爪伸颈啄 → 扇翅拉起 |
| 水貂 / 雷豹 / 山猫 | 见 T7.12 | **扑击挥爪** |
| 月羽灵蝶 | 见 T7.10 | 举翅蓄力 → 前冲、腹部刺出 |

`Pig_v083.fbx` 保留原 Idle / Walk / Run / Jump，朝向与 Pig_v080 相同（Blender 里朝 -Y）。控制器按名字优先匹配 `Attack`，接入后就不会再拿 Jump 当攻击。来源：Quaternius Farm Animals（CC0）+ v0.8.0 Blender 补的走跑。预览图：`预览图\T7.13_Pig_Attack冲撞_侧面.png`、`T7.13_Pig_Attack前斜_Hit.png`。

**伤害时机（给接入时参考，路线图要求“攻击动作和伤害时机对齐”）**：各 Attack 的命中帧约为——猪第 14–16 帧（0.6 s）、龟第 10 帧（0.4 s）、猫第 10–11 帧（0.45 s）、灵鸟第 12–13 帧（0.5 s）、苍鹰第 14 帧（0.58 s）、蝶第 8 帧（0.33 s）。目前 `M1CreatureAnimator.Attack()` 只触发动画，伤害在代码里立即结算，建议按这些时间延迟结算。

**攻击时朝向目标**属于代码侧（转身朝向），模型这边动作都是朝正前方（+Z）出招。

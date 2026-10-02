# 苦难摇篮（ArknightsChernobog）

《杀戮尖塔 2》第二幕变体，取材《明日方舟》主线第七章“苦难摇篮”：切尔诺伯格核心城，
感染者游击队“盾”，Boss 固定为爱国者。与原版蜂巢同属第二幕候选，开局时随机二选一。

- 创意工坊：<https://steamcommunity.com/sharedfiles/filedetails/?id=3810696357>
- 模组 ID：`ArknightsChernobog`，作者 `Natsuki`
- 目标游戏版本：STS2 `0.111.0`，单 DLL，前置 RitsuLib（≥ 0.6.2，全部内容经它注册，见下方“结构”）
- 当前状态：幕已能被随机抽中，战斗背景、休息处、地图底图是本幕美术；弱怪/普通/精英战斗池已换成第七章敌人（第三版设计，按兵种编组）；
  Boss 为爱国者（两阶段，带两名游击队战士护卫）；事件池是 6 个本幕事件 + 蜂巢里 3 个主题中性的事件，先古之民沿用蜂巢

## 安装

推荐直接在创意工坊订阅本模组和前置 [RitsuLib](https://steamcommunity.com/sharedfiles/filedetails/?id=3747602295)（≥ 0.6.2）。
手动安装：把发行包里的 `ArknightsChernobog/` 文件夹（`ArknightsChernobog.dll`、`.pck`、`.json` 三个文件）放进游戏的 `mods` 目录。

## 许可

代码、构建工具、本地化文本和为本模组创作的美术以 [MIT](LICENSE) 许可开源。
以下第三方素材不在 MIT 范围内，版权归原方所有，仅作同人、非商业用途（英文说明见 [NOTICE](NOTICE.md)）：

- 《明日方舟》角色、敌人 Spine 动画（`assets/animations/`、`assets/_imported/`）及相关设计：鹰角网络（Hypergryph）；
- `assets/audio/music/` 中的 Boss 战音乐：塞壬唱片（Monster Siren Records）；
- 《杀戮尖塔2》本体及其资源：Mega Crit。

## 事件

写法照集成战略事件：`src/Events/ReunionEventModel.cs` 继承 RitsuLib 的 `ModEventTemplate`，带选项/效果助手；
`src/Events/ChernobogEvents.cs` 是六个事件，`ModEntry.ActEventTypes` 经 `ModContentRegistry.RegisterActEvent` 只注册到本幕，
模型 id 形如 `ARKNIGHTS_CHERNOBOG_EVENT_SUPPLY_CACHE`（新增只往末尾追加，存档按 id 引用）。
本地化是 `localization/*/events.json`、`cards.json`，由 `tools/write_event_loc.py` 生成，选项里的数值与事件类里的常量一一对应。
立绘在 `assets/images/events/`（原版 3440×1616，主体在左、右半边压暗给文字面板），诅咒“矿石病”（`src/Cards/Oripathy.cs`）的卡图在 `assets/images/cards/`。

| 事件 | 选项 |
|---|---|
| 冻土补给箱（共享事件） | 回复 18 生命 / 攻击牌三选一 / 与两只猎犬战斗，胜后额外 75 金币 |
| 游击队的篝火 | 花 50 金币，最大生命 +8 / 升级 1 张牌 / 离开 |
| 源石矿脉 | 稀有牌三选一 + 矿石病 / 失去 9 生命得 85 金币 / 离开 |
| 核心城动力阀 | 失去 7 生命移除 1 张牌 / 变化 2 张牌 / 离开 |
| 罗德岛空投 | 回复 30% 最大生命 / 2 瓶随机药水 |
| 黑蛇的低语 | 失去 6 最大生命得随机遗物 / 150 金币 + 矿石病 / 拒绝 |

矿石病：不能打出；回合结束时若在手牌中，受到等同于当前回合数的伤害（不设上限），比原版腐朽（固定 2）更怕拖长的战斗。它在原版诅咒卡池里，随机诅咒效果也可能给出它。

## 结构

幕、战斗池、事件、怪物、能力、诅咒都经前置 RitsuLib 注册（`src/ModEntry.cs`），资源全部放在模组命名空间
`res://ArknightsChernobog/`，经 RitsuLib 的 `AssetProfile` 接到原版取路径的地方；原版命名空间下不放任何文件，也不打自己的 Harmony 补丁
（`ChernobogSelfTest` 的开发期挂钩除外）。注册后的模型 id 统一为 `ARKNIGHTS_CHERNOBOG_<类别>_<类名>`，
如幕 `ARKNIGHTS_CHERNOBOG_ACT_CHERNOBOG_ACT`、爱国者 `ARKNIGHTS_CHERNOBOG_MONSTER_REUNION_PATRIOT`；存档、联机与控制台都按它引用，不要改类名。

- `src/ModEntry.cs`：注册表。`ActEncounterTypes`（战斗池）与 `ActEventTypes`（本幕事件）只往末尾追加；怪物与能力按类型全集注册。
- `src/Acts/ChernobogAct.cs`：幕模型（`ModActTemplate`）。`AllowInRandomActList` 让 RitsuLib 把它并进原版第二幕的随机候选，与蜂巢同组。
- `src/ChernobogAssets.cs`：全部资源路径（幕背景、休息处、地图底图、Boss 节点与对局历史图标、遭遇站位场景、能力图标），与 `art/install_assets.py` 一一对应。
- `src/Encounters/`：遭遇战基类 `ReunionEncounter`（`ModEncounterTemplate`）；`ReunionTestEncounters.cs` 与事件战斗只供控制台/事件发起，
  不注册、不进战斗池，保留原版按类名得出的 id（如 `REUNION_PATRIOT_TEST`）。
- `src/Monsters/`：第七章敌人，基类 `ReunionMonster`（`ModMonsterTemplate`），类名统一带 `Reunion` 前缀；
  `ReunionMonsterSelfTest.cs` 是自检里的敌人/场景/测试遭遇战检查。
- `src/Powers/`：机制能力（`ModPowerTemplate`），图标走 `PowerAssetProfile`。
- `src/Audio/ReunionBossMusic.cs`：爱国者战音乐，挂在 RitsuLib 的战斗生命周期事件上，经它的音频服务流式播放；流式文件不走 FMOD 音乐总线，音量每帧乘上音乐总线音量跟随游戏滑条。
- `src/ChernobogSelfTest.cs`：`CHERNOBOG_SELFTEST=1` 时在启动后自检注册、选幕、生成房间、各项资源是否都走模组路径。
- `assets/`：打包到 `res://ArknightsChernobog/`。
  - `localization/{zhs,eng}/`：`acts`、`monsters`、`encounters`、`powers`、`events`、`cards` 六张原版同名表（游戏只合并原版同名表），
    键是注册后的模型 id；`tools/write_event_loc.py` 生成事件与卡牌两张。
  - `animations/monsters/arknights/<后缀>/` 与 `_imported/`：PRTS Spine 资源（已转 Spine 4.2.43，坏时间轴由 `tools/fix_spine_timelines.py` 修过）；
    `scenes/creature_visuals/` 是敌人场景。
  - `images/`、`scenes/backgrounds/`、`scenes/rest_site/`：由 `art/install_assets.py` 从 `art/final/` 安装、生成；背景图层与休息处场景
    继承蜂巢场景（保留粒子、光效与脚本），只覆盖贴图，并隐藏按蜂巢几何画的补光、树根环与虫光。`scenes/encounters/` 是手写的站位场景。
  - `audio/music/`：爱国者 Boss 战音乐（塞壬唱片，monster-siren.hypergryph.com 原件，未转码）：
    `reunion_patriot_boss.mp3` 一阶段《Непоколебимость（不屈）》（曲目 048732），`reunion_patriot_boss_phase2.mp3` 二阶段《视死如归》（曲目 461180）。
    FMOD 读原文件，`.import` 为 `keep`（不导入），原文件进包。
  - 构建时先用 Godot 4.5.1 编辑器把这棵树导入成 ctex 再打包，图片只打 `.import` 与 ctex；Spine 图集按 ResourceLoader 读贴图，裸 PNG 在运行时加载不了。
- `art/`：美术流水线，定稿贴图由本机 Codex 生成。
  - `ART_BRIEF.md`：需求说明；`codex/style.md`、`codex/job_header.md`、`codex/assets/*.md` 是实际提示词，
    `codex/make_*_prompts.py` 生成它们，`codex/run_job.sh <素材名> [参考图...]` 调 ChatGPT.app 自带的 `codex exec` 出一张图到 `generated/`。
  - `process_generated.py`：把生成图规整到原版精确尺寸（地图长图切三段）输出到 `final/`；
    `install_assets.py`：把 `final/` 装进 `assets/` 并生成背景图层、休息处场景。
  - `reference/`、`codex/previews/` 含原版资源导出，`generated/`、`final/`、`codex/runs/` 是中间产物，都不入库。

## 构建与验证

需要 macOS（脚本是 zsh，路径按 Steam 的 macOS 安装位置）、.NET 9 SDK、Godot 4.5.1 Mono 编辑器（用于把贴图导入成 ctex，主次版本要与游戏运行时一致），
以及已安装的《杀戮尖塔2》和已订阅的 RitsuLib。在模组根目录执行：

```zsh
tools/build_and_deploy.sh
```

产物在 `dist/`，默认会部署到游戏的 `mods/ArknightsChernobog/`；只构建不部署：`CHERNOBOG_DEPLOY=0`。路径都可以用环境变量覆盖：

| 变量 | 默认值 |
|---|---|
| `STS2_GAME_APP` | `~/Library/Application Support/Steam/steamapps/common/Slay the Spire 2/SlayTheSpire2.app` |
| `GODOT_EDITOR` | `/opt/homebrew/bin/godot` |
| `CHERNOBOG_REFS_ROOT` | 按版本存档的游戏程序集（`<目录>/<版本>/game-refs/`）；不存在时直接引用已安装游戏的程序集 |
| `CHERNOBOG_STS2_TARGET` | `0.111.0` |

RitsuLib 默认从创意工坊安装目录引用，放在别处时给 `dotnet build` 传 `-p:RitsuLibRoot=<目录>`。部署后做隔离 headless 验证时必须加 `--force-steam=off`，
并设置 `CHERNOBOG_SELFTEST=1`，日志里搜 `[ArknightsChernobog][SelfTest]`，应为 0 failure。
关掉 Steam 后创意工坊里的 RitsuLib 不会加载，本模组会因缺前置被跳过：用 `cp -cR` 把游戏 .app 做成 APFS 克隆，
把 RitsuLib 放进克隆的 `Contents/MacOS/mods/STS2-RitsuLib/`，验证在克隆上跑；不要往真实游戏目录放第二份 RitsuLib。

敌人场景可以脱离游戏流程单独验证（游戏自带引擎带 Spine 运行时；`Skeleton version ... does not match` 视为转换失败）：

```zsh
"<SlayTheSpire2.app>/Contents/MacOS/Slay the Spire 2" --headless --path tools \
  -s res://validate_creature_scenes.gd -- <绝对路径>/ArknightsChernobog/dist/ArknightsChernobog.pck \
  res://ArknightsChernobog/scenes/creature_visuals/reunion_patriot.tscn
```

## 第七章敌人（招式与数值见 docs/战斗设计.md）

资源来自 PRTS 页面 `SPINEDATA`（`https://torappu.prts.wiki/assets/enemy_spine/<file>/`），用
`sts2-spine-enemy-import` 技能的 `prepare_prts_spine_asset.py` 下载并由 Spine 3.8.99 转 4.2.43。
PRTS 会拦截简单 UA，抓页面要带完整浏览器请求头；资源 CDN 不拦。

| 敌人 | 模型类 / 场景 | PRTS 资源 | 用到的动画 |
|---|---|---|---|
| 爱国者 | `ReunionPatriot` / `reunion_patriot` | `enemy_1506_patrt` | 一阶段 Idle_1、Attack_1、Move_1；倒地 revive_1、revive_2；重生 revive_3；二阶段 Idle_2、Attack_2、Move_2、Skill（投枪）；Die 只在真正倒下时播 |
| 游击队战士 | `ReunionGuerrillaFighter` / `reunion_guerrilla_fighter` | `enemy_1078_sotisc` | Idle、Attack、Die |
| 游击队猎犬 | `ReunionGuerrillaHound` / `reunion_guerrilla_hound` | `enemy_1077_sotihd` | Idle、Attack、Die |
| 游击队狙击手 | `ReunionGuerrillaSniper` / `reunion_guerrilla_sniper` | `enemy_1079_sotisp` | Idle、Attack、Die |
| 游击队传令兵 | `ReunionGuerrillaHerald` / `reunion_guerrilla_herald` | `enemy_1080_sotidp` | Idle、Attack、Die |
| 游击队盾卫 | `ReunionGuerrillaShieldGuard` / `reunion_guerrilla_shield_guard` | `enemy_1081_sotisd` | Idle、Attack、Die |
| 游击队迫击炮兵 | `ReunionGuerrillaMortarGunner` / `reunion_guerrilla_mortar_gunner` | `enemy_1082_soticn` | Idle_1、Idle_2（装填）、Attack、Die |
| 游击队突袭战士 | `ReunionGuerrillaAssaulter` / `reunion_guerrilla_assaulter` | `enemy_1083_sotiab` | Idle、Attack、Start（降落突袭）、Die |
| 游击队萨卡兹战士 | `ReunionGuerrillaSarkazWarrior` / `reunion_guerrilla_sarkaz_warrior` | `enemy_1084_sotidm` | Idle/Attack/Die，强化后 Idle_2/Attack_2/Die_2 |
| 游击队萨卡兹术师 | `ReunionGuerrillaSarkazCaster` / `reunion_guerrilla_sarkaz_caster` | `enemy_1085_sotiwz` | Idle、Idle_2（强化）、Die；skel 没有攻击动画 |
| 游击队猎犬pro | `ReunionGuerrillaHoundPro` / `reunion_guerrilla_hound_pro` | `enemy_1077_sotihd_2` | 同猎犬 |
| 游击队战士组长 | `ReunionGuerrillaFighterLeader` / `reunion_guerrilla_fighter_leader` | `enemy_1078_sotisc_2` | 同战士 |
| 游击队狙击手组长 | `ReunionGuerrillaSniperLeader` / `reunion_guerrilla_sniper_leader` | `enemy_1079_sotisp_2`（只有图集/贴图，skel 复用 `enemy_1079_sotisp`） | 同狙击手 |
| 游击队传令兵组长 | `ReunionGuerrillaHeraldLeader` / `reunion_guerrilla_herald_leader` | `enemy_1080_sotidp_2` | 同传令兵 |
| 游击队盾卫组长 | `ReunionGuerrillaShieldGuardLeader` / `reunion_guerrilla_shield_guard_leader` | `enemy_1081_sotisd_2` | 同盾卫 |
| 游击队迫击炮兵组长 | `ReunionGuerrillaMortarGunnerLeader` / `reunion_guerrilla_mortar_gunner_leader` | `enemy_1082_soticn_2` | 同迫击炮兵 |
| 游击队突袭战士组长 | `ReunionGuerrillaAssaulterLeader` / `reunion_guerrilla_assaulter_leader` | `enemy_1083_sotiab_2` | 同突袭战士 |
| 游击队萨卡兹战士组长 | `ReunionGuerrillaSarkazWarriorLeader` / `reunion_guerrilla_sarkaz_warrior_leader` | `enemy_1084_sotidm_2` | 同萨卡兹战士（两套形态） |
| 游击队萨卡兹术师组长 | `ReunionGuerrillaSarkazCasterLeader` / `reunion_guerrilla_sarkaz_caster_leader` | `enemy_1085_sotiwz_2` | 同萨卡兹术师 |
| 宿主士兵 | `ReunionHostSoldier` / `reunion_host_soldier` | `enemy_1043_zomsabr`（资源目录 `enemy_1043_zomsbr`） | Idle、Attack、Die |
| 宿主拾荒者 | `ReunionHostScavenger` / `reunion_host_scavenger` | `enemy_1044_zomstr` | Idle、Attack、Die |
| 宿主流浪者 | `ReunionHostWanderer` / `reunion_host_wanderer` | `enemy_1044_zomstr_2` | Idle、Attack、Die |
| 宿主士兵组长 | `ReunionHostSoldierLeader` / `reunion_host_soldier_leader` | `enemy_1043_zomsabr_2`（资源目录 `enemy_1043_zomsbr_2`） | Idle、Attack、Die |
| 狂暴宿主士兵 | `ReunionRagingHostSoldier` / `reunion_raging_host_soldier` | `enemy_1062_rager` | Idle、Attack、Die |
| 狂暴宿主投掷手 | `ReunionRagingHostThrower` / `reunion_raging_host_thrower` | `enemy_1063_rageth` | Idle、Attack（2.5 秒，出手 1.47 秒）、Die |
| 狂暴宿主组长 | `ReunionRagingHostLeader` / `reunion_raging_host_leader` | `enemy_1062_rager_2` | Idle、Attack、Die |
| 特战士兵 | `ReunionSpecOpsSoldier` / `reunion_spec_ops_soldier` | `enemy_1037_lunsabr`（资源目录 `enemy_1037_lunsbr`） | Idle、Attack、Die |
| 特战术师 | `ReunionSpecOpsCaster` / `reunion_spec_ops_caster` | `enemy_1038_lunmag` | Idle、Attack、Die |
| 法术大师A1 | `ReunionArtsMaster` / `reunion_arts_master` | `enemy_1041_lazerd` | Idle、Attack、Die |
| 雇佣军萨卡兹战士 | `ReunionMercenarySarkazWarrior` / `reunion_mercenary_sarkaz_warrior` | `enemy_1084_sotidm_3` | Idle、Attack、Attack_2（裂斩）、Die |
| 雇佣军萨卡兹术师 | `ReunionMercenarySarkazCaster` / `reunion_mercenary_sarkaz_caster` | `enemy_1085_sotiwz_3` | Idle、Die；skel 没有攻击动画 |

游击队组长、宿主流浪者/组长、狂暴宿主组长与雇佣军都和对应本体同一套骨骼与动画，但除狙击手组长外 skel 里的附件网格
（UV、区域尺寸）随各自贴图重新打包过，不能只换图集套用本体 skel，所以单独转换。狙击手组长的 skel 与本体逐字节相同，
场景的 `_skel_data.tres` 直接引用本体的 `enemy_1079_sotisp.skel.spskel`。

### 控制台测试

原版 `fight <id>` 直接按 id 查遭遇战（不分大小写）。战斗池里的遭遇 id 带模组前缀（如 `fight arknights_chernobog_encounter_reunion_patriot_boss`），
Tab 补全能列出来；下面这些测试遭遇不注册、不进战斗池，id 不带前缀，要手打。强制进入本幕：`act arknights_chernobog_act_chernobog_act`。

```text
fight reunion_patriot_test
fight reunion_guerrilla_fighter_test
fight reunion_guerrilla_hound_test          # 两只猎犬
fight reunion_guerrilla_sniper_test
fight reunion_guerrilla_herald_test
fight reunion_guerrilla_shield_guard_test
fight reunion_guerrilla_mortar_gunner_test
fight reunion_guerrilla_assaulter_test
fight reunion_guerrilla_sarkaz_warrior_test
fight reunion_guerrilla_sarkaz_caster_test
fight reunion_guerrilla_squad_test          # 战士 + 传令兵 + 狙击手
fight reunion_sarkaz_ritual_test            # 萨卡兹战士 + 术师
fight reunion_guerrilla_hound_pro_test
fight reunion_guerrilla_fighter_leader_test
fight reunion_guerrilla_sniper_leader_test
fight reunion_guerrilla_herald_leader_test
fight reunion_guerrilla_shield_guard_leader_test
fight reunion_guerrilla_mortar_gunner_leader_test
fight reunion_guerrilla_assaulter_leader_test
fight reunion_guerrilla_sarkaz_warrior_leader_test
fight reunion_guerrilla_sarkaz_caster_leader_test
fight reunion_host_soldier_test
fight reunion_host_scavenger_test
fight reunion_host_wanderer_test
fight reunion_host_soldier_leader_test
fight reunion_raging_host_soldier_test
fight reunion_raging_host_thrower_test
fight reunion_raging_host_leader_test
fight reunion_host_herd_test                # 宿主士兵 + 士兵组长 + 拾荒者
fight reunion_spec_ops_soldier_test
fight reunion_spec_ops_caster_test
fight reunion_arts_master_test
fight reunion_spec_ops_team_test            # 特战士兵 + 特战术师 + 法术大师A1
fight reunion_mercenary_sarkaz_warrior_test
fight reunion_mercenary_sarkaz_caster_test
```

场景的缩放、Bounds 与标记点只按骨骼范围粗配，站位、朝向、命中时机和死亡动画长度（爱国者 Die 长 5.3 秒）要进游戏看。

## 本幕战斗池

弱怪 4 / 普通 10 / 精英 4，编组照原版：一场只围绕一种兵（单体、同种成群、头目带同族、召唤型），同一群怪有弱怪版和普通版并打同一个遭遇标签，
部分遭遇战阵容随机。定义在 `src/Encounters/ChernobogEncounters.cs`，由 `ChernobogAct.GenerateAllEncounters` 接入；
萨卡兹雇佣军是换皮，不进池；Boss 固定为爱国者（`ReunionPatriotBoss`，`fight reunion_patriot_boss`）。
各敌人机制、数值（对照原版蜂巢的血量与伤害）、每场组成见 [战斗设计](docs/战斗设计.md)（第三版，待审）。
各怪的起手招由遭遇战的 `SetOpenings` / `SetOpeningsByType` 指定，取值来自 `docs/tools/opening_sim.py`（按原版蜂巢的开局伤害区间模拟前 4 回合，
含召唤与随机阵容），改阵容或数值后要重跑它。自检会逐场核对标题本地化，用多个种子抽随机阵容，把每个组合的总血量与实际起手招打进日志。
这些遭遇战同样可以用 `fight <id>` 直接进，例如 `fight reunion_vertical_strike_elite`。

## 已知限制

- 单人模式下，未发现过的非默认幕会被强制选中一次（原版对暗港也是如此）。
- 集成战略事件按 `typeof(Hive)` 限定第二幕事件，这些事件暂时不会出现在本幕。

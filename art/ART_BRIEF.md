# 苦难摇篮 美术需求

场景：《明日方舟》主线第七章“苦难摇篮”，切尔诺伯格核心城的枢纽层与中央区。
这里曾是乌萨斯移动城市的心脏，现在被感染者游击队“盾”占据：冰冷的钢铁甲板、断裂的管线、积雪、
黑色源石结晶带橙色辉光的裂纹、萨卡兹源石祭坛。

对照图在 `art/reference/`（原版“蜂巢”资源导出，只作构图参考，不入库）：

| 文件 | 用途 |
|---|---|
| `previews/combat_layers_sheet.png` | 战斗背景 11 个图层，品红色 = 透明区域 |
| `previews/combat_composite_a.png` | 图层叠起来的效果 |
| `previews/map_stack.png` | 地图底图上中下三段拼接效果 |
| `hive/hive_rest_site_00.png` | 休息处底图 |
| `previews/rest_site_props.png` | 休息处道具：地面碎屑、左右遮幅、左右座位、火堆 |

## 通用约定

- 交付 PNG。需要透明的图层，工具不支持透明就用纯绿底 `#00FF00` 出图，我这边抠。
- 尺寸按下面表格；比例对就行，我会缩放到精确尺寸。
- 图里不要出现文字、标志、水印、人物。
- 整体明度偏暗、饱和度偏低，和原版对照图的亮度范围接近；游戏会在上面叠加光效和粒子。

### 通用风格前缀（每条提示词前都加上）

```
Slay the Spire 2 style hand-painted 2D game background, bold dark silhouettes with clean shapes,
painterly texture, limited muted palette, strong value separation between depth layers,
side-view stage composition, no characters, no text, no logos.
Setting: the frozen core district of a colossal abandoned mobile industrial city in a cold northern
empire, occupied by a hardened guerrilla army. Cold steel-blue and concrete grey, dirty snow,
rusted orange metal, black crystalline growths with glowing amber-orange veins.
```

## 一、战斗背景（最优先）

每组随机选一张叠在一起，所以同组的几张必须同景深、同地平线、互相可替换。
原图 2048×960，游戏里放大约 1.4 倍显示。
画面中间横带（纵向约 y=450–800）是角色站位区，不要放高大的遮挡物。

| 交付文件 | 尺寸 | 透明 | 内容 |
|---|---|---|---|
| `chernobog_act_00.png` | 2048×960 | 否 | 最远景 + 地面。地平线约 y≈280；下方是甲板地面 |
| `chernobog_act_01_a.png` `_01_b.png` | 2048×960 | 是 | 顶部横条（约 y 0–320）：上层甲板底面、桁架、垂挂电缆；以下透明 |
| `chernobog_act_02_a.png` `_02_b.png` `_02_c.png` | 2048×960 | 是 | 中景剪影（约 y 0–480，落地线约 y 330–450）：远处楼群、塔吊、源石结晶簇；雾化、低对比；y≈490 以下透明 |
| `chernobog_act_03_a.png` `_03_b.png` `_03_c.png` | 2048×960 | 是 | 近景左右两侧的框景物（落地约 y≈600）：粗柱、断墙、锈蚀机械；其中一张放萨卡兹源石祭坛。画面中间 40% 保持通透；底部可有淡出的倒影/阴影 |
| `chernobog_act_04_a.png` `_04_b.png` | 2048×960 | 是 | 最前景：只占底部（约 y 660–960），近黑色碎石、钢筋残骸剪影；两侧高、中间低 |

提示词：

- **00 远景 + 地面**
  ```
  [风格前缀] Full opaque backdrop, 2048x960. Deep interior of a gigantic city hub level:
  distant layered industrial megastructures, huge pipes and support pillars fading into cold smog,
  faint pale sky through gaps high above. Horizon line at about 30% from the top. Lower two thirds
  is a wide flat steel deck floor seen at a low angle, riveted plates with cracks, thin snow drifts,
  dark stains, a few faint amber crystal glints. Dim, cold, oppressive.
  ```
- **01 顶部横条（a/b 两张变体）**
  ```
  [风格前缀] Top strip only, 2048x960 canvas, content in the top third, everything below
  transparent (or pure #00FF00). Underside of the upper city deck: steel girders, hanging cables,
  broken ducts and icicles, dark silhouette with subtle rim light. Variant [a: dense girders /
  b: thinner, with a torn gap showing sky].
  ```
- **02 中景剪影（a/b/c 三张变体）**
  ```
  [风格前缀] Midground silhouette band, 2048x960 canvas, content from the top down to about half
  height, ground contact around 40% height, everything below transparent (or #00FF00).
  Hazy low-contrast silhouettes: [a: collapsed apartment blocks and a leaning tower crane /
  b: rows of rusted storage tanks and pipelines / c: clusters of black originium crystals growing
  out of ruined buildings, faint amber glow]. Leave sky gaps transparent.
  ```
- **03 近景框景（a/b/c 三张变体）**
  ```
  [风格前缀] Near-midground framing elements on the left and right thirds only, 2048x960 canvas,
  objects stand on a ground line at about 62% height, the central 40% stays open, background
  transparent (or #00FF00). Darker and higher contrast than the midground. [a: massive concrete
  pillars with torn banners and barbed wire / b: broken bulkhead wall and rusted heavy machinery
  / c: a crude ritual altar of black originium crystals with glowing amber veins on one side,
  sandbag barricade on the other]. A soft fading shadow below the ground line is allowed.
  ```
- **04 最前景（a/b 两张变体）**
  ```
  [风格前缀] Foreground only along the bottom edge, 2048x960 canvas, content in the bottom 30%,
  higher at the left and right corners and low in the middle, everything else transparent
  (or #00FF00). Near-black silhouettes of rubble, twisted rebar and broken concrete slabs.
  Variant [a: chunky rubble / b: rebar and steel plates].
  ```

## 二、地图底图

原版是上中下三张 2036×1440 竖向拼接，内容只在 x≈92–1912，外侧透明；顶图上沿、底图下沿是破损边缘。
**建议一次生成一张 2036×4320 的长图**（或同比例），我来切成三段，这样接缝最自然。
地图节点和路线会画在上面，所以中间区域必须平淡、低对比，细节集中在边缘。

```
[风格前缀] Tall vertical game map background, aspect ratio 2036:4320, a single worn military
tactical map sheet on a transparent (or #00FF00) background, sheet has torn frosted edges and a
small margin on the left and right. Cold grey-blue paper with very faint city block grid lines and
contour marks, frost creeping in from the edges, rust stains and a few faint amber crystal specks
near the borders. The center must stay flat and low contrast so icons drawn on top stay readable.
No text, no symbols, no legend.
```

## 三、休息处

原版是一张 3072×1440 的不透明底图，加若干透明道具。火焰动画由游戏生成，这里只画火堆底座。
座位和火堆的位置、大小要和原版接近，角色的坐姿是按原版位置摆的。

| 交付文件 | 尺寸 | 透明 | 内容 |
|---|---|---|---|
| `chernobog_act_rest_site_00.png` | 3072×1440 | 否 | 底图：两侧被黑暗遮挡的狭窄空间，中间一块地面 |
| `chernobog_act_rest_site_debris.png` | 2728×1228 | 是 | 火堆周围一圈散落的碎屑（原版是一圈树根） |
| `chernobog_act_rest_site_cutter_left.png` | 1436×1920 | 是 | 左侧竖向黑色遮幅剪影 |
| `chernobog_act_rest_site_cutter_right.png` | 1532×1920 | 是 | 右侧竖向黑色遮幅剪影 |
| `chernobog_act_rest_site_seat_left.png` | 712×456 | 是 | 左座位（原版是横倒的木头）：横放的钢梁或一排物资箱 |
| `chernobog_act_rest_site_seat_right.png` | 816×428 | 是 | 右座位，与左边成对 |
| `chernobog_act_rest_site_firepit.png` | 492×380 | 是 | 火堆底座：低矮的碎木板、废铁、油桶残片堆 |

```
[风格前缀] Opaque rest area background, 3072x1440. A sheltered guerrilla camp under a collapsed
overpass deep in the frozen city core: tall dark silhouettes framing the left and right edges,
a narrow view into the ruined street behind, an open patch of cracked concrete ground in the
middle where a campfire will be placed. Cold blue darkness, subtle warm spill light at the center.
```

座位、火堆、碎屑、遮幅四类道具用同一个前缀，各自一句：

- 座位：`a long horizontal steel I-beam lying on the ground, seen from the side` / `a row of battered military supply crates`
- 火堆：`a low pile of broken planks, scrap metal and a split oil drum, ready to burn`
- 碎屑：`scattered debris and snow clumps arranged in a loose ring around an empty center`
- 遮幅：`tall near-black silhouette of a broken concrete column with hanging cables, vertical, filling the canvas height`

## 四、之后再做

- 爱国者的地图 Boss 节点图标：等 Boss 做完，用 `sts2-map-node-icon-generation` 流程出图。
- 敌人立绘和动画走 PRTS 的 Spine 素材，不在本文档范围。

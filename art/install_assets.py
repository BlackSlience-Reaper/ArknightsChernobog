"""把 art/final/ 的定稿贴图装进模组私有资源目录 assets/（打包到 res://ArknightsChernobog/），并生成背景图层、休息处场景。

所有路径都在模组命名空间里，由 RitsuLib 的资源配置接到游戏上（src/ChernobogAssets.cs 与这里的目录一一对应）：
幕的背景/休息处/地图底图走 ModActTemplate.AssetProfile，Boss 战背景/遭遇场景/对局历史图标走 ModEncounterTemplate.AssetProfile，
能力图标走 ModPowerTemplate.AssetProfile。不再往原版命名空间（res://images、res://scenes）放任何文件。

背景图层与休息处场景继承蜂巢场景（保留原版的粒子、光效、脚本与节点布局），只覆盖贴图、隐藏按蜂巢几何画的补光；
原版场景的节点名、偏移与贴图路径由游戏引擎直接读取场景核对过（hive 背景图层与 hive_rest_site）。
"""
import shutil
from pathlib import Path

from rest_layout import LIGHTING_OFFSET, LIT_COPIES, PROP_RECTS, scaled

MOD = Path(__file__).resolve().parent.parent
FINAL = MOD / "art" / "final"
ASSETS = MOD / "assets"
RES = "res://ArknightsChernobog"
ACT = "chernobog_act"
BOSS = "reunion_patriot_boss"

# 相对 assets/ 的目录，也是 res://ArknightsChernobog/ 下的目录。改这里要同步 src/ChernobogAssets.cs。
ACT_ROOMS = f"images/rooms/{ACT}"
BOSS_ROOMS = f"images/rooms/{BOSS}"
REST_IMAGES = "images/rest_site"
MAP_IMAGES = "images/map"
ACT_BACKGROUND = f"scenes/backgrounds/{ACT}"
BOSS_BACKGROUND = f"scenes/backgrounds/{BOSS}"

# 图层场景文件后缀 → (原版场景, 贴图文件, 需要替换贴图的节点路径)。
# 中景/近景图层带恐虫模式替代贴图 PhobiaModeVisual，两张都换成同一张，保证该模式下也显示本幕背景。
# 文件名里的 _bg_NN_ / _fg_ 是原版 BackgroundAssets（RitsuLib 自定义图层目录同样沿用）按层分组随机的依据。
LAYERS = {
    "bg_00_a": ("hive_bg_00_a", "00", ["."]),
    "bg_01_a": ("hive_bg_01_a", "01_a", ["."]),
    "bg_01_b": ("hive_bg_01_b", "01_b", ["."]),
    "bg_02_a": ("hive_bg_02_a", "02_a", ["Visual", "PhobiaModeVisual"]),
    "bg_02_b": ("hive_bg_02_b", "02_b", ["Visual", "PhobiaModeVisual"]),
    "bg_02_c": ("hive_bg_02_c", "02_c", ["Visual", "PhobiaModeVisual"]),
    "bg_03_a": ("hive_bg_03_a", "03_a", ["Visual", "PhobiaModeVisual"]),
    "bg_03_b": ("hive_bg_03_b", "03_b", ["Visual", "PhobiaModeVisual"]),
    "bg_03_c": ("hive_bg_03_c", "03_c", ["Visual", "PhobiaModeVisual"]),
    "fg_a": ("hive_fg_a", "04_a", ["."]),
    "fg_b": ("hive_fg_b", "04_b", ["."]),
}

REST_TEXTURES = {
    "RestSiteBG": "00",
    "RestSiteLLog": "seat_left",
    "RestSiteRLog": "seat_right",
    "RestSiteLighting/RestSiteLLog2": "seat_left",
    "RestSiteLighting/RestSiteRLog2": "seat_right",
    "RestSiteFireLogs": "firepit",
    "RestSiteForegroundLeft": "cutter_left",
    "RestSiteForegroundRight": "cutter_right",
}
# 这些是按蜂巢树根/木头几何手绘的补光和装饰，套在新底图上会错位；虫光粒子不符合城市场景。
# roots 是蜂巢独有的地面树根环，其他原版休息处都没有这一层。
REST_HIDDEN = (
    [f"RestSiteLighting/WallLight{i}" for i in range(1, 26)]
    + ["RestSiteLighting/LLogHighlight", "RestSiteLighting/LLogHighlight2", "RestSiteLighting/FireLight", "b_blob"]
    + ["RestSiteLighting/roots"]
    + ["overlay_vfx/bugs_1", "overlay_vfx/bugs_2", "overlay_vfx/bugs_3"]
)

# Boss 战背景是一整张 3072×1440、1:1 显示的画（构图见 codex/make_boss_bg_refs.py）。
BOSS_BG_HALF = (1536, 720)


def node_header(path: str, root_name: str) -> str:
    if path == ".":
        return f'[node name="{root_name}" instance=ExtResource("1_base")]'
    parent, _, name = path.rpartition("/")
    return f'[node name="{name}" parent="{parent or "."}"]'


def prop_rect_overrides() -> dict[str, list[str]]:
    """道具按 rest_layout.PROP_SCALE 放大后的 offset 覆盖（受光副本换算到父节点本地坐标）。"""
    overrides = {}
    for node, rect in PROP_RECTS.items():
        overrides[node] = scaled(rect)
    for node, main in LIT_COPIES.items():
        left, top, right, bottom = scaled(PROP_RECTS[main])
        dx, dy = LIGHTING_OFFSET
        overrides[node] = (left - dx, top - dy, right - dx, bottom - dy)
    return {
        node: [f"offset_left = {l:.3f}", f"offset_top = {t:.3f}", f"offset_right = {r:.3f}", f"offset_bottom = {b:.3f}"]
        for node, (l, t, r, b) in overrides.items()
    }


def inherited_scene(base: str, root_name: str, textures: dict[str, str], hidden: list[str],
                    extra: dict[str, list[str]] | None = None) -> str:
    tex_ids = {res: f"tex_{i}" for i, res in enumerate(sorted(set(textures.values())))}
    lines = [f"[gd_scene load_steps={len(tex_ids) + 2} format=3]", ""]
    lines.append(f'[ext_resource type="PackedScene" path="{base}" id="1_base"]')
    lines += [f'[ext_resource type="Texture2D" path="{res}" id="{rid}"]' for res, rid in tex_ids.items()]
    lines.append("")
    overrides: dict[str, list[str]] = {".": []}
    for path, res in textures.items():
        overrides.setdefault(path, []).append(f'texture = ExtResource("{tex_ids[res]}")')
    for path in hidden:
        overrides.setdefault(path, []).append("visible = false")
    for path, props in (extra or {}).items():
        overrides.setdefault(path, []).extend(props)
    for path, props in overrides.items():
        lines.append(node_header(path, root_name))
        lines += props
        lines.append("")
    return "\n".join(lines)


def write(rel: str, text: str) -> None:
    path = ASSETS / rel
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def copy_dir(kind: str, rel: str) -> None:
    for png in sorted((FINAL / kind).glob("*.png")):
        dst = ASSETS / rel / png.name
        dst.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(png, dst)


def res(rel: str) -> str:
    return f"{RES}/{rel}"


copy_dir("rooms", ACT_ROOMS)
copy_dir("rest_site", REST_IMAGES)
copy_dir("map", MAP_IMAGES)
copy_dir("powers", "images/powers")
copy_dir("map_nodes", "images/map_nodes")
copy_dir("run_history", "images/run_history")
copy_dir("boss_rooms", BOSS_ROOMS)
copy_dir("events", "images/events")
copy_dir("cards", "images/cards")

# 幕背景根场景：继承蜂巢的背景根（层节点 Layer_00…、前景节点与粒子），图层由 RitsuLib 从自定义图层目录挑选后挂上。
write(
    f"{ACT_BACKGROUND}/{ACT}_background.tscn",
    "\n".join([
        "[gd_scene load_steps=2 format=3]",
        "",
        '[ext_resource type="PackedScene" path="res://scenes/backgrounds/hive/hive_background.tscn" id="1_base"]',
        "",
        '[node name="ChernobogActBackground" instance=ExtResource("1_base")]',
        "",
    ]),
)
for suffix, (hive_scene, image, nodes) in LAYERS.items():
    texture = res(f"{ACT_ROOMS}/{ACT}_{image}.png")
    write(
        f"{ACT_BACKGROUND}/layers/{ACT}_{suffix}.tscn",
        inherited_scene(f"res://scenes/backgrounds/hive/layers/{hive_scene}.tscn", "A", {n: texture for n in nodes}, []),
    )

# Boss 战专属背景：根场景继承本幕背景（同一套粒子与层节点），只放一层整幅画；没有 _fg_ 文件时不加前景层。
write(
    f"{BOSS_BACKGROUND}/{BOSS}_background.tscn",
    "\n".join([
        "[gd_scene load_steps=2 format=3]",
        "",
        f'[ext_resource type="PackedScene" path="{res(f"{ACT_BACKGROUND}/{ACT}_background.tscn")}" id="1_base"]',
        "",
        '[node name="ReunionPatriotBossBackground" instance=ExtResource("1_base")]',
        "",
    ]),
)
write(
    f"{BOSS_BACKGROUND}/layers/{BOSS}_bg_00_a.tscn",
    "\n".join([
        "[gd_scene load_steps=2 format=3]",
        "",
        f'[ext_resource type="Texture2D" path="{res(f"{BOSS_ROOMS}/{BOSS}_bg_00.png")}" id="tex_0"]',
        "",
        '[node name="A" type="TextureRect"]',
        "anchors_preset = 8",
        "anchor_left = 0.5",
        "anchor_top = 0.5",
        "anchor_right = 0.5",
        "anchor_bottom = 0.5",
        f"offset_left = -{BOSS_BG_HALF[0]}.0",
        f"offset_top = -{BOSS_BG_HALF[1]}.0",
        f"offset_right = {BOSS_BG_HALF[0]}.0",
        f"offset_bottom = {BOSS_BG_HALF[1]}.0",
        "grow_horizontal = 2",
        "grow_vertical = 2",
        'texture = ExtResource("tex_0")',
        "expand_mode = 1",
        "",
    ]),
)

write(
    f"scenes/rest_site/{ACT}_rest_site.tscn",
    inherited_scene(
        "res://scenes/rest_site/hive_rest_site.tscn",
        "ChernobogActRestSite",
        {node: res(f"{REST_IMAGES}/{ACT}_rest_site_{image}.png") for node, image in REST_TEXTURES.items()},
        REST_HIDDEN,
        prop_rect_overrides(),
    ),
)
print("assets installed:", ASSETS)

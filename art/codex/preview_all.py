"""拼出全部分层组合预览：战斗背景 36 种随机组合总览、休息处按原版节点坐标摆放、地图三段拼接。"""
from itertools import product
from pathlib import Path
from PIL import Image, ImageDraw

ART = Path(__file__).resolve().parent.parent
FINAL = ART / "final"
OUT = ART / "codex" / "previews"
OUT.mkdir(parents=True, exist_ok=True)


def room(name: str) -> Image.Image:
    return Image.open(FINAL / "rooms" / f"chernobog_act_{name}.png").convert("RGBA")


# 战斗背景：每组随机取一张（第 1 层 a/b × 第 2 层 a/b/c × 第 3 层 a/b/c × 前景 a/b）。
base = room("00")
cache = {n: room(n) for n in ["01_a", "01_b", "02_a", "02_b", "02_c", "03_a", "03_b", "03_c", "04_a", "04_b"]}
combos = list(product(["01_a", "01_b"], ["02_a", "02_b", "02_c"], ["03_a", "03_b", "03_c"], ["04_a", "04_b"]))
tile_w, tile_h = 512, 240
sheet = Image.new("RGB", (tile_w * 6, tile_h * 6), (0, 0, 0))
for i, combo in enumerate(combos):
    img = base.copy()
    for layer in combo:
        img.alpha_composite(cache[layer])
    thumb = img.convert("RGB").resize((tile_w, tile_h), Image.LANCZOS)
    draw = ImageDraw.Draw(thumb)
    label = " ".join(c.replace("_", "") for c in combo)
    draw.rectangle((0, 0, 8 + 7 * len(label), 16), fill=(0, 0, 0))
    draw.text((4, 3), label, fill=(255, 255, 255))
    sheet.paste(thumb, ((i % 6) * tile_w, (i // 6) * tile_h))
sheet.save(OUT / "combat_all_36_combos.png")

# 休息处：矩形取自原版 hive_rest_site 场景（TextureRect 的 offset 矩形，expand 后按矩形拉伸），
# roots 在 RestSiteLighting(-65,-65) 之下已换算成根坐标。蜂巢原图按同一套矩形摆一张做对照。
import sys
sys.path.insert(0, str(ART))
from rest_layout import PROP_RECTS, scaled  # noqa: E402

RECTS = [  # (本幕贴图名, 蜂巢贴图名, left, top, right, bottom)；道具用放大后的矩形。原版对照见 preview_vanilla_rest_sites.py
    ("00", "hive_rest_site_00", -444, -139, 2320.8, 1157),
    ("seat_left", "hive_rest_site_llog", *scaled(PROP_RECTS["RestSiteLLog"])),
    ("seat_right", "hive_rest_site_rlog", *scaled(PROP_RECTS["RestSiteRLog"])),
    ("firepit", "hive_rest_site_fire", *scaled(PROP_RECTS["RestSiteFireLogs"])),
    ("cutter_left", "hive_rest_site_cutter_left", -443, -138, 522.363, 1155.12),
    ("cutter_right", "hive_rest_site_cutter_right", 1288, -138, 2319, 1156),
]


def assemble_rest(loader) -> Image.Image:
    left0, top0 = RECTS[0][2], RECTS[0][3]
    canvas = Image.new("RGBA", (round(RECTS[0][4] - left0), round(RECTS[0][5] - top0)), (0, 0, 0, 255))
    for ours, hive, left, top, right, bottom in RECTS:
        img = loader(ours, hive).resize((round(right - left), round(bottom - top)), Image.LANCZOS)
        canvas.alpha_composite(img, (round(left - left0), round(top - top0)))
    return canvas.convert("RGB")


assemble_rest(lambda o, h: Image.open(FINAL / "rest_site" / f"chernobog_act_rest_site_{o}.png").convert("RGBA")).save(
    OUT / "rest_site_assembled.png")

# 地图：三段竖向拼接，底色用幕模型当前的 MapBgColor。
parts = [Image.open(FINAL / "map" / f"map_{p}_chernobog_act.png").convert("RGBA") for p in ("top", "middle", "bottom")]
tall = Image.new("RGBA", (parts[0].width, sum(p.height for p in parts)), (0x8E, 0x87, 0x7A, 255))
y = 0
for p in parts:
    tall.alpha_composite(p, (0, y))
    y += p.height
tall.convert("RGB").resize((tall.width // 3, tall.height // 3), Image.LANCZOS).save(OUT / "map_assembled.png")
print("ok")

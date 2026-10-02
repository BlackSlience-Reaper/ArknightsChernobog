"""休息处重出所需参考图：布局示意（座位/火堆在底图上的真实位置）与原版道具对照表。"""
from pathlib import Path
from PIL import Image, ImageDraw

ART = Path(__file__).resolve().parent.parent
REF = ART / "reference"
OUT = ART / "codex" / "previews"
BG = (-444, -139, 2320.8, 1157)
# 本幕休息处继承蜂巢场景，道具矩形取蜂巢节点再按 PROP_SCALE 以中心放大（与 install_assets.py 一致）。
import sys
sys.path.insert(0, str(ART))
from rest_layout import PROP_RECTS, scaled  # noqa: E402

BOXES = {
    "LEFT SEAT": scaled(PROP_RECTS["RestSiteLLog"]),
    "RIGHT SEAT": scaled(PROP_RECTS["RestSiteRLog"]),
    "FIRE PIT": scaled(PROP_RECTS["RestSiteFireLogs"]),
}
CUTTERS = [(-443, -138, 522.363, 1155.12), (1288, -138, 2319, 1156)]


def to_bg(rect):
    return (rect[0] - BG[0], rect[1] - BG[1], rect[2] - BG[0], rect[3] - BG[1])


w, h = round(BG[2] - BG[0]), round(BG[3] - BG[1])
guide = Image.new("RGB", (w, h), (28, 32, 40))
draw = ImageDraw.Draw(guide)
# 空地范围：四个原版休息处都在左右遮幅之间、从上方约 20% 到底部铺开一块椭圆地面。
draw.ellipse((w * 0.2, h * 0.18, w * 0.8, h * 1.25), fill=(70, 78, 88))
for rect in CUTTERS:
    l, t, r, b = to_bg(rect)
    draw.rectangle((l, t, r, b), outline=(120, 120, 120), width=6)
    draw.text((l + 20, t + 40), "DARK FRAME (covered)", fill=(160, 160, 160))
for label, rect in BOXES.items():
    l, t, r, b = to_bg(rect)
    draw.rectangle((l, t, r, b), outline=(255, 70, 70), width=8)
    draw.text((l + 10, t + 10), label, fill=(255, 120, 120))
draw.text((w * 0.42, h * 0.25), "OPEN GROUND (high-angle view)", fill=(220, 220, 220))
guide.save(OUT / "rest_layout_guide.png")

props = [
    "hive/hive_rest_site_llog", "hive/hive_rest_site_rlog", "hive/hive_rest_site_fire",
    "rest_sites/overgrowth_rest_site_l_log", "rest_sites/overgrowth_rest_site_r_log", "rest_sites/overgrowth_rest_site_fire",
    "rest_sites/glory_rest_site_l_log", "rest_sites/glory_rest_site_r_log", "rest_sites/glory_rest_site_fire",
    "rest_sites/underdocks_rest_site_log_l", "rest_sites/underdocks_rest_site_log_r", "rest_sites/underdocks_rest_site_fire",
]
cell = (460, 300)
sheet = Image.new("RGB", (cell[0] * 3, cell[1] * 4), (255, 0, 255))
for i, name in enumerate(props):
    img = Image.open(REF / f"{name}.png").convert("RGBA")
    img.thumbnail((cell[0] - 20, cell[1] - 20))
    tile = Image.new("RGBA", cell, (255, 0, 255, 255))
    tile.alpha_composite(img, ((cell[0] - img.width) // 2, (cell[1] - img.height) // 2))
    sheet.paste(tile.convert("RGB"), ((i % 3) * cell[0], (i // 3) * cell[1]))
sheet.save(OUT / "vanilla_rest_props_sheet.png")
print("ok")

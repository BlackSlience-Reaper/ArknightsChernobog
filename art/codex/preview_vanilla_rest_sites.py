"""按原版场景的 TextureRect 矩形拼出四个原版休息处，用来归纳视角与构图规律。

矩形都在场景根坐标系下（底图固定在 -444,-139 到 2320.8,1157）。
"""
from pathlib import Path
from PIL import Image, ImageDraw

ART = Path(__file__).resolve().parent.parent
REF = ART / "reference"
OUT = ART / "codex" / "previews"
BG_RECT = (-444, -139, 2320.8, 1157)

SITES = {
    "hive": [
        ("hive/hive_rest_site_00", BG_RECT),
        ("hive/hive_rest_site_roots", (-49, 330, 1792, 1157)),
        ("hive/hive_rest_site_llog", (508.68, 546.6, 827.021, 751.344)),
        ("hive/hive_rest_site_rlog", (980.68, 540.6, 1345.72, 732.772)),
        ("hive/hive_rest_site_fire", (802.68, 646.6, 1022.24, 815.873)),
        ("hive/hive_rest_site_cutter_left", (-443, -138, 522.363, 1155.12)),
        ("hive/hive_rest_site_cutter_right", (1288, -138, 2319, 1156)),
    ],
    "overgrowth": [
        ("rest_sites/overgrowth_rest_site_bg", BG_RECT),
        ("rest_sites/overgrowth_rest_site_l_log", (519, 550, 829.5, 751.15)),
        ("rest_sites/overgrowth_rest_site_r_log", (1045, 554, 1347.85, 746.6)),
        ("rest_sites/overgrowth_rest_site_fire", (844, 670, 1032.55, 794.65)),
        ("rest_sites/overgrowth_rest_site_cutter_2", (1454, -135, 1737.5, 1160.55)),
        ("rest_sites/overgrowth_rest_site_cutter_1", (1454 - 1372, -135 - 7, 1454 - 1007.5, -135 + 1289)),
    ],
    "underdocks": [
        ("rest_sites/underdocks_rest_site_water", (-140, -56, 1924, 778)),
        ("rest_sites/underdocks_rest_site_bg", BG_RECT),
        ("rest_sites/underdocks_rest_site_log_l", (444, 545, 444 + 402 * 1.005, 545 + 253 * 0.92)),
        ("rest_sites/underdocks_rest_site_log_r", (978, 558, 978 + 424 * 0.85, 558 + 232 * 0.885)),
        ("rest_sites/underdocks_rest_site_fire", (808, 671, 1044, 827)),
        ("rest_sites/underdocks_rest_site_cutter_r", (1508, -138, 1887, 1159)),
        ("rest_sites/underdocks_rest_site_cutter_l", (-84, -138, 384, 1158)),
    ],
    "glory": [
        ("rest_sites/glory_rest_site_00", BG_RECT),
        ("rest_sites/glory_rest_site_l_log", (518, 547, 828.5, 748.15)),
        ("rest_sites/glory_rest_site_r_log", (976, 545, 1363, 768)),
        ("rest_sites/glory_rest_site_fire", (864, 667, 1015, 791)),
        ("rest_sites/glory_rest_site_cutter_2", (1069, -140, 1794, 1156)),
        ("rest_sites/glory_rest_site_cutter_1", (50, -139, 694, 1157)),
    ],
}


def assemble(parts, guides: bool) -> Image.Image:
    left0, top0 = BG_RECT[0], BG_RECT[1]
    canvas = Image.new("RGBA", (round(BG_RECT[2] - left0), round(BG_RECT[3] - top0)), (0, 0, 0, 255))
    for name, (left, top, right, bottom) in parts:
        img = Image.open(REF / f"{name}.png").convert("RGBA").resize((round(right - left), round(bottom - top)), Image.LANCZOS)
        canvas.alpha_composite(img, (round(left - left0), round(top - top0)))
    if guides:
        draw = ImageDraw.Draw(canvas)
        for name, (left, top, right, bottom) in parts[1:]:
            if "cutter" in name or "roots" in name or "water" in name:
                continue
            draw.rectangle((left - left0, top - top0, right - left0, bottom - top0), outline=(255, 60, 60, 255), width=4)
    return canvas.convert("RGB")


tiles = []
for site, parts in SITES.items():
    img = assemble(parts, guides=False)
    img.save(OUT / f"vanilla_rest_{site}.png")
    thumb = img.resize((1382, 648), Image.LANCZOS)
    ImageDraw.Draw(thumb).text((12, 10), site, fill=(255, 255, 255))
    tiles.append(thumb)
sheet = Image.new("RGB", (1382 * 2, 648 * 2))
for i, t in enumerate(tiles):
    sheet.paste(t, ((i % 2) * 1382, (i // 2) * 648))
sheet.save(OUT / "vanilla_rest_sites_sheet.png")
print("ok")

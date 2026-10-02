"""按原版 normal_map_point 的着色规则模拟地图节点在纸面上的样子，用来调纸面明度与 MapBgColor。

原版规则（normal_map_point.tscn + normal_map_point.gdshader + NNormalMapPoint._Ready）：
- 图标像素与 initial_color(0.706, 0.616, 0.537) 的 RGB 距离 < 0.5 时换成 map_color = MapBgColor.Lerp(灰, 0.5)；
- 不可达状态图标 self_modulate 为白色 50% 透明；
- 描边 Outline 的 modulate = MapBgColor，垫在图标下面。
"""
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw

ART = Path(__file__).resolve().parent.parent
ICONS = ART / "reference" / "map_icons"
OUT = ART / "codex" / "previews"
INITIAL = np.array([0.705882, 0.615686, 0.537255])
NAMES = ["map_monster", "map_elite", "map_unknown", "map_rest", "map_shop"]


def hex_rgb(h: str) -> np.ndarray:
    return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)]) / 255.0


def node(name: str, bg: np.ndarray, alpha: float) -> tuple[Image.Image, Image.Image]:
    icon = np.asarray(Image.open(ICONS / f"{name}.tres").convert("RGBA")).astype(float) / 255
    face = np.linalg.norm(icon[..., :3] - INITIAL, axis=-1) < 0.5
    map_color = bg * 0.5 + 0.5 * 0.5
    icon[face, :3] = map_color
    icon[..., 3] *= alpha
    outline = np.asarray(Image.open(ICONS / f"{name}_outline.tres").convert("RGBA")).astype(float) / 255
    outline[..., :3] = outline[..., :3] * bg
    to_img = lambda a: Image.fromarray((a * 255).clip(0, 255).astype(np.uint8), "RGBA")
    return to_img(icon), to_img(outline)


def panel(title: str, paper: Image.Image, bg_hex: str, alpha: float) -> Image.Image:
    canvas = paper.convert("RGBA").resize((560, 150))
    bg = hex_rgb(bg_hex)
    x = 20
    for name in NAMES:
        icon, outline = node(name, bg, alpha)
        oy = (150 - outline.height) // 2
        canvas.alpha_composite(outline, (x, oy))
        canvas.alpha_composite(icon, (x + (outline.width - icon.width) // 2, oy + (outline.height - icon.height) // 2))
        x += outline.width + 12
    ImageDraw.Draw(canvas).text((8, 4), title, fill=(0, 0, 0))
    return canvas


def paper_crop(path: Path) -> Image.Image:
    img = Image.open(path).convert("RGBA")
    return img.crop((700, 500, 1260, 650))


hive = paper_crop(ART / "reference" / "hive" / "map_middle_hive.png")
ours_old = Image.new("RGBA", (560, 150), (0xBA, 0xC4, 0xCE, 255))
ours_new = paper_crop(ART / "final" / "map" / "map_middle_chernobog_act.png")
rows = [
    panel("vanilla hive: paper #9C9663, MapBgColor 9B9562", hive, "9B9562", 0.5),
    panel("ours before: paper #BAC4CE, MapBgColor 8C98A3", ours_old, "8C98A3", 0.5),
    panel("ours after: paper #8D969D, MapBgColor 8D969D", ours_new, "8D969D", 0.5),
    panel("ours after, reachable (alpha 1)", ours_new, "8D969D", 1.0),
]
sheet = Image.new("RGBA", (560, 150 * len(rows)))
for i, row in enumerate(rows):
    sheet.paste(row, (0, 150 * i))
sheet.save(OUT / "map_nodes_compare.png")
print("ok")

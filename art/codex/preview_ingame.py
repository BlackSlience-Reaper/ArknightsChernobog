"""模拟 16:9（1920×1080）游戏画面里战斗背景的实际可见范围。

原版战斗房间把背景容器放在屏幕中心（x 偏右 23 像素），蜂巢图层的 TextureRect 以中心为锚、
offset ±1440.77 × ±675.36，即 2048×960 贴图被拉伸到约 2881.5×1350.7 显示，只有中间一块落在屏幕里。
用法：python3 preview_ingame.py <图层目录> <输出前缀> [组合...]，组合形如 01_a,02_b,03_c,04_a。
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw

SCREEN = (1920, 1080)
RECT = (2881.54, 1350.72)
CONTAINER_X_OFFSET = 23


def render(layer_dir: Path, combo: list[str]) -> Image.Image:
    base = Image.open(layer_dir / "chernobog_act_00.png").convert("RGBA")
    for layer in combo:
        base.alpha_composite(Image.open(layer_dir / f"chernobog_act_{layer}.png").convert("RGBA"))
    stretched = base.resize((round(RECT[0]), round(RECT[1])), Image.LANCZOS)
    left = round(RECT[0] / 2 - SCREEN[0] / 2 - CONTAINER_X_OFFSET)
    top = round(RECT[1] / 2 - SCREEN[1] / 2)
    return stretched.crop((left, top, left + SCREEN[0], top + SCREEN[1])).convert("RGB")


if __name__ == "__main__":
    layer_dir, prefix = Path(sys.argv[1]), sys.argv[2]
    combos = [c.split(",") for c in sys.argv[3:]] or [["01_a", "02_b", "03_c", "04_a"]]
    for combo in combos:
        img = render(layer_dir, combo)
        ImageDraw.Draw(img).text((12, 12), " ".join(combo), fill=(255, 255, 255))
        img.save(f"{prefix}_{'_'.join(c.replace('_', '') for c in combo)}.png")
    print("ok")

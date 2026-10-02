"""爱国者 Boss 战背景的构图示意图（给 Codex 的参考，不入库）。

Boss 战背景画成一整张 3072×1440、1:1 显示（场景里 TextureRect ±1536×±720 居中）。
镜头按 ReunionPatriotBoss.GetCameraScaling 0.85、下移 30：16:9 画面在图上看到的是 x 407–2665、y 49–1320；
角色脚底在容器 y≈740，折到图上 y≈920（约 64%）。玩家在左、两名护卫与爱国者在右。
"""
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ART = Path(__file__).resolve().parent.parent
OUT = ART / "reference" / "patriot"
W, H = 3072, 1440
SCALE, OFFSET = 0.85, 30
SCREEN = (1920, 1080)


def to_image(sx: float, sy: float) -> tuple[float, float]:
    """16:9 屏幕坐标 → 背景图坐标。"""
    cx = (sx - 960) / SCALE + 960
    cy = (sy - OFFSET - 540) / SCALE + 540
    return cx - 960 + W / 2, cy - 540 + H / 2


font = ImageFont.truetype("/System/Library/Fonts/Supplemental/Arial Bold.ttf", 44)
img = Image.new("RGB", (W, H), (70, 74, 82))
d = ImageDraw.Draw(img)
feet = 740 - 540 + H / 2
d.rectangle((0, feet + 110, W, H), fill=(52, 55, 60))
d.rectangle((0, feet - 150, W, feet + 110), fill=(95, 98, 104))
d.line((0, feet, W, feet), fill=(255, 200, 60), width=6)
d.text((40, feet - 60), "FLOOR LINE: characters' feet stand here (~64% from top)", font=font, fill=(255, 200, 60))
d.text((40, feet - 140), "flat walkable ground band, keep it clear", font=font, fill=(230, 230, 230))
# 角色占位（只示意位置和大小，不要画出来）
for x0, x1, h, label in ((330, 560, 300, "player"), (1130, 1300, 300, "guard"), (1330, 1500, 300, "guard"), (1540, 1800, 470, "boss")):
    a, _ = to_image(x0, 0)
    b, _ = to_image(x1, 0)
    top = feet - h / SCALE
    d.rectangle((a, top, b, feet), outline=(120, 200, 255), width=6)
    d.text((a + 10, top + 10), label, font=font, fill=(120, 200, 255))
vx0, vy0 = to_image(0, 0)
vx1, vy1 = to_image(*SCREEN)
d.rectangle((vx0, vy0, vx1, vy1), outline=(255, 90, 90), width=8)
d.text((vx0 + 20, vy0 + 20), "16:9 screen view (everything important inside)", font=font, fill=(255, 90, 90))
d.text((40, H - 70), "outside red box: only seen on ultrawide screens - continue the scenery, no key objects", font=font, fill=(255, 90, 90))
OUT.mkdir(parents=True, exist_ok=True)
img.resize((W // 2, H // 2), Image.LANCZOS).save(OUT / "boss_bg_layout.png")
print("visible", round(vx0), round(vy0), round(vx1), round(vy1), "feet", feet)

"""Preview generated combat layers: per-layer sheet on magenta and stacked composites of two variant combos."""
from pathlib import Path
from PIL import Image, ImageDraw

ART = Path(__file__).resolve().parent.parent
GEN = ART / "generated"
OUT = ART / "codex" / "previews"
OUT.mkdir(parents=True, exist_ok=True)
SIZE = (2048, 960)


def load(name: str) -> Image.Image:
    return Image.open(GEN / f"chernobog_act_{name}.png").convert("RGBA").resize(SIZE, Image.LANCZOS)


layers = ["00", "01_a", "01_b", "02_a", "02_b", "02_c", "03_a", "03_b", "03_c", "04_a", "04_b"]
sheet = Image.new("RGBA", (512 * 3, 240 * 4), (0, 0, 0, 255))
for i, name in enumerate(layers):
    board = Image.new("RGBA", SIZE, (255, 0, 255, 255))
    board.alpha_composite(load(name))
    thumb = board.resize((512, 240))
    ImageDraw.Draw(thumb).text((8, 8), name, fill=(255, 255, 255, 255))
    sheet.paste(thumb, ((i % 3) * 512, (i // 3) * 240))
sheet.save(OUT / "layers_sheet.png")

for label, combo in {"a": ["00", "01_a", "02_a", "03_a", "04_a"], "b": ["00", "01_b", "02_c", "03_c", "04_b"]}.items():
    base = load(combo[0])
    for name in combo[1:]:
        base.alpha_composite(load(name))
    base.save(OUT / f"composite_{label}.png")
print("ok")

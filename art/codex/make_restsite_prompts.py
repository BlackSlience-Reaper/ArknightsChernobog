"""休息处左右遮幅的 Codex 提示词。底图、座位、火堆见 make_restsite_v2_prompts.py（高机位俯视构图）。

遮幅是贴在画面两侧的通用暗色剪影，不受视角影响，第一版出图沿用至今。
参考图依次为：本幕战斗背景定稿、原版蜂巢休息处底图、原版蜂巢休息处道具表。
"""
from pathlib import Path

OUT = Path(__file__).resolve().parent / "assets"
TRANSPARENT = (
    "- 背景必须透明：能直接输出透明背景 PNG 就输出透明；否则整个背景填充纯绿色 #00FF00，物体边缘不要带绿边，我会抠图。\n"
)
REF = (
    "- 参考图：第一张是本幕已定稿的战斗背景（色调、画风以它为准）；第二张是原版休息处底图；"
    "第三张是原版休息处道具（从左到右：地面碎屑环、左遮幅、右遮幅、左座位、右座位、火堆、斑点），品红色表示透明。\n"
)

PIECES = {
    "chernobog_act_rest_site_cutter_left": (
        "左侧遮幅", "竖向比例约 1436:1920（约 3:4）。",
        "对应原版左遮幅：占满画布高度的近乎纯黑剪影，右边缘是不规则的轮廓（断裂的混凝土柱、垂下的电缆），"
        "左侧大部分是实心黑色，右侧透明。",
    ),
    "chernobog_act_rest_site_cutter_right": (
        "右侧遮幅", "竖向比例约 1532:1920（约 4:5）。",
        "左遮幅的镜像思路：右侧大部分实心黑色，左边缘是不规则轮廓（断墙、钢梁、电缆），左侧透明。不要与左遮幅完全对称。",
    ),
}

for name, (title, canvas, body) in PIECES.items():
    text = f"## 要生成的图：休息处{title}\n- {canvas}\n" + REF + TRANSPARENT
    text += f"- 画面：{body}\n\n保存为：`generated/{name}.png`\n"
    (OUT / f"{name}.md").write_text(text, encoding="utf-8")
print("wrote", len(PIECES))

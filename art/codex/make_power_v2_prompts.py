"""第二版能力图标的提示词与参考图。

第一版的问题（与原版 32 个怪物能力图标在 64 像素下对比得出）：主体偏暗（深色主体压在深色状态栏上）、细零件和写实纹理多、
主体没撑满画布。原版的共同点：一个简单的大物件撑满格子（包围盒两向都在 0.9 以上）、亮的主色（平均明度 120–180）、
接近黑色的像素只占约一成、粗深色描边、2–3 个色阶。

参考图（reference/powers_v2/，原版导出物不入库）：
- good_sheet.png：原版图标 64 像素放大 4 倍排成的总表，只参考画风；
- bad_<name>.png：本图标第一版缩到 64 再放大，作为反例；
- 每张另附 1–2 个语义相近的原版图标。
"""
from pathlib import Path

from PIL import Image

ART = Path(__file__).resolve().parent.parent
VANILLA = ART / "reference" / "powers_vanilla64"  # 原版 power_atlas 导出的 64 像素图标
REF = ART / "reference" / "powers_v2"
OLD = ART / "final" / "powers"

# 参考图垫成状态栏的深灰色：原版描边是半透明深色，垫在绿底上会显成深绿，Codex 会照着画出绿描边。
PANEL = (38, 40, 46, 255)

GOOD = ["strength", "artifact", "plating", "rampart", "regen", "thorns", "vulnerable", "territorial",
        "feral", "flutter", "soar", "unmovable", "hard_to_kill", "curl_up", "frail", "doom"]

COMMON = """- 本任务是一枚能力（Buff）图标；上面风格段落是给场景背景用的，其中“不要人物/徽记/旗帜”的限制对这张图不适用，按下面的图标规格画。
- 正方形 1:1（例如 1024×1024），背景必须透明：能直接输出透明背景 PNG 就输出透明；否则整个背景填充纯绿色 #00FF00，物体边缘不要带绿边。
- 这张图在游戏里只显示成约 40–64 像素，放在深色的状态栏上。
- 参考图（按附件顺序：第 1 张总表、第 2 张上一版反例、其余是原版单个图标）：
  - `good_sheet.png`：《杀戮尖塔2》原版能力图标总表（垫的深灰底是游戏状态栏的颜色，不是图标的一部分）。**画风必须照它**：一个简单、粗壮的物件**撑满整个画布**（上下左右都几乎碰边），
    **明亮饱和的主色**，粗的深灰色外描边（约画布宽度的 4%），每个物体只有 2–3 个平涂色阶，一小块高光，**没有纹理、裂纹、锈迹、细线、小零件**。
  - `bad_*.png`：这个图标的上一版（错误示范），问题见下文，这次必须改掉。
  - 其余是语义相近的原版图标，照它们的体量和画法。
- 硬性要求：主体填满画布约 90% 以上的宽或高；主体主色要亮（深色只用于描边和少量暗部，不得超过画面的一成）；
  最细的形体也要有画布宽度的约 6%；不要文字、字母、数字。
"""

JOBS = {
    "reunion_pack_fury_power": (
        "狼群", ["feral", "territorial"],
        "上一版是黑色的犬头压在深色背景上，缩小后几乎看不见，只剩红色爪痕。",
        "一个浅灰偏白的狼头侧面剪影（朝右、张嘴咆哮、露出白色獠牙、一只红眼），狼头占画面大部分；"
        "狼头左下方叠三道粗大的鲜红色斜爪痕，爪痕要宽。只画一个狼头，不要背后的其他犬头。主色浅灰白 + 鲜红。",
    ),
    "reunion_patriot_wrath_power": (
        "毁灭姿态", ["disintegration", "vulnerable"],
        "上一版是一根细黑长矛，缩小后只剩一条斜线，覆盖率很低，而且和“让对手瓦解”的效果无关。",
        "一块粗大的黑色结晶（明日方舟的源石：棱角分明的多面晶体簇），正从中间炸裂开，裂缝和碎块边缘透出明亮的橙红色光，"
        "几块大碎片向外飞散。晶体本身用深灰紫色但要有明显的亮面，橙红裂光是画面的亮点，整体要亮、撑满画布。",
    ),
    "reunion_permanent_regen_power": (
        "永续再生", ["regen"],
        "上一版是暗酒红色，压在深色状态栏上发闷，和原版鲜绿的“再生”放一起显得脏。",
        "中央一个粗大的十字（形状、粗细、描边都照原版“再生”），但主色换成明亮的玫粉色；"
        "十字外面一圈首尾相接的粗循环箭头（两段，浅粉白色），圆环直径接近整个画布。整体明亮。",
    ),
    "reunion_patriot_armor_power": (
        "残甲", ["plating", "hard_to_kill"],
        "上一版是写实的暗色胸甲，满是锈迹和细裂纹，偏暗偏写实。",
        "一块浅钢灰色的胸甲正面（照原版“覆甲”那顶头盔的颜色与画法：浅钢灰 + 白色高光），胸甲撑满画布；"
        "甲面斜着一道粗大的裂口，裂口处缺掉一块。不要锈迹、不要铆钉以外的细节，铆钉最多两三个大圆点。",
    ),
    "reunion_hover_power": (
        "悬浮", ["soar", "flutter"],
        "上一版是扁长的深色无人机，纵向只占画面六成，旋翼是细线，缩小后看不清。",
        "一架小型无人机的正面近景：圆胖的机身（中灰色、有明亮的紫色发光核心）占画面中部，左右两侧各一个粗短的旋翼罩；"
        "机身下方两道粗的亮紫色弧形气流。整体紧凑、上下左右都撑满画布，不要细长的旋翼杆。",
    ),
    "reunion_patriot_oath_power": (
        "旧部之誓", ["territorial", "leadership"],
        "上一版是细旗杆 + 小旗 + 小箭头，覆盖率很低，缩小后糊成一团。",
        "一面大战旗占满画面（照原版“领地意识”那面红旗的体量和画法）：短而粗的旗杆斜立，旗面很大、向右飘展，"
        "旗面是鲜艳的深红色、上面一个粗大的白色 X（笔刷涂出来的粗 X，两笔略不对称），旗尾撕成两三个大尖角。"
        "旗面绝对不要用蓝色，整面旗不要像任何真实国家或地区的旗帜。不要长戟、不要箭头。",
    ),
    "reunion_host_remnant_power": (
        "残存", ["reattach", "hard_to_kill"],
        "上一版的手指细、裂纹和丝线都是细线，缩小后显碎。",
        "一只粗壮的苍白灰白色的手从下方伸出、五指张开向上抓，手指要粗短（像卡通手），手掌撑满画面；"
        "手腕处缠着两圈粗的暗红色绷带（不是细丝线），手背上一块琥珀橙色的结晶斑。不要裂纹细线、不要骷髅。",
    ),
}


def upscale(img: Image.Image, size: int = 256) -> Image.Image:
    return img.resize((size, size), Image.LANCZOS)


def on_panel(img: Image.Image) -> Image.Image:
    bg = Image.new("RGBA", img.size, PANEL)
    bg.alpha_composite(img)
    return bg.convert("RGB")


def vanilla(name: str) -> Image.Image:
    im = Image.open(VANILLA / f"{name}_power.png").convert("RGBA")
    c = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    c.alpha_composite(im, ((64 - im.width) // 2, (64 - im.height) // 2))
    return upscale(c)


REF.mkdir(parents=True, exist_ok=True)
sheet = Image.new("RGBA", (4 * 256, 4 * 256), PANEL)
for i, name in enumerate(GOOD):
    sheet.alpha_composite(vanilla(name), ((i % 4) * 256, (i // 4) * 256))
sheet.convert("RGB").save(REF / "good_sheet.png")
for name in {n for _, refs, _, _ in JOBS.values() for n in refs} | {"leadership"}:
    on_panel(vanilla(name)).save(REF / f"vanilla_{name}.png")

for job, (title, refs, bad, scene) in JOBS.items():
    old = Image.open(OLD / f"{job}.png").convert("RGBA").resize((64, 64), Image.LANCZOS)
    on_panel(upscale(old)).save(REF / f"bad_{job}.png")
    text = (f"## 要生成的图：能力（Buff）图标“{title}”（第二版）\n{COMMON}"
            f"- 上一版的问题（`bad_{job}.png`）：{bad}\n- 画面：{scene}构图居中。\n\n"
            f"保存为：`generated/{job}.png`\n")
    (ART / "codex" / "assets" / f"{job}.md").write_text(text)
    args = " ".join(str(REF / p) for p in ["good_sheet.png", f"bad_{job}.png"] + [f"vanilla_{r}.png" for r in refs])
    print(f"{job}\t{args}")

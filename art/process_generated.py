"""把 art/generated/ 里的生成图规整成游戏要求的精确尺寸，输出到 art/final/。

尺寸与内容边界取自原版蜂巢同名资源（见 art/reference/），因为背景图层场景、地图拼接和休息处
道具的摆放都按这些尺寸写死在原版场景里。
"""
from pathlib import Path
import numpy as np
from PIL import Image, ImageFilter, ImageStat

ART = Path(__file__).resolve().parent
GEN = ART / "generated"
FINAL = ART / "final"

COMBAT = ["00", "01_a", "01_b", "02_a", "02_b", "02_c", "03_a", "03_b", "03_c", "04_a", "04_b"]

# 道具：(原版画布尺寸, 在画布内的对齐方式)。原版道具几乎铺满画布，这里裁掉生成图的透明留白后等比塞进画布。
REST_PROPS = {
    "cutter_left": ((1436, 1920), "left"),
    "cutter_right": ((1532, 1920), "right"),
    "seat_left": ((712, 456), "bottom"),
    "seat_right": ((816, 428), "bottom"),
    "firepit": ((492, 380), "bottom"),
}

# 地图纸面的目标平均明度。原版节点图标用固定的半透明色，着色器把图标“面部”换成 MapBgColor 与灰色的中间色，
# 描边光晕直接用 MapBgColor；原版纸面明度约 146 且 MapBgColor 就等于纸面平均色（蜂巢纸面 #9C9663 对 #9B9562）。
# 纸面太亮会让图标发白发虚，所以按比例压到这个明度，再把纸面平均色填回 ChernobogAct.MapBgColor。
MAP_PAPER_TARGET_L = 148.0

# 原版地图三段各 2036x1440，纸面内容 x 92..1912，顶段从 y=52 开始，底段在 y=1324 结束。
MAP_SIZE = (2036, 1440)
MAP_CONTENT = (92, 52, 1912, 1440 * 2 + 1324)


# 休息处的目标平均明度（0-255，按不透明像素计）。原版四幕底图 6.6~15.6、座位/火堆 12~22：
# 休息处靠游戏里的火光与光粒子照亮，贴图本身必须很暗，否则加光后发灰。
REST_BG_TARGET_L = 12.0
REST_PROP_TARGET_L = 20.0
# 原版遮幅 1.0~5.6：它是压在最前面的暗框，比底图还暗才能把视线收向中央。
REST_CUTTER_TARGET_L = 5.0


def darken_to(img: Image.Image, target: float) -> Image.Image:
    """用伽马曲线把平均明度压到 target（只压不提），保留黑位与高光层次。"""
    alpha = img.getchannel("A").point(lambda v: 255 if v > 128 else 0) if img.mode == "RGBA" else None
    current = ImageStat.Stat(img.convert("L"), alpha).mean[0]
    if current <= target:
        return img
    lo, hi = 1.0, 6.0
    for _ in range(30):
        gamma = (lo + hi) / 2
        lut = [round(255 * (v / 255) ** gamma) for v in range(256)]
        probe = img.convert("RGB").point(lut * 3).convert("L")
        if ImageStat.Stat(probe, alpha).mean[0] > target:
            lo = gamma
        else:
            hi = gamma
    lut = [round(255 * (v / 255) ** hi) for v in range(256)]
    rgb = img.convert("RGB").point(lut * 3)
    if img.mode == "RGBA":
        rgb.putalpha(img.getchannel("A"))
    return rgb


# 战斗背景图层的显示方式沿用蜂巢场景：2048×960 贴图被拉伸到约 2881.5×1350.7、居中显示（兼顾超宽屏），
# 16:9 画面只看得到贴图中间 x≈325–1690、y≈96–864 这一块。生成图是按“整张可见”构图的，
# 所以把整张构图缩到 COMBAT_VIEW_SCALE 放进中央，纵向正好填满可见区、横向每侧只裁掉约 8%；
# 缩小后空出的四周用镜像延展补齐，只在超宽屏或镜头拉远时露出。
COMBAT_VIEW_SCALE = 0.8


def fit_to_view(img: Image.Image, scale: float = COMBAT_VIEW_SCALE) -> Image.Image:
    width, height = img.size
    inner = img.resize((round(width * scale), round(height * scale)), Image.LANCZOS)
    pad_x = width - inner.width
    pad_y = height - inner.height
    left, top = pad_x // 2, pad_y // 2
    pixels = np.asarray(inner)
    padded = np.pad(pixels, ((top, pad_y - top), (left, pad_x - left), (0, 0)), mode="reflect")
    return Image.fromarray(padded, mode=img.mode)


def save(img: Image.Image, rel: str) -> None:
    path = FINAL / rel
    path.parent.mkdir(parents=True, exist_ok=True)
    img.save(path, optimize=True)
    print(f"{rel:60s} {img.size[0]}x{img.size[1]} {img.mode}")


def crop_to_alpha(img: Image.Image) -> Image.Image:
    box = img.getchannel("A").point(lambda v: 255 if v > 8 else 0).getbbox()
    return img.crop(box) if box else img


def fit(img: Image.Image, size: tuple[int, int], align: str) -> Image.Image:
    img = crop_to_alpha(img)
    scale = min(size[0] / img.width, size[1] / img.height)
    img = img.resize((max(1, round(img.width * scale)), max(1, round(img.height * scale))), Image.LANCZOS)
    canvas = Image.new("RGBA", size, (0, 0, 0, 0))
    x = {"left": 0, "right": size[0] - img.width}.get(align, (size[0] - img.width) // 2)
    y = size[1] - img.height if align in ("bottom", "left", "right") else (size[1] - img.height) // 2
    canvas.alpha_composite(img, (x, y))
    return canvas


for name in COMBAT:
    img = Image.open(GEN / f"chernobog_act_{name}.png")
    img = img.convert("RGB" if name == "00" else "RGBA").resize((2048, 960), Image.LANCZOS)
    save(fit_to_view(img), f"rooms/chernobog_act_{name}.png")

# Boss 战专属背景：一整张画，按 1:1 显示在 3072×1440 的矩形里（原版幕背景是 2048×960 拉到 2881×1351）。
# 爱国者战镜头缩到 0.85、下移 30，16:9 画面看到图上 x 407–2665、y 49–1320，超宽屏再往两侧延伸，所以比幕背景大一圈。
# 构图示意见 codex/make_boss_bg_refs.py。
BOSS_BG_SIZE = (3072, 1440)
boss_bg = Image.open(GEN / "reunion_patriot_boss_bg.png").convert("RGB").resize(BOSS_BG_SIZE, Image.LANCZOS)
save(boss_bg, "boss_rooms/reunion_patriot_boss_bg_00.png")

# 本幕事件立绘：原版事件图 3440×1616（主体在左、右半边压暗给文字面板）；生成图约 1829×860，等比放大。
# 诅咒“矿石病”卡图与原版图集卡图同为 250×190。都装进模组私有目录（assets/images/），由事件与卡牌类按路径引用。
EVENT_SIZE = (3440, 1616)
for name in ("supply_cache", "guerrilla_campfire", "originium_vein", "core_valve", "rhodes_airdrop", "black_snake"):
    event = Image.open(GEN / f"reunion_event_{name}.png").convert("RGB").resize(EVENT_SIZE, Image.LANCZOS)
    save(event, f"events/reunion_event_{name}.png")
save(Image.open(GEN / "reunion_oripathy.png").convert("RGB").resize((250, 190), Image.LANCZOS), "cards/oripathy.png")

rest_bg = Image.open(GEN / "chernobog_act_rest_site_00.png").convert("RGB").resize((3072, 1440), Image.LANCZOS)
save(darken_to(rest_bg, REST_BG_TARGET_L), "rest_site/chernobog_act_rest_site_00.png")
for name, (size, align) in REST_PROPS.items():
    prop = fit(Image.open(GEN / f"chernobog_act_rest_site_{name}.png").convert("RGBA"), size, align)
    prop = darken_to(prop, REST_CUTTER_TARGET_L if name.startswith("cutter") else REST_PROP_TARGET_L)
    save(prop, f"rest_site/chernobog_act_rest_site_{name}.png")

paper = crop_to_alpha(Image.open(GEN / "chernobog_act_map.png").convert("RGBA"))
left, top, right, bottom = MAP_CONTENT
paper = paper.resize((right - left, bottom - top), Image.LANCZOS)
opaque = paper.getchannel("A").point(lambda v: 255 if v > 200 else 0)
factor = MAP_PAPER_TARGET_L / ImageStat.Stat(paper.convert("L"), opaque).mean[0]
paper_rgb = paper.convert("RGB").point(lambda v: min(255, round(v * factor)))
paper_rgb.putalpha(paper.getchannel("A"))
paper = paper_rgb
mean = ImageStat.Stat(paper.convert("RGB"), opaque).mean
print(f"map paper mean #{round(mean[0]):02X}{round(mean[1]):02X}{round(mean[2]):02X} (factor {factor:.3f}) -> ChernobogAct.MapBgColor")
tall = Image.new("RGBA", (MAP_SIZE[0], MAP_SIZE[1] * 3), (0, 0, 0, 0))
tall.alpha_composite(paper, (left, top))
for i, part in enumerate(("top", "middle", "bottom")):
    piece = tall.crop((0, i * MAP_SIZE[1], MAP_SIZE[0], (i + 1) * MAP_SIZE[1]))
    save(piece, f"map/map_{part}_chernobog_act.png")

# 能力图标：原版图集里没有这张图时，加载器回退到 res://images/powers/<能力 id>.png（原版是 256×256），大图标也读这个路径。
for name in ("reunion_permanent_regen_power", "reunion_hover_power", "reunion_host_remnant_power",
             "reunion_guard_cover_power", "reunion_pack_fury_power", "reunion_patriot_armor_power",
             "reunion_patriot_oath_power", "reunion_patriot_rebirth_power", "reunion_patriot_wrath_power"):
    icon = Image.open(GEN / f"{name}.png").convert("RGBA")
    save(fit(icon, (256, 256), "center"), f"powers/{name}.png")

# 地图 Boss 节点：照原版知识恶魔的占位图写法（BossNodePath 指向 images/map/placeholder/<id>_icon），画布与原版一致 352×300、内容区约 312×282。
# 原版两张图的分工：<id>_icon 是黑色墨线（游戏里染成地图墨色），<id>_icon_outline 是整块填满的外轮廓（染成纸面色，垫在墨线下面），
# 合起来就是“纸上的一枚墨线徽章”。源图由 codex 按这个格式画成绿底、白色填充、黑色墨线（参考图是原版节点合成的同格式图）。
# 拆层：从画布四边对绿色做洪水填充，没被填到的就是徽章本体 → 描边层（略外扩压住锯齿）；本体里的深色像素 → 墨线层。
NODE_CANVAS = (352, 300)
NODE_CONTENT = (312, 282)


def split_ink_badge(src: Image.Image) -> tuple[Image.Image, Image.Image]:
    rgb = np.asarray(src.convert("RGB")).astype(np.int32)
    green = (rgb[..., 1] - np.maximum(rgb[..., 0], rgb[..., 2])) > 80
    h, w = green.shape
    outside = np.zeros_like(green)
    stack = [(y, x) for y in (0, h - 1) for x in range(w)] + [(y, x) for y in range(h) for x in (0, w - 1)]
    while stack:
        y, x = stack.pop()
        if outside[y, x] or not green[y, x]:
            continue
        outside[y, x] = True
        if y > 0: stack.append((y - 1, x))
        if y < h - 1: stack.append((y + 1, x))
        if x > 0: stack.append((y, x - 1))
        if x < w - 1: stack.append((y, x + 1))
    body = Image.fromarray(np.where(outside, 0, 255).astype(np.uint8))
    # 描边比墨线外沿多出一圈（原版约 4px@352），源图里的白边之外再外扩约 0.5%。
    fill = body.filter(ImageFilter.MaxFilter(max(3, (w // 200) | 1)))
    luma = rgb[..., 0] * 0.299 + rgb[..., 1] * 0.587 + rgb[..., 2] * 0.114
    # 原版节点只有纯黑纯白，中间值只出现在线条边缘的抗锯齿上：这里用陡峭阈值，源图里的灰都归到黑或白。
    ink = np.clip((150.0 - luma) * (255.0 / 40.0), 0, 255)
    ink[outside] = 0
    return Image.fromarray(ink.astype(np.uint8)), fill


for slug, source in (("reunion_patriot_boss_icon", "reunion_patriot_boss_node_source_green"),):
    ink, fill = split_ink_badge(Image.open(GEN / f"{source}.png"))
    # 两张按描边（较大）的包围盒一起裁、一起缩放，叠在节点上才对得齐。
    box = fill.point(lambda v: 255 if v > 8 else 0).getbbox()
    scale = min(NODE_CONTENT[0] / (box[2] - box[0]), NODE_CONTENT[1] / (box[3] - box[1]))
    size = (round((box[2] - box[0]) * scale), round((box[3] - box[1]) * scale))
    for suffix, alpha in (("", ink), ("_outline", fill)):
        white = Image.new("RGBA", size, (255, 255, 255, 0))
        small = alpha.crop(box).resize(size, Image.LANCZOS)
        if suffix == "":
            # 缩小后细线会被平均成半透明；再压一次对比，只留边缘一两像素的过渡（原版墨线层半透明像素约为实心像素的 0.2–0.9 倍）。
            small = small.point(lambda v: max(0, min(255, (v - 64) * 2)))
        white.putalpha(small)
        canvas = Image.new("RGBA", NODE_CANVAS, (0, 0, 0, 0))
        canvas.alpha_composite(white, ((NODE_CANVAS[0] - size[0]) // 2, (NODE_CANVAS[1] - size[1]) // 2))
        save(canvas, f"map_nodes/{slug}{suffix}.png")

# 对局历史的 Boss 头像：照原版 88×88，彩色头像占约 76×76 居中；_outline 是头像透明度外扩约 3 像素的纯白剪影（原版描边层就是这样，
# 游戏把它垫在头像下面当白边）。
RUN_HISTORY_CANVAS = 88
RUN_HISTORY_CONTENT = 76
RUN_HISTORY_OUTLINE_PX = 3

for slug, source in (("reunion_patriot_boss", "reunion_patriot_boss_run_history"),):
    head = fit(Image.open(GEN / f"{source}.png").convert("RGBA"), (RUN_HISTORY_CONTENT,) * 2, "center")
    canvas = Image.new("RGBA", (RUN_HISTORY_CANVAS,) * 2, (0, 0, 0, 0))
    offset = (RUN_HISTORY_CANVAS - RUN_HISTORY_CONTENT) // 2
    canvas.alpha_composite(head, (offset, offset))
    save(canvas, f"run_history/{slug}.png")
    ring = canvas.getchannel("A").filter(ImageFilter.MaxFilter(RUN_HISTORY_OUTLINE_PX * 2 + 1)).filter(ImageFilter.GaussianBlur(0.6))
    outline = Image.new("RGBA", canvas.size, (255, 255, 255, 0))
    outline.putalpha(ring)
    save(outline, f"run_history/{slug}_outline.png")

"""生成本幕事件与诅咒“矿石病”的本地化（events.json / cards.json），并往 encounters.json 补事件战斗的标题。

键前缀是 RitsuLib 注册后的真实模型 id（ARKNIGHTS_CHERNOBOG_EVENT_<类名蛇形>、ARKNIGHTS_CHERNOBOG_CARD_<类名蛇形>），
自检会逐个核对标题是否存在。选项里的数值与 src/Events/ChernobogEvents.cs 的常量一一对应，改数值两边同步。
用法：python3 tools/write_event_loc.py（覆盖 events.json、cards.json；encounters.json 只增改这里列出的键）。
"""
import json
from pathlib import Path

LOC = Path(__file__).resolve().parent.parent / "assets" / "localization"
EVENT = "ARKNIGHTS_CHERNOBOG_EVENT_"
CARD = "ARKNIGHTS_CHERNOBOG_CARD_"

LEAVE_ZH = ("离开", "什么也不做。")
LEAVE_EN = ("Leave", "Do nothing.")

# 事件：id 后缀 → 语言 → (标题, 初始描述, {选项: (标题, 描述)}, {结果页: 描述})
EVENTS = {
    "SUPPLY_CACHE": {
        "zhs": (
            "冻土补给箱",
            "积雪的小巷尽头，堆着一批被[orange]整合运动[/orange]遗弃的补给箱。几只木箱已经被人撬开过，剩下的口粮和弹药还冻在里面。\n\n"
            "最里面那只[gold]铁箱[/gold]挂着一把大锁，锁上的霜还没有人碰过。\n\n远处的黑暗里，好像有什么东西在[jitter]喘气[/jitter]。",
            {
                "RATIONS": ("拿走口粮", "回复[green]18[/green]点生命。"),
                "AMMO": ("翻找弹药", "从[blue]3[/blue]张攻击牌中选择[blue]1[/blue]张加入你的[gold]牌组[/gold]。"),
                "PRY": ("撬开铁箱", "与[red]游击队猎犬[/red]战斗。获胜后额外获得[blue]75[/blue][gold]金币[/gold]。"),
            },
            {
                "RATIONS": "冻得梆硬的罐头和压缩饼干。你就着雪水吃了一顿怎么也热不起来的饭，但总算[green]缓过劲来[/green]。",
                "AMMO": "弹药袋里除了子弹，还夹着一本写满[gold]战术笔记[/gold]的小册子，字迹潦草，却很实用。",
            },
        ),
        "eng": (
            "Frozen Supply Cache",
            "At the end of a snowed-in alley lies a pile of supply crates abandoned by [orange]Reunion[/orange]. A few have already been pried open; "
            "the rations and ammunition left inside are frozen solid.\n\nThe [gold]steel chest[/gold] at the back is still padlocked, its frost untouched.\n\n"
            "Somewhere in the dark, something is [jitter]panting[/jitter].",
            {
                "RATIONS": ("Take the rations", "Heal [green]18[/green] HP."),
                "AMMO": ("Search for ammunition", "Choose [blue]1[/blue] of [blue]3[/blue] Attack cards to add to your [gold]Deck[/gold]."),
                "PRY": ("Pry open the chest", "Fight [red]Guerrilla Hounds[/red]. Gain an extra [blue]75[/blue] [gold]Gold[/gold] if you win."),
            },
            {
                "RATIONS": "Rock-hard tins and ration bars. You wash down a meal that refuses to warm up with snowmelt, but you [green]feel stronger[/green].",
                "AMMO": "Tucked between the magazines is a notebook full of scrawled [gold]tactical notes[/gold]. Messy, but useful.",
            },
        ),
    },
    "GUERRILLA_CAMPFIRE": {
        "zhs": (
            "游击队的篝火",
            "几堵断墙挡住了风雪，一小堆[orange]篝火[/orange]噼啪作响。围坐在火边的是几名裹着厚斗篷的游击队老兵，一面伤痕累累的[gold]大盾[/gold]靠在墙边。\n\n"
            "他们看了你一眼，没有拔刀。其中一人往火里添了块木头，示意你坐下。",
            {
                "SHARE": ("分享食物", "花费[blue]50[/blue][gold]金币[/gold]。最大生命值+[green]8[/green]。"),
                "SHARE_LOCKED": ("已锁定", "需要[blue]50[/blue][gold]金币[/gold]。"),
                "LISTEN": ("听老兵讲过去", "[gold]升级[/gold]一张牌。"),
                "LEAVE": ("悄悄离开", "什么也不做。"),
            },
            {
                "SHARE": "你把带来的物资分给了他们。锅里的汤很稀，但每个人都分到了一碗。\n\n老兵们说起在冻原上行军的日子——那时候，他们的长官总是[gold]最后一个[/gold]吃饭。",
                "LISTEN": "老兵讲起一场雪原上的阻击战：怎么用盾墙挡住铳弹，怎么在队伍溃散之前再撑一轮。\n\n你默默记下了其中的[gold]诀窍[/gold]。",
                "LEAVE": "你没有打扰他们，悄悄绕开了火光。身后传来一阵低低的[sine]歌声[/sine]。",
            },
        ),
        "eng": (
            "Guerrilla Campfire",
            "A few broken walls hold back the snow, and a small [orange]campfire[/orange] crackles between them. Around it sit several guerrilla veterans in heavy cloaks, "
            "a battered [gold]tower shield[/gold] propped against the wall.\n\nThey glance at you and leave their blades sheathed. One of them feeds a plank to the fire and nods for you to sit.",
            {
                "SHARE": ("Share your food", "Spend [blue]50[/blue] [gold]Gold[/gold]. Gain [green]8[/green] Max HP."),
                "SHARE_LOCKED": ("Locked", "Requires [blue]50[/blue] [gold]Gold[/gold]."),
                "LISTEN": ("Listen to the veterans", "[gold]Upgrade[/gold] a card."),
                "LEAVE": ("Slip away", "Do nothing."),
            },
            {
                "SHARE": "You share what you brought. The soup in the pot is thin, but everyone gets a bowl.\n\nThe veterans talk about marching across the tundra, when their commander always ate [gold]last[/gold].",
                "LISTEN": "A veteran recounts a holding action on the snowfield: how the shield wall stopped the gunfire, how to hold one more round before the line breaks.\n\nYou quietly take note of the [gold]tricks[/gold].",
                "LEAVE": "You leave them be and skirt around the firelight. Behind you, a low [sine]song[/sine] begins.",
            },
        ),
    },
    "ORIGINIUM_VEIN": {
        "zhs": (
            "源石矿脉",
            "管道隧道的墙壁裂开了，一大簇黑色的[orange]源石结晶[/orange]从钢板缝里刺出来，裂纹里透着[orange][sine]琥珀色的光[/sine][/orange]。\n\n"
            "地上丢着一把矿镐，镐柄上缠着的布已经发黑。你知道徒手碰它意味着什么。",
            {
                "TOUCH": ("触碰结晶", "从[blue]3[/blue]张[gold]稀有[/gold]牌中选择[blue]1[/blue]张加入你的[gold]牌组[/gold]。将一张[red]矿石病[/red]加入你的[gold]牌组[/gold]。"),
                "MINE": ("开采", "失去[red]9[/red]点生命。获得[blue]85[/blue][gold]金币[/gold]。"),
                "MINE_LOCKED": ("已锁定", "你的生命值太低了。"),
                "LEAVE": LEAVE_ZH,
            },
            {
                "TOUCH": "结晶在你掌心里是温热的。一阵[jitter]刺痛[/jitter]过后，脑海里多了些不属于你的东西。\n\n手背上，一粒黑色的[red]结晶[/red]悄悄冒了出来。",
                "MINE": "你用布裹住手，一镐一镐地把碎晶敲下来。碎屑划破了手臂，但这些结晶在黑市上[gold]很值钱[/gold]。",
                "LEAVE": "你退后几步，绕开了这片结晶。有些东西，最好永远别碰。",
            },
        ),
        "eng": (
            "Originium Vein",
            "The wall of the conduit tunnel has split open. A cluster of black [orange]Originium crystals[/orange] juts out between the steel plates, "
            "[orange][sine]amber light[/sine][/orange] seeping through the cracks.\n\nA pickaxe lies on the ground, the cloth on its handle gone black. You know what touching it bare-handed means.",
            {
                "TOUCH": ("Touch the crystal", "Choose [blue]1[/blue] of [blue]3[/blue] [gold]Rare[/gold] cards to add to your [gold]Deck[/gold]. Add an [red]Oripathy[/red] to your [gold]Deck[/gold]."),
                "MINE": ("Mine it", "Lose [red]9[/red] HP. Gain [blue]85[/blue] [gold]Gold[/gold]."),
                "MINE_LOCKED": ("Locked", "Your HP is too low."),
                "LEAVE": LEAVE_EN,
            },
            {
                "TOUCH": "The crystal is warm in your palm. After a [jitter]sting[/jitter], something that is not yours settles in your mind.\n\nOn the back of your hand, a tiny black [red]crystal[/red] quietly breaks the skin.",
                "MINE": "You wrap your hands in cloth and chip the shards loose one swing at a time. The splinters cut your arms, but these crystals are [gold]worth a fortune[/gold] on the black market.",
                "LEAVE": "You back away and give the crystals a wide berth. Some things are best never touched.",
            },
        ),
    },
    "CORE_VALVE": {
        "zhs": (
            "核心城动力阀",
            "引擎舱深处，一只巨大的[gold]阀门手轮[/gold]卡在管道汇合处，接缝里嘶嘶地喷着[sine]蒸汽[/sine]。上方的[red]警示灯[/red]一直在闪，压力表的指针早已顶到了尽头。\n\n"
            "这座移动城市还在前进，而它的心脏快要撑不住了。",
            {
                "VENT": ("手动泄压", "失去[red]7[/red]点生命。从你的[gold]牌组[/gold]中移除一张牌。"),
                "VENT_LOCKED": ("已锁定", "你的生命值太低，或没有可以移除的牌。"),
                "OVERLOAD": ("让它超载", "[gold]变化[/gold]你[gold]牌组[/gold]中的[blue]2[/blue]张牌。"),
                "OVERLOAD_LOCKED": ("已锁定", "你的[gold]牌组[/gold]中没有足够可以变化的牌。"),
                "LEAVE": LEAVE_ZH,
            },
            {
                "VENT": "你咬牙转动手轮，滚烫的蒸汽扑面而来。压力终于[green]降了下来[/green]，你也甩掉了一些多余的负担。",
                "OVERLOAD": "你反其道而行，把阀门拧到了底。管道剧烈[jitter]震动[/jitter]，一阵白光过后，你的记忆被[purple]搅成了一团[/purple]。",
                "LEAVE": "你没有去碰它。身后的警示灯还在一明一灭。",
            },
        ),
        "eng": (
            "Core City Valve",
            "Deep in the engine hold, a huge [gold]valve wheel[/gold] sits where the pipes converge, [sine]steam[/sine] hissing from every seam. The [red]warning lamp[/red] above keeps flashing; "
            "the pressure gauge pinned itself long ago.\n\nThe mobile city is still moving, and its heart is about to give out.",
            {
                "VENT": ("Vent it by hand", "Lose [red]7[/red] HP. Remove a card from your [gold]Deck[/gold]."),
                "VENT_LOCKED": ("Locked", "Your HP is too low, or you have no card to remove."),
                "OVERLOAD": ("Let it overload", "[gold]Transform[/gold] [blue]2[/blue] cards in your [gold]Deck[/gold]."),
                "OVERLOAD_LOCKED": ("Locked", "Your [gold]Deck[/gold] has too few cards that can be transformed."),
                "LEAVE": LEAVE_EN,
            },
            {
                "VENT": "You grit your teeth and turn the wheel as scalding steam blasts your face. The pressure finally [green]drops[/green], and you shed some dead weight along with it.",
                "OVERLOAD": "You do the opposite and crank the valve all the way. The pipes [jitter]shudder[/jitter], a flash of white light follows, and your memories are [purple]scrambled[/purple].",
                "LEAVE": "You leave it alone. Behind you, the warning lamp keeps blinking.",
            },
        ),
    },
    "RHODES_AIRDROP": {
        "zhs": (
            "罗德岛空投",
            "雪地上斜插着一只[blue]补给舱[/blue]，降落伞瘫在一旁。舱门半开，里面透出冷白色的灯光。\n\n"
            "天上，一架小型无人机正在远去——看来是友方送来的。舱里只剩两个箱子，你只来得及带走一个。",
            {
                "MEDKIT": ("打开医疗箱", "回复最大生命值[green]30%[/green]的生命。"),
                "SUPPLIES": ("打开药剂箱", "获得[blue]2[/blue]瓶随机药水。"),
            },
            {
                "MEDKIT": "绷带、止血剂和一支[green]急救针[/green]。你简单处理了伤口，感觉好多了。",
                "SUPPLIES": "箱子里整整齐齐地码着几支[blue]药剂[/blue]，标签上写着用法，还有一句手写的“[gold]保重[/gold]”。",
            },
        ),
        "eng": (
            "Rhodes Island Airdrop",
            "A [blue]supply pod[/blue] juts from the snow at an angle, its parachute slumped beside it. The hatch hangs half open, cold white light spilling out.\n\n"
            "Overhead, a small drone is flying away. A friendly delivery, it seems. Only two crates remain inside, and you only have time to take one.",
            {
                "MEDKIT": ("Open the medkit", "Heal [green]30%[/green] of your Max HP."),
                "SUPPLIES": ("Open the supply crate", "Obtain [blue]2[/blue] random potions."),
            },
            {
                "MEDKIT": "Bandages, hemostatics and a [green]field injector[/green]. You patch yourself up and feel much better.",
                "SUPPLIES": "Neatly packed [blue]vials[/blue], each labeled with instructions, and a handwritten note that says \"[gold]Take care[/gold].\"",
            },
        ),
    },
    "BLACK_SNAKE_WHISPER": {
        "zhs": (
            "黑蛇的低语",
            "空旷的大厅里，黑暗在[purple][sine]缓缓蠕动[/sine][/purple]。一条巨大的黑蛇盘踞在那里，只有一只眼睛燃着[red]暗红色的火光[/red]。\n\n"
            "“你想要力量。”它的声音像是从你自己的脑海里响起，“我可以给你。只要付出一点……[jitter]代价[/jitter]。”",
            {
                "BLOOD": ("以血为契", "失去[red]6[/red]点最大生命值。获得一件随机[gold]遗物[/gold]。"),
                "BLOOD_LOCKED": ("已锁定", "你的最大生命值太低了。"),
                "GIFT": ("收下馈赠", "获得[blue]150[/blue][gold]金币[/gold]。将一张[red]矿石病[/red]加入你的[gold]牌组[/gold]。"),
                "REFUSE": ("拒绝", "什么也不做。"),
            },
            {
                "BLOOD": "你的血滴落在地上，被黑暗[sine]吞没[/sine]。等你回过神来，手中多了一件东西。\n\n黑蛇的眼睛眯了起来，像是在笑。",
                "GIFT": "一袋沉甸甸的[gold]金币[/gold]落在你脚边。你弯腰去捡时，指尖碰到了一粒冰冷的[red]结晶[/red]。",
                "REFUSE": "你转身离开。黑暗里传来一声低笑：“[purple]我们还会再见的。[/purple]”",
            },
        ),
        "eng": (
            "Whisper of the Black Snake",
            "In the empty hall, the darkness [purple][sine]writhes slowly[/sine][/purple]. A great black snake lies coiled there, a single eye burning with [red]dim red fire[/red].\n\n"
            "\"You want power,\" it says, the voice rising from inside your own head. \"I can give it to you. For a small... [jitter]price[/jitter].\"",
            {
                "BLOOD": ("Seal it in blood", "Lose [red]6[/red] Max HP. Obtain a random [gold]Relic[/gold]."),
                "BLOOD_LOCKED": ("Locked", "Your Max HP is too low."),
                "GIFT": ("Accept the gift", "Gain [blue]150[/blue] [gold]Gold[/gold]. Add an [red]Oripathy[/red] to your [gold]Deck[/gold]."),
                "REFUSE": ("Refuse", "Do nothing."),
            },
            {
                "BLOOD": "Your blood drips to the floor and the darkness [sine]swallows[/sine] it. When you come to, something is in your hand.\n\nThe snake's eye narrows, as if smiling.",
                "GIFT": "A heavy pouch of [gold]Gold[/gold] lands at your feet. As you bend to pick it up, your fingertips brush a cold [red]crystal[/red].",
                "REFUSE": "You turn to leave. A low laugh follows you from the dark: \"[purple]We will meet again.[/purple]\"",
            },
        ),
    },
}

CARDS = {
    "ORIPATHY": {
        "zhs": ("矿石病", "在你的回合结束时，如果这张牌在你的[gold]手牌[/gold]中，你受到等同于当前回合数的伤害。"),
        "eng": ("Oripathy", "At the end of your turn, if this is in your [gold]Hand[/gold], take damage equal to the current turn number."),
    },
}

ENCOUNTERS = {
    "REUNION_SUPPLY_AMBUSH_EVENT": {
        "zhs": ("补给箱伏击", "{character}没能从[gold]{encounter}[/gold]里逃出来。"),
        "eng": ("Supply Cache Ambush", "{character} never made it out of the [gold]{encounter}[/gold]."),
    },
}


def write(path: Path, data: dict) -> None:
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"{path.relative_to(LOC.parent.parent)}: {len(data)} keys")


for lang in ("zhs", "eng"):
    events: dict[str, str] = {}
    for suffix, langs in EVENTS.items():
        title, initial, options, pages = langs[lang]
        key = EVENT + suffix
        events[f"{key}.title"] = title
        events[f"{key}.pages.INITIAL.description"] = initial
        for option, (option_title, option_desc) in options.items():
            events[f"{key}.pages.INITIAL.options.{option}.title"] = option_title
            events[f"{key}.pages.INITIAL.options.{option}.description"] = option_desc
        for page, desc in pages.items():
            events[f"{key}.pages.{page}.description"] = desc
    write(LOC / lang / "events.json", events)

    cards = {}
    for suffix, langs in CARDS.items():
        title, desc = langs[lang]
        cards[f"{CARD}{suffix}.title"] = title
        cards[f"{CARD}{suffix}.description"] = desc
    write(LOC / lang / "cards.json", cards)

    encounters_path = LOC / lang / "encounters.json"
    encounters = json.loads(encounters_path.read_text(encoding="utf-8"))
    for key, langs in ENCOUNTERS.items():
        title, loss = langs[lang]
        encounters[f"{key}.title"] = title
        encounters[f"{key}.loss"] = loss
    write(encounters_path, encounters)

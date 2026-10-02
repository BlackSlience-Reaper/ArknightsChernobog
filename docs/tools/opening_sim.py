"""苦难摇篮遭遇战的开局伤害模拟：按招式算前 N 个敌方回合玩家要承受的伤害，并为每个怪挑起手位置。

口径（普通难度，玩家不减伤、不打死任何怪）：
- 攻击伤害 =（基础 + 自身力量 + 领袖气质）× 段数；玩家带易伤时每段 ×1.5 向下取整。
- 领袖气质：持有者之外的盟友每段 +层数（原版 LeadershipPower）。
- 易伤在敌方回合结束时掉 1 层：本回合后出手的怪与之后 (层数-1) 个敌方回合生效。
- 固定循环的怪按表中顺序出招；RANDOM 里的怪第一招按起手取，之后每回合从其余招式里任选一招（不连用同一招，照原版外骨骼虫）。
  随机怪把所有可能的出招序列都走一遍，每回合取最差值（逐回合最大伤害），按这个最差结果打分。
- 召唤：招式效果 summon 在队尾加一名怪（最多 SUMMON_CAP 名），从下一个敌方回合起出手（原版刚召唤的怪当回合不行动），
  起手用场上第一只同种怪本回合出的招（与游戏里传令兵组长的做法一致）。
- 同一场里的同种怪起手必须不同（硬约束）。
- 出手顺序 = 阵容顺序，召唤的怪排在最后。
- 随机阵容：按怪物种类（而不是站位）挑起手，取所有可能组合里最差的一组来判超标。
- 不再有“给其他盟友加力量”的招式（会让后出手的怪意图在本回合里涨伤害，玩家按意图算的伤害就不准）；
  格挡、削弱玩家、塞状态牌不影响敌方出伤，模拟里记为空效果。
参照原版蜂巢（普通难度）第 1 回合：弱怪约 12–22，普通 0–25（常见 10–16），精英约 15–24。
"""
from itertools import combinations, product

# 招式：(名称, 单段伤害, 段数, 效果)；效果键 self_str / vuln / summon。
M = {
    "猎犬": [("扑咬", 4, 2, {}), ("撕咬", 7, 1, {}), ("环伺", 0, 0, {"self_str": 1})],
    "猎犬pro": [("扑咬", 5, 2, {}), ("撕咬", 8, 1, {}), ("嚎叫", 0, 0, {"self_str": 2})],
    "战士": [("劈砍", 9, 1, {}), ("全力冲锋", 6, 1, {"self_str": 2}), ("压制", 5, 1, {})],
    "战士组长": [("劈砍", 11, 1, {}), ("全力冲锋", 7, 1, {"self_str": 2}), ("督战", 0, 0, {})],
    "狙击手": [("狙击", 9, 1, {}), ("狙击", 9, 1, {}), ("瞄准", 0, 0, {"self_str": 2})],
    "狙击手组长": [("狙击", 11, 1, {}), ("标定目标", 0, 0, {"vuln": 1}), ("双重狙击", 5, 2, {})],
    "传令兵组长": [("呼叫增援", 0, 0, {"summon": "战士"}), ("殴打", 8, 1, {}), ("战旗", 0, 0, {})],
    "盾卫": [("推进", 9, 1, {}), ("盾击", 15, 1, {}), ("盾墙", 0, 0, {})],
    "盾卫组长": [("推进", 10, 1, {}), ("盾击", 15, 1, {}), ("盾墙", 0, 0, {})],
    "迫击炮兵": [("装填", 0, 0, {}), ("炮击", 16, 1, {}), ("急速射", 4, 3, {})],
    "迫击炮兵组长": [("装填", 0, 0, {}), ("炮击", 22, 1, {}), ("急速射", 5, 3, {})],
    "突袭战士": [("降落重击", 16, 1, {}), ("起飞", 0, 0, {"self_str": 2})],
    "突袭战士组长": [("降落重击", 18, 1, {}), ("起飞", 0, 0, {"self_str": 3})],
    "萨卡兹战士": [("斩击", 14, 1, {}), ("斩击", 14, 1, {}), ("仪式强化", 0, 0, {"self_str": 3})],
    "萨卡兹战士组长": [("斩击", 15, 1, {}), ("斩击", 15, 1, {}), ("仪式强化", 0, 0, {"self_str": 4})],
    "萨卡兹术师": [("源石法术", 9, 1, {}), ("献祭仪式", 0, 0, {"self_str": 3}), ("源石洪流", 3, 3, {})],
    "萨卡兹术师组长": [("源石法术", 11, 1, {}), ("献祭仪式", 0, 0, {"self_str": 3}), ("源石洪流", 5, 3, {})],
    "宿主士兵": [("劈砍", 9, 1, {}), ("猛扑", 5, 2, {}), ("溃烂撕咬", 6, 1, {})],
    "宿主拾荒者": [("乱砸", 8, 1, {}), ("撕扯", 5, 1, {}), ("翻找", 0, 0, {"self_str": 1})],
    "宿主流浪者": [("重击", 9, 1, {}), ("嘶吼", 0, 0, {"self_str": 2}), ("蹒跚冲撞", 6, 1, {})],
    "宿主士兵组长": [("劈砍", 8, 1, {}), ("牧群号令", 0, 0, {}), ("连斩", 4, 2, {})],
    "狂暴宿主士兵": [("狂斩", 7, 3, {}), ("撕裂", 18, 1, {}), ("狂嚎", 0, 0, {"self_str": 3})],
    "狂暴宿主投掷手": [("投掷", 14, 1, {}), ("乱掷", 5, 3, {}), ("砸石", 8, 1, {})],
    "狂暴宿主组长": [("狂暴连斩", 9, 3, {}), ("狂乱之嚎", 0, 0, {"self_str": 2}), ("处决", 26, 1, {})],
    "特战士兵": [("伏击", 14, 1, {}), ("连刺", 6, 2, {}), ("隐蔽", 0, 0, {})],
    "特战术师": [("源石冲击", 10, 1, {}), ("腐蚀法术", 7, 1, {}), ("源石屏障", 0, 0, {})],
    "法术大师A1": [("法术射线", 3, 3, {}), ("法术射线", 3, 3, {}), ("超载", 0, 0, {"self_str": 2})],
    # 爱国者一阶段（二阶段在重生之后，开局 4 回合内打不到）。残甲只影响玩家打它，不影响它的出伤。
    "爱国者": [("行军", 0, 0, {}), ("长戟四连", 4, 4, {}), ("盾击", 16, 1, {})],
}
# 随机出招的怪（代码里用 ReunionMonster.RandomMachine / ReunionHostMonster.HostRandomMachine）。
RANDOM = {"猎犬", "宿主士兵", "宿主拾荒者", "宿主流浪者", "狂暴宿主士兵", "狂暴宿主投掷手"}
LEADERSHIP = {"传令兵组长": 3}
SUMMON_CAP = 2
# 固定起手：组长空降兵先降落；传令兵组长先呼叫增援（同原版卵翼虫开场下蛋）。
FIXED = {"突袭战士组长": [0], "传令兵组长": [0]}

HOST_WORKERS = ["宿主士兵", "宿主拾荒者", "宿主流浪者"]
# 值是一个阵容，或阵容列表（随机阵容，列出全部可能）。
ENCOUNTERS = {
    "弱|猎犬群": ["猎犬", "猎犬"],
    "弱|失控的牧群": ["宿主士兵", "宿主士兵"],
    "弱|空降兵": ["突袭战士"],
    "弱|整合运动残党": ["特战士兵", "法术大师A1"],
    "普|猎犬群": ["猎犬", "猎犬", "猎犬pro"],
    "普|梅菲斯特的牧群": [["宿主士兵组长", a, b] for a, b in combinations(HOST_WORKERS, 2)],
    "普|狂暴宿主": [["狂暴宿主士兵"], ["狂暴宿主投掷手"]],
    "普|特战分队": ["特战士兵", "特战术师", "法术大师A1"],
    "普|游击队突击组": ["战士组长", "战士", "战士"],
    "普|狙击阵地": ["狙击手组长", "狙击手", "狙击手"],
    "普|炮击阵地": ["盾卫", "迫击炮兵"],
    "普|浸染": ["萨卡兹战士", "萨卡兹术师"],
    "普|空降小队": ["突袭战士", "突袭战士"],
    "普|增援信号": ["战士", "传令兵组长"],
    "精|感染者之盾": ["盾卫组长", "迫击炮兵组长"],
    "精|垂直打击": ["突袭战士组长", "突袭战士"],
    "精|萨卡兹仪式": ["萨卡兹战士组长", "萨卡兹术师组长"],
    "精|狂暴宿主组长": ["狂暴宿主组长"],
    "首|爱国者": ["战士", "战士", "爱国者"],
}
# 各类型的开局上限与后续回合上限（普通难度）。
# 首领参照原版第二幕：暴食者/知识恶魔第 1 回合不攻击，凯撒蟹约 15；之后单回合最高 31–35。
CAPS = {"弱": (20, 26), "普": (22, 30), "精": (26, 36), "首": (20, 36)}
TURNS = 4


def next_moves(name: str, last: int, step: int, offset: int) -> list:
    """本回合可能出的招（招式序号）。固定循环只有一种；随机怪第一招按起手，之后不连用上一招。"""
    n = len(M[name])
    if name not in RANDOM:
        return [(offset + step) % n]
    if step == 0:
        return [offset % n]
    return [i for i in range(n) if i != last]


def simulate(lineup: list, offsets: tuple, turns: int = TURNS) -> list:
    """所有随机出招序列的逐回合最大伤害。"""
    worst = [0] * turns

    def run(t, members, strength, vuln, summoned, dmg_so_far):
        if t == turns:
            for i, d in enumerate(dmg_so_far):
                worst[i] = max(worst[i], d)
            return
        active = [i for i, m in enumerate(members) if m["start"] <= t]
        choices = [next_moves(members[i]["name"], members[i]["last"], t - members[i]["start"], members[i]["offset"]) for i in active]
        for picks in product(*choices):
            mem = [dict(m) for m in members]
            st = list(strength)
            v = vuln
            s = summoned
            total = 0
            for i, move in zip(active, picks):
                m = mem[i]
                m["last"] = move
                _, dmg, hits, eff = M[m["name"]][move]
                lead = sum(LEADERSHIP.get(o["name"], 0) for j, o in enumerate(mem) if j != i and o["start"] <= t)
                if hits:
                    per = dmg + st[i] + lead
                    if v > 0:
                        per = int(per * 1.5)
                    total += per * hits
                st[i] += eff.get("self_str", 0)
                if eff.get("vuln"):
                    v = max(v, eff["vuln"])
                if eff.get("summon") and s < SUMMON_CAP:
                    s += 1
                    name = eff["summon"]
                    # 与游戏里一致：新召来的怪起手用场上第一只同种怪本回合出的招。
                    same = next((o for o in mem if o["name"] == name and o["start"] <= t), None)
                    mem.append({"name": name, "offset": same["last"] if same else 0, "last": -1, "start": t + 1})
                    st.append(0)
            run(t + 1, mem, st, max(0, v - 1), s, dmg_so_far + [total])

    members = [{"name": n, "offset": o, "last": -1, "start": 0} for n, o in zip(lineup, offsets)]
    run(0, members, [0] * len(members), 0, 0, [])
    return worst


def options_for(name: str):
    return FIXED.get(name) or range(len(M[name]))


def choose(kind: str, lineups: list):
    """固定阵容按站位挑起手；随机阵容按种类挑起手（同种同起手），用所有组合里最差的结果打分。"""
    first_cap, peak_cap = CAPS[kind]
    randomized = len(lineups) > 1
    keys = sorted({n for lu in lineups for n in lu}) if randomized else list(range(len(lineups[0])))
    names = keys if randomized else lineups[0]
    best = None
    for combo in product(*(options_for(n) for n in names)):
        # 同一场里的同种怪起手必须不同（随机阵容里同种最多一只，按种类挑不受影响）。
        if not randomized and any(names[a] == names[b] and combo[a] == combo[b]
                                  for a in range(len(names)) for b in range(a + 1, len(names))):
            continue
        pick = dict(zip(keys, combo))
        results = [simulate(lu, tuple(pick[n] if randomized else pick[i] for i, n in enumerate(lu))) for lu in lineups]
        worst = max(results, key=lambda d: (max(0, d[0] - first_cap) * 3 + sum(max(0, x - peak_cap) for x in d[1:]), sum(d)))
        over = sum(max(0, d[0] - first_cap) * 3 + sum(max(0, x - peak_cap) for x in d[1:]) for d in results)
        under = sum(max(0, first_cap // 2 - d[0]) for d in results)
        changes = sum(1 for c in combo if c)
        score = (over, under, changes, -sum(sum(d) for d in results))
        if best is None or score < best[0]:
            best = (score, pick, results, worst)
    return best


def describe(pick: dict, lineup: list, randomized: bool) -> str:
    parts = []
    for i, n in enumerate(lineup):
        o = pick[n] if randomized else pick[i]
        parts.append(f"{n}:{M[n][o % len(M[n])][0]}" + ("*" if o else "") + ("(随)" if n in RANDOM else ""))
    return " ".join(parts)


if __name__ == "__main__":
    for key, value in ENCOUNTERS.items():
        kind, title = key.split("|")
        lineups = value if isinstance(value[0], list) else [value]
        (over, _, _, _), pick, results, _ = choose(kind, lineups)
        flag = "" if over == 0 else "  <-- 超标"
        for lu, dmg in zip(lineups, results):
            print(f"{kind} {title:8s} {dmg}  起手 {describe(pick, lu, len(lineups) > 1)}{flag}")

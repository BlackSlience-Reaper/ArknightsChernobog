"""修 PRTS 骨骼经 Spine 3.8 → 4.2 转换后留下的坏时间轴，重新导出 4.2 二进制骨骼。

已知问题：爱国者 revive_2（倒地等待，循环播放）里盾牌槽 C_Shield2 的网格变形时间轴关键帧时间是 0 → 1.333 → 0 → 1.333，
像是两条变形轨道被拼成了一条。时间倒退时运行时只按前两帧插值：盾牌一轮循环里逐渐变形（最多约 36 单位），
循环回到开头时又弹回原样，看起来就是盾牌跳变。后两帧与 revive_1 末帧、revive_3 首帧完全一致，所以只保留后两帧（静止）。

全模组扫过一遍：猎犬（含 pro）Attack、宿主士兵（含组长）Attack/Die/Run_Loop、特战士兵多段动画的附件切换时间轴
也有同样的时间倒退，但每一帧都是同一个附件名，重复了不改变画面，不需要修。

用法：python3 tools/fix_spine_timelines.py
会把 enemy_1506_patrt.skel 与 assets/_imported/enemy_1506_patrt.skel.spskel 一起覆盖（两者按导入约定是同一文件的拷贝）；
修完再扫一遍，确认没有时间倒退的时间轴。需要本机 Spine 编辑器（与转换时同一个 4.2.43）。
"""
import json
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

MOD = Path(__file__).resolve().parent.parent
SPINE = "/Applications/Spine.app/Contents/MacOS/Spine"
VERSION = "4.2.43"

# (骨骼文件名, 动画, 皮肤, 槽位, 附件, 时间轴, 保留的关键帧下标)
FIXES = [
    ("enemy_1506_patrt", "revive_2", "default", "C_Shield2", "C_Shield", "deform", [2, 3]),
]


def spine(*args: str) -> None:
    result = subprocess.run([SPINE, "-u", VERSION, *args], capture_output=True, text=True)
    if result.returncode != 0 or "Complete." not in result.stdout:
        sys.exit(f"Spine failed: {' '.join(args)}\n{result.stdout}\n{result.stderr}")


def non_monotonic(node, trail=()):
    if isinstance(node, list) and node and all(isinstance(k, dict) for k in node):
        times = [k.get("time", 0) for k in node]
        if any(b < a for a, b in zip(times, times[1:])):
            yield "/".join(trail), times
        return
    if isinstance(node, dict):
        for key, value in node.items():
            yield from non_monotonic(value, trail + (key,))


def main() -> None:
    for name in sorted({fix[0] for fix in FIXES}):
        skel = next(MOD.glob(f"assets/animations/monsters/arknights/*/{name}.skel"))
        imported = MOD / "assets" / "_imported" / f"{name}.skel.spskel"
        with tempfile.TemporaryDirectory() as tmp:
            work = Path(tmp)
            shutil.copy2(skel, work / skel.name)
            for ext in (".atlas", ".png"):
                shutil.copy2(skel.with_suffix(ext), work / (name + ext))
            spine("-i", str(work / skel.name), "-o", str(work / "a.spine"), "-r")
            spine("-i", str(work / "a.spine"), "-o", str(work / "json"), "-e", "json")
            json_path = work / "json" / f"{name}.json"
            data = json.loads(json_path.read_text())
            for _, anim, skin, slot, attachment, timeline, keep in (f for f in FIXES if f[0] == name):
                keys = data["animations"][anim]["attachments"][skin][slot][attachment][timeline]
                data["animations"][anim]["attachments"][skin][slot][attachment][timeline] = [keys[i] for i in keep]
                print(f"{name}/{anim}/{slot}/{attachment}/{timeline}: kept keys {keep} of {len(keys)}")
            bad = list(non_monotonic(data["animations"]))
            if bad:
                sys.exit(f"still non-monotonic: {bad}")
            json_path.write_text(json.dumps(data))
            # JSON → 工程 → 4.2 二进制；导出目录里的 .skel 就是修好的骨骼。
            spine("-i", str(json_path), "-o", str(work / "b.spine"), "-r")
            spine("-i", str(work / "b.spine"), "-o", str(work / "bin"), "-e", "binary")
            fixed = work / "bin" / f"{name}.skel"
            if not fixed.exists():
                fixed = next((work / "bin").glob("*.skel"))
            shutil.copy2(fixed, skel)
            shutil.copy2(fixed, imported)
            print(f"wrote {skel.relative_to(MOD)} and {imported.relative_to(MOD)} ({fixed.stat().st_size} bytes)")


if __name__ == "__main__":
    main()

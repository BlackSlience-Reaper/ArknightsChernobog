"""休息处道具矩形的唯一来源：布局示意图、预览拼图和场景改写都从这里取。

矩形取自原版 hive_rest_site 场景的 TextureRect offset（根坐标系）；RestSiteLighting 下的受光副本
LLog2/RLog2 挂在 (-65,-65) 的父节点下，本地 offset 等于根坐标 +65，与主节点重合。
本幕道具按 PROP_SCALE 以各自中心放大，角色坐位由原版代码按固定位置摆放，放大量不宜过大。
"""

PROP_SCALE = 1.25
LIGHTING_OFFSET = (-65.0, -65.0)

PROP_RECTS = {
    "RestSiteLLog": (508.68, 546.6, 827.021, 751.344),
    "RestSiteRLog": (980.68, 540.6, 1345.72, 732.772),
    "RestSiteFireLogs": (802.68, 646.6, 1022.24, 815.873),
}

# 受光副本 → 对应主节点
LIT_COPIES = {
    "RestSiteLighting/RestSiteLLog2": "RestSiteLLog",
    "RestSiteLighting/RestSiteRLog2": "RestSiteRLog",
}


def scaled(rect, scale: float = PROP_SCALE):
    left, top, right, bottom = rect
    cx, cy = (left + right) / 2, (top + bottom) / 2
    hw, hh = (right - left) / 2 * scale, (bottom - top) / 2 * scale
    return (cx - hw, cy - hh, cx + hw, cy + hh)

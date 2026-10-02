using STS2RitsuLib.Scaffolding.Content;

namespace ArknightsChernobog;

/// <summary>
/// 本模组的资源路径，全部在模组命名空间 res://ArknightsChernobog/ 下，经 RitsuLib 的资源配置（AssetProfile）接到游戏上。
/// 目录与 art/install_assets.py 一一对应，改一边要同步另一边；自检（ChernobogSelfTest）逐项加载核对。
/// </summary>
internal static class ChernobogAssets
{
	public const string Root = $"res://{ModEntry.ModId}/";

	private const string Act = "chernobog_act";
	private const string Boss = "reunion_patriot_boss";

	/// <summary>幕：背景根场景 + 图层目录（_bg_NN_x/_fg_x 分组随机）、休息处、地图三段底图。宝箱沿用第二幕原版。</summary>
	public static readonly ActAssetProfile ActProfile = new(
		BackgroundScenePath: Root + $"scenes/backgrounds/{Act}/{Act}_background.tscn",
		BackgroundLayersDirectoryPath: Root + $"scenes/backgrounds/{Act}/layers",
		RestSiteBackgroundPath: Root + $"scenes/rest_site/{Act}_rest_site.tscn",
		MapTopBgPath: Root + $"images/map/map_top_{Act}.png",
		MapMidBgPath: Root + $"images/map/map_middle_{Act}.png",
		MapBotBgPath: Root + $"images/map/map_bottom_{Act}.png");

	/// <summary>
	/// Boss 节点图标的路径前缀：原版 EncounterModel.MapNodeAssetPaths 在它后面拼 .png（剪影）和 _outline.png（描边）。
	/// RitsuLib 的 CustomBossNodePath 会先检查路径本身是否存在，而前缀不是文件，所以由遭遇战直接重写原版的 BossNodePath。
	/// </summary>
	public const string PatriotBossNodePrefix = Root + $"images/map_nodes/{Boss}_icon";

	/// <summary>爱国者战：遭遇场景（站位标记点）、专属背景、对局历史头像；重生才召唤的空降兵与两段音乐随房间预载。</summary>
	public static EncounterAssetProfile PatriotBossProfile(IEnumerable<string> extraAssetPaths) => new(
		EncounterScenePath: EncounterScene(Boss),
		BackgroundScenePath: Root + $"scenes/backgrounds/{Boss}/{Boss}_background.tscn",
		BackgroundLayersDirectoryPath: Root + $"scenes/backgrounds/{Boss}/layers",
		ExtraAssetPaths: [.. extraAssetPaths],
		RunHistoryIconPath: Root + $"images/run_history/{Boss}.png",
		RunHistoryIconOutlinePath: Root + $"images/run_history/{Boss}_outline.png");

	/// <summary>按站位标记点摆放的遭遇战场景（召唤要落在预留的槽位上）。</summary>
	public static string EncounterScene(string sceneName) => Root + $"scenes/encounters/{sceneName}.tscn";

	/// <summary>能力图标：原版图集没有的图，大小图标都用同一张 256×256。</summary>
	public static PowerAssetProfile PowerIcon(string iconName)
	{
		string path = Root + $"images/powers/{iconName}.png";
		return new PowerAssetProfile(IconPath: path, BigIconPath: path);
	}

	public const string EventPortraitRoot = Root + "images/events/";

	public const string CardPortraitRoot = Root + "images/cards/";

	public const string MusicRoot = Root + "audio/music/";
}

using ArknightsChernobog.Encounters;
using Godot;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Unlocks;
using STS2RitsuLib.Scaffolding.Content;

namespace ArknightsChernobog.Acts;

/// <summary>
/// 第二幕变体“摇篮”，取材主线第七章“苦难摇篮”，场景为切尔诺伯格核心城，Boss 固定为爱国者。
/// 经 RitsuLib 注册（ModEntry），模型 id 为 ARKNIGHTS_CHERNOBOG_ACT_CHERNOBOG_ACT，存档、联机开局和 `act` 控制台命令都按它引用，改类名等于换一个幕。
/// - 选幕：AllowInRandomActList 让 RitsuLib 把它并进原版按序号分组的随机幕列表，与蜂巢同为第二幕候选；
///   非默认幕在单人模式下未发现过时会被原版强制选中一次（与暗港相同），之后均匀随机。
/// - 资源：背景、休息处、地图底图都在模组命名空间，经 AssetProfile 接到原版取路径的地方（见 <see cref="ChernobogAssets"/>）。
/// - 内容：遭遇战与本幕事件经 RitsuLib 按幕注册（ModEntry），这里只列复用的原版事件与先古之民。
/// </summary>
public sealed class ChernobogAct : ModActTemplate
{
	public const int ActIndex = 1;

	private static Hive Hive => ModelDb.Act<Hive>();

	public override int Index => ActIndex;

	public override bool IsDefault => false;

	public override bool AllowInRandomActList => true;

	public override ActAssetProfile AssetProfile => ChernobogAssets.ActProfile;

	// MapBgColor 必须等于地图纸面平均色：原版节点着色器把图标“面部”换成它与灰色的中间色，节点描边光晕直接用它，
	// 与纸面不一致就会在每个节点周围多出一圈色晕（原版蜂巢纸面 #9C9663 对 #9B9562）。
	// 纸面由 art/process_generated.py 压到原版明度（约 148），它打印的平均色就是这里的值；换地图底图后要同步。
	// 路线颜色按原版比例取：未走过约为纸面的 0.5–0.8 倍，走过的接近黑色。
	public override Color MapTraveledColor => new("1A1F27");

	public override Color MapUntraveledColor => new("525A64");

	public override Color MapBgColor => new("8D969D");

	public override string[] BgMusicOptions => ["event:/music/act2_a1_v2", "event:/music/act2_a2_v2"];

	public override string[] MusicBankPaths => ["res://banks/desktop/act2_a1.bank", "res://banks/desktop/act2_a2.bank"];

	public override string AmbientSfx => "event:/sfx/ambience/act2_ambience";

	protected override int NumberOfWeakEncounters => 2;

	protected override int BaseNumberOfRooms => 14;

	// 宝箱沿用第二幕原版的 Spine 与音效（原版资源，不在 AssetProfile 里重定向）。
	public override string ChestSpineResourcePath => "res://animations/backgrounds/treasure_room/chest_room_act_2_skel_data.tres";

	public override string ChestSpineSkinNameNormal => "act2";

	public override string ChestSpineSkinNameStroke => "act2_stroke";

	public override string ChestOpenSfx => "event:/sfx/ui/treasure/treasure_act2";

	// RitsuLib 只按幕过滤 Boss 发现顺序、不往里追加，Boss 要在这里自己列出；遭遇池里的 Boss 来自按幕注册。
	public override IEnumerable<EncounterModel> BossDiscoveryOrder => [ModelDb.Encounter<ReunionPatriotBoss>()];

	public override IEnumerable<AncientEventModel> AllAncients => Hive.AllAncients;

	// 事件池里复用的原版事件：蜂巢里与虫巢主题无关的三个（其余七个是虫子、菌类主题，放进冰封城市不搭）。
	// 本幕事件由 RitsuLib 按幕追加（ModEntry.ActEventTypes）。
	public override IEnumerable<EventModel> AllEvents =>
	[
		ModelDb.Event<MegaCrit.Sts2.Core.Models.Events.LostWisp>(),
		ModelDb.Event<MegaCrit.Sts2.Core.Models.Events.TheLanternKey>(),
		ModelDb.Event<MegaCrit.Sts2.Core.Models.Events.ColorfulPhilosophers>(),
	];

	// 遭遇池全部由 RitsuLib 按幕追加（ModEntry.ActEncounterTypes），原版的 AllBossEncounters 也从这里按房间类型筛出 Boss。
	public override IEnumerable<EncounterModel> GenerateAllEncounters() => [];

	public override IEnumerable<AncientEventModel> GetUnlockedAncients(UnlockState unlockState)
	{
		return Hive.GetUnlockedAncients(unlockState);
	}

	protected override void ApplyActDiscoveryOrderModifications(UnlockState unlockState)
	{
	}

	public override bool IsUnlocked(UnlockState unlockState)
	{
		return true;
	}

	public override MapPointTypeCounts GetMapPointTypes(Rng mapRng)
	{
		int restCount = mapRng.NextGaussianInt(6, 1, 6, 7);
		int unknownCount = MapPointTypeCounts.StandardRandomUnknownCount(mapRng) - 1;
		return new MapPointTypeCounts(unknownCount, restCount);
	}
}

using ActLikeIt2;
using ArknightsChernobog.Acts;
using ArknightsChernobog.Cards;
using ArknightsChernobog.Encounters;
using ArknightsChernobog.Events;
using ArknightsChernobog.Monsters;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Content;

namespace ArknightsChernobog;

[ModInitializer(nameof(Initialize))]
public static class ModEntry
{
	public const string ModId = "ArknightsChernobog";
	private const string HarmonyId = "Natsuki.ArknightsChernobog";

	private static bool _initialized;

	public static void Initialize()
	{
		if (_initialized)
		{
			return;
		}

		_initialized = true;
		RegisterContent();
		Audio.ReunionBossMusic.Install(new Harmony(HarmonyId));
		Powers.LeadershipNullDealerGuard.Install(new Harmony(HarmonyId));
		ChernobogSelfTest.InstallIfRequested(new Harmony(HarmonyId));
		Log.Info($"[{ModId}] Loaded; registered {nameof(ChernobogAct)} as an act-index {ChernobogAct.ActIndex} variant through RitsuLib and ActLikeIt2.");
	}

	// 以下三张表只往末尾追加、不重排不改名：类名经 RitsuLib 得出模型 id（ARKNIGHTS_CHERNOBOG_<类别>_<类名>），存档按 id 引用。

	/// <summary>本幕战斗池（弱怪、普通、精英、Boss），经 RitsuLib 按幕注册；原版的 AllBossEncounters 从中按房间类型筛出 Boss。</summary>
	internal static readonly Type[] ActEncounterTypes =
	[
		typeof(ReunionHoundPackWeak),
		typeof(ReunionHostStragglersWeak),
		typeof(ReunionParatrooperWeak),
		typeof(ReunionSpecOpsRemnantsWeak),
		typeof(ReunionHoundPackNormal),
		typeof(ReunionHostHerdNormal),
		typeof(ReunionRagingHostNormal),
		typeof(ReunionSpecOpsTeamNormal),
		typeof(ReunionAssaultSquadNormal),
		typeof(ReunionSniperNestNormal),
		typeof(ReunionMortarPositionNormal),
		typeof(ReunionInfectionNormal),
		typeof(ReunionParatroopersNormal),
		typeof(ReunionReinforcementsNormal),
		typeof(ReunionShieldOfInfectedElite),
		typeof(ReunionVerticalStrikeElite),
		typeof(ReunionSarkazRitualElite),
		typeof(ReunionRagingHostLeaderElite),
		typeof(ReunionPatriotBoss),
	];

	/// <summary>本幕事件（只在本幕出现）。</summary>
	internal static readonly Type[] ActEventTypes =
	[
		typeof(SupplyCache),
		typeof(GuerrillaCampfire),
		typeof(OriginiumVein),
		typeof(CoreValve),
		typeof(RhodesAirdrop),
		typeof(BlackSnakeWhisper),
	];

	/// <summary>全部整合运动敌人与本模组能力：按类型全集注册（顺序不影响 id）。</summary>
	internal static IEnumerable<Type> ModelTypes<TBase>() =>
		typeof(ModEntry).Assembly.GetTypes()
			.Where(type => !type.IsAbstract && typeof(TBase).IsAssignableFrom(type))
			.OrderBy(type => type.FullName, StringComparer.Ordinal);

	/// <summary>
	/// 幕、战斗池、事件、怪物、能力、诅咒全部经 RitsuLib 注册（写法照集成战略事件），必须在模型库初始化之前调用（模组初始化阶段）。
	/// 只供控制台测试或由事件发起的遭遇战（ReunionTestEncounters、事件战斗）不注册，保留原版按类名得出的 id。
	/// </summary>
	private static void RegisterContent()
	{
		ModContentRegistry registry = ModContentRegistry.For(ModId);
		registry.RegisterAct<ChernobogAct>();
		// 第二幕选幕界面的候选（ActNumber 从 1 起算）；ActLikeIt2 在模型库初始化后自行解析模型，这里只需在初始化阶段登记。
		ActRegistry.Register<ChernobogAct>(ChernobogAct.ActIndex + 1);
		foreach (Type encounterType in ActEncounterTypes)
		{
			registry.RegisterActEncounter(typeof(ChernobogAct), encounterType);
		}

		foreach (Type eventType in ActEventTypes)
		{
			registry.RegisterActEvent(typeof(ChernobogAct), eventType);
		}

		foreach (Type monsterType in ModelTypes<ReunionMonster>())
		{
			registry.RegisterMonster(monsterType);
		}

		foreach (Type powerType in ModelTypes<PowerModel>())
		{
			registry.RegisterPower(powerType);
		}

		registry.RegisterCard(typeof(CurseCardPool), typeof(Oripathy));
	}
}

using ArknightsChernobog.Cards;
using ArknightsChernobog.Encounters;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace ArknightsChernobog.Events;

// 本幕事件。强度对照原版蜂巢（第二幕）事件：
// - 灵魂嫁接：回 25 血 + 一张负面牌 / 掉 10 血升级 1 张；人形坑洞田：删 2 张 + 诅咒；禅织者：50/125/250 金换 2 张特殊牌/删 1/删 2；
// - 迷失磷火：遗物 + 腐朽 / 约 60 金；巨型花：掉 5/11/18 血换 35/75/135 金；灯笼钥匙：100 金 / 打一场战斗换一张特殊牌。
// 选项里的数字写死在本地化（events.json）里，改这里的常量要一起改文本。

/// <summary>冻土补给箱：整合运动遗弃的物资。口粮回血 / 弹药拿攻击牌 / 撬开铁箱引来猎犬，打赢多拿金币。</summary>
public sealed class SupplyCache : ReunionEventModel
{
	private const int RationsHeal = 18;
	private const int AmbushGold = 75;

	protected override string PortraitFile => "reunion_event_supply_cache.png";

	// 撬箱要进事件战斗，原版 EnterCombatWithoutExitingEvent 只允许共享事件（联机时全队投票）。
	public override bool IsShared => true;

	protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
	[
		Choice(Rations, "RATIONS"),
		Choice(Ammo, "AMMO"),
		Choice(Pry, "PRY"),
	];

	private async Task Rations()
	{
		await Heal(RationsHeal);
		Finish("RATIONS");
	}

	private async Task Ammo()
	{
		await OfferCardReward(3, CardRarityOddsType.RegularEncounter, card => card.Type == CardType.Attack);
		Finish("AMMO");
	}

	private Task Pry()
	{
		return EnterEventCombat<ReunionSupplyAmbushEvent>([new GoldReward(AmbushGold, OwnerOrThrow)]);
	}
}

/// <summary>游击队的篝火：爱国者旧部的营地。分食换最大生命 / 听老兵讲过去升级一张牌 / 悄悄离开。</summary>
public sealed class GuerrillaCampfire : ReunionEventModel
{
	private const int ShareCost = 50;
	private const int ShareMaxHp = 8;

	protected override string PortraitFile => "reunion_event_guerrilla_campfire.png";

	protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
	[
		GoldChoice(ShareCost, Share, "SHARE", "SHARE_LOCKED"),
		Choice(Listen, "LISTEN"),
		Choice(Leave, "LEAVE"),
	];

	private async Task Share()
	{
		await SpendGold(ShareCost);
		await GainMaxHp(ShareMaxHp);
		Finish("SHARE");
	}

	private async Task Listen()
	{
		await UpgradeCards(1);
		Finish("LISTEN");
	}

	private Task Leave()
	{
		Finish("LEAVE");
		return Task.CompletedTask;
	}
}

/// <summary>源石矿脉：裸露的黑色结晶。触碰拿稀有牌但染上矿石病 / 开采掉血换金币 / 离开。</summary>
public sealed class OriginiumVein : ReunionEventModel
{
	private const int MineHpLoss = 9;
	private const int MineGold = 85;

	protected override string PortraitFile => "reunion_event_originium_vein.png";

	protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
	[
		CardPreviewChoice<Oripathy>(Touch, "TOUCH"),
		HpChoice(MineHpLoss, Mine, "MINE", "MINE_LOCKED"),
		Choice(Leave, "LEAVE"),
	];

	private async Task Touch()
	{
		await OfferCardReward(3, CardRarityOddsType.Uniform, card => card.Rarity == CardRarity.Rare);
		await AddCurse<Oripathy>();
		Finish("TOUCH");
	}

	private async Task Mine()
	{
		await LoseHp(MineHpLoss);
		await GainGold(MineGold);
		Finish("MINE");
	}

	private Task Leave()
	{
		Finish("LEAVE");
		return Task.CompletedTask;
	}
}

/// <summary>核心城动力阀：移动城市的引擎舱。手动泄压掉血删一张牌 / 让它超载变化两张牌 / 离开。</summary>
public sealed class CoreValve : ReunionEventModel
{
	private const int VentHpLoss = 7;
	private const int OverloadCount = 2;

	protected override string PortraitFile => "reunion_event_core_valve.png";

	protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
	[
		HasRemovableCards(1) ? HpChoice(VentHpLoss, Vent, "VENT", "VENT_LOCKED") : LockedChoice("VENT_LOCKED"),
		HasTransformableCards(OverloadCount) ? Choice(Overload, "OVERLOAD") : LockedChoice("OVERLOAD_LOCKED"),
		Choice(Leave, "LEAVE"),
	];

	private async Task Vent()
	{
		await LoseHp(VentHpLoss);
		await RemoveCards(1);
		Finish("VENT");
	}

	private async Task Overload()
	{
		await TransformCards(OverloadCount);
		Finish("OVERLOAD");
	}

	private Task Leave()
	{
		Finish("LEAVE");
		return Task.CompletedTask;
	}
}

/// <summary>罗德岛空投：友方无人机投下的补给舱。医疗箱按最大生命百分比回血 / 药剂箱拿两瓶随机药水。</summary>
public sealed class RhodesAirdrop : ReunionEventModel
{
	private const int MedkitPercent = 30;
	private const int PotionCount = 2;

	protected override string PortraitFile => "reunion_event_rhodes_airdrop.png";

	protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
	[
		Choice(Medkit, "MEDKIT"),
		Choice(Supplies, "SUPPLIES"),
	];

	private async Task Medkit()
	{
		await Heal((int)Math.Round(OwnerOrThrow.Creature.MaxHp * MedkitPercent / 100m));
		Finish("MEDKIT");
	}

	private async Task Supplies()
	{
		await OfferRandomPotions(PotionCount);
		Finish("SUPPLIES");
	}
}

/// <summary>黑蛇的低语：暗面交易。以最大生命换随机遗物 / 收下金币但染上矿石病 / 拒绝。</summary>
public sealed class BlackSnakeWhisper : ReunionEventModel
{
	private const int BloodMaxHpLoss = 6;
	private const int GiftGold = 150;

	protected override string PortraitFile => "reunion_event_black_snake.png";

	protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
	[
		OwnerOrThrow.Creature.MaxHp > BloodMaxHpLoss ? Choice(Blood, "BLOOD") : LockedChoice("BLOOD_LOCKED"),
		CardPreviewChoice<Oripathy>(Gift, "GIFT"),
		Choice(Refuse, "REFUSE"),
	];

	private async Task Blood()
	{
		await LoseMaxHp(BloodMaxHpLoss);
		await ObtainRandomRelic();
		Finish("BLOOD");
	}

	private async Task Gift()
	{
		await GainGold(GiftGold);
		await AddCurse<Oripathy>();
		Finish("GIFT");
	}

	private Task Refuse()
	{
		Finish("REFUSE");
		return Task.CompletedTask;
	}
}

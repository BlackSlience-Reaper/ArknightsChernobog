using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace ArknightsChernobog.Cards;

/// <summary>
/// 本幕诅咒“矿石病”：不能打出；回合结束时若在手牌中，受到等同于当前回合数的伤害，不设上限。
/// 照原版腐朽（Decay）的回合末在手效果写，区别是伤害随战斗拖长一直上升，表现源石病的蔓延；伤害能被格挡（同腐朽的 Unpowered|Move）。
/// 经 RitsuLib 注册进原版诅咒卡池（ModEntry）。
/// </summary>
public sealed class Oripathy : CardModel
{
	public const string PortraitAssetPath = ChernobogAssets.CardPortraitRoot + "oripathy.png";

	public Oripathy()
		: base(-1, CardType.Curse, CardRarity.Curse, TargetType.None)
	{
	}

	public override CardPoolModel Pool => ModelDb.CardPool<CurseCardPool>();

	public override int MaxUpgradeLevel => 0;

	public override string PortraitPath => PortraitAssetPath;

	public override IEnumerable<string> AllPortraitPaths => [PortraitPath];

	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];

	public override bool HasTurnEndInHandEffect => true;

	protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
	{
		int damage = Math.Max(1, CombatState?.RoundNumber ?? 1);
		await CreatureCmd.Damage(choiceContext, Owner.Creature, damage, ValueProp.Unpowered | ValueProp.Move, this, null);
	}
}

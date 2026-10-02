using ArknightsChernobog.Monsters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Scaffolding.Content;

namespace ArknightsChernobog.Powers;

/// <summary>
/// 悬浮：法术大师A1（无人机）的飞行。受到的攻击伤害减半；每吃到一次未被格挡的攻击伤害减 1 层，
/// 归零时坠落并被击晕，坠落表现和之后的起飞由持有者（<see cref="IReunionHovering"/>）负责。
/// 对应 PRTS 里飞行单位只能被远程攻击到。
/// 仿原版“振翅”（FlutterPower），但原版实现把持有者强转成窃贼跳蚤，不能直接给别的怪用。
/// 只减免 IsPoweredAttack 的伤害，中毒、能力和技能牌的直接伤害不受影响，这正是鼓励的解法。
/// </summary>
public sealed class ReunionHoverPower : ModPowerTemplate
{
	public override PowerAssetProfile AssetProfile => ChernobogAssets.PowerIcon("reunion_hover_power");

	private const string DamageDecreaseKey = "DamageDecrease";

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override bool ShouldScaleInMultiplayer => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(DamageDecreaseKey, 50m)];

	public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
	{
		if (target != Owner || !props.IsPoweredAttack())
		{
			return 1m;
		}

		return DynamicVars[DamageDecreaseKey].BaseValue / 100m;
	}

	public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (target != Owner || result.UnblockedDamage == 0 || !props.IsPoweredAttack())
		{
			return;
		}

		await PowerCmd.Decrement(this);
		if (Amount > 0 || Owner.IsDead)
		{
			return;
		}

		Flash();
		if (Owner.Monster is IReunionHovering hovering)
		{
			await hovering.Crash();
		}
		else
		{
			await CreatureCmd.Stun(Owner);
		}
	}
}

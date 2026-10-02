using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Scaffolding.Content;

namespace ArknightsChernobog.Powers;

/// <summary>
/// 掩护：游击队盾卫在场时，每个玩家回合开始给其他所有盟友层数点格挡（PRTS：使自身容易受到攻击，即优先攻击目标）。
/// 写法照原版“盾墙”（RampartPower，活体盾牌给高塔炮手格挡），但原版只认高塔炮手，这里给持有者以外的全部存活盟友。
/// 能力随持有者死亡移除，所以盾卫倒下后掩护自然停止；逼玩家先拆盾卫。
/// 格挡是 Unpowered，不吃脆弱/敏捷；在玩家回合开始时给，正好撑过玩家回合，敌方回合开始时照常清掉。
/// </summary>
public sealed class ReunionGuardCoverPower : ModPowerTemplate
{
	public override PowerAssetProfile AssetProfile => ChernobogAssets.PowerIcon("reunion_guard_cover_power");

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override bool ShouldScaleInMultiplayer => true;

	public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		// 与原版一致：玩家额外回合不再给一次。
		if (side != CombatSide.Player || CombatManager.Instance.PlayersTakingExtraTurn.Count > 0 || Owner.IsDead)
		{
			return;
		}

		List<Creature> allies = combatState.GetTeammatesOf(Owner).Where(c => c != Owner && c.IsAlive).ToList();
		if (allies.Count == 0)
		{
			return;
		}

		Flash();
		foreach (Creature ally in allies)
		{
			await CreatureCmd.GainBlock(ally, Amount, ValueProp.Unpowered, null);
		}
	}
}

using ArknightsChernobog.Monsters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using STS2RitsuLib.Scaffolding.Content;

namespace ArknightsChernobog.Powers;

/// <summary>
/// 残存：普通宿主（<see cref="ReunionHostMonster"/>）被击倒时，只要场上还有其他宿主活着，就留在战场上倒地，两回合后以层数点生命重新站起
/// （PRTS：梅菲斯特让宿主不停站起来）。逼玩家同一时间窗口里把牧群一起收掉。
/// 做法照原版“接续”（ReattachPower，十足虫节段）：
/// - 击倒时 ShouldCreatureBeRemovedFromCombatAfterDeath 返回 false，生物留在战斗里继续走招式状态机；
///   持有者通过 <see cref="IReunionRevivable.DownedState"/> 切到“倒地 → 重新站起”两步，站起那招调用 <see cref="Revive"/>。
/// - 原版 CreatureCmd.Heal 对已死亡生物会复活并触发 NCreature.StartReviveAnim（动画机要有 Revive 触发器）。
/// - 倒地期间不可被选中、不会被命中；本能力保留，其他能力照常在死亡时移除，站起后由招式重新挂回。
/// 判断“其他宿主”看同伴身上有没有永续再生或残存；倒地中的同伴已死亡，不算。
/// 胜负不用特殊处理：原版只在还有存活的主要敌人时继续战斗，倒地的宿主是死亡状态，全倒即获胜。
/// </summary>
public sealed class ReunionHostRemnantPower : ModPowerTemplate
{
	public override PowerAssetProfile AssetProfile => ChernobogAssets.PowerIcon("reunion_host_remnant_power");

	private sealed class Data
	{
		public bool IsDowned;
	}

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Single;

	public override bool ShouldScaleInMultiplayer => true;

	public bool IsDowned => GetInternalData<Data>().IsDowned;

	protected override object InitInternalData()
	{
		return new Data();
	}

	private bool OtherHostsAlive()
	{
		return Owner.CombatState?.GetTeammatesOf(Owner).Any(c => c != Owner && c.IsAlive
			&& (c.HasPower<ReunionPermanentRegenPower>() || c.HasPower<ReunionHostRemnantPower>())) ?? false;
	}

	// 击杀流程里先问这个、再调 AfterDeath，两处的判断必须一致：此时 IsDowned 还是 false，都看“其他宿主是否活着”。
	// 倒地后原版在它每次出招后还会再问一次，那时已经决定要站起，保持 false。
	public override bool ShouldCreatureBeRemovedFromCombatAfterDeath(Creature creature)
	{
		return creature != Owner || (!IsDowned && !OtherHostsAlive());
	}

	public override bool ShouldOwnerDeathTriggerFatal()
	{
		return !IsDowned && !OtherHostsAlive();
	}

	public override bool ShouldPowerBeRemovedAfterOwnerDeath()
	{
		return !IsDowned;
	}

	public override bool ShouldAllowHitting(Creature creature)
	{
		return creature != Owner || !IsDowned;
	}

	public override Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
	{
		if (wasRemovalPrevented || creature != Owner || !OtherHostsAlive() || Owner.Monster is not IReunionRevivable revivable)
		{
			return Task.CompletedTask;
		}

		GetInternalData<Data>().IsDowned = true;
		Owner.Monster.SetMoveImmediate(revivable.DownedState, forceTransition: true);
		NCombatRoom.Instance?.SetCreatureIsInteractable(Owner, on: false);
		return Task.CompletedTask;
	}

	/// <summary>由持有者“重新站起”招式调用：以层数点生命复活。一旦倒地就一定会站起，不再检查其他宿主。</summary>
	public async Task Revive()
	{
		GetInternalData<Data>().IsDowned = false;
		NCombatRoom.Instance?.SetCreatureIsInteractable(Owner, on: true);
		Flash();
		await CreatureCmd.Heal(Owner, Amount);
	}
}

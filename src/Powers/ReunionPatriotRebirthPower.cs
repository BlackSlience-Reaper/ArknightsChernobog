using ArknightsChernobog.Monsters;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using STS2RitsuLib.Scaffolding.Content;

namespace ArknightsChernobog.Powers;

/// <summary>
/// 不屈：爱国者一阶段生命归零时不死，进入重生（PRTS：生命值降为 0 后进入重生阶段，之后切换至毁灭姿态）。
/// 做法照原版“适者生存”（AdaptablePower，实验体换形态）：
/// - 死亡时 ShouldCreatureBeRemovedFromCombatAfterDeath 返回 false，生物留在战斗里；持有者（<see cref="IReunionRebirth"/>）切到重生招式。
/// - 重生期间不可选中、不会被命中；ShouldStopCombatFromEnding 让护卫全灭时战斗也不结束。
/// - 重生招式调用 <see cref="Revive"/> 并在换完形态后移除本能力，二阶段再倒下就是真正的死亡。
/// 原版适者生存把持有者强转成实验体，不能直接复用。
/// </summary>
public sealed class ReunionPatriotRebirthPower : ModPowerTemplate
{
	public override PowerAssetProfile AssetProfile => ChernobogAssets.PowerIcon("reunion_patriot_rebirth_power");

	private sealed class Data
	{
		public bool IsReviving;
	}

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Single;

	public bool IsReviving => GetInternalData<Data>().IsReviving;

	protected override object InitInternalData()
	{
		return new Data();
	}

	public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
	{
		if (wasRemovalPrevented || creature != Owner || Owner.Monster is not IReunionRebirth rebirth)
		{
			return;
		}

		GetInternalData<Data>().IsReviving = true;
		NCombatRoom.Instance?.SetCreatureIsInteractable(Owner, on: false);
		await rebirth.BeginRebirth();
	}

	/// <summary>由持有者的重生招式调用：恢复可选中。回血与换形态由招式自己做。</summary>
	public void Revive()
	{
		GetInternalData<Data>().IsReviving = false;
		NCombatRoom.Instance?.SetCreatureIsInteractable(Owner, on: true);
	}

	public override bool ShouldAllowHitting(Creature creature)
	{
		return creature != Owner || !IsReviving;
	}

	public override bool ShouldStopCombatFromEnding()
	{
		return true;
	}

	public override bool ShouldCreatureBeRemovedFromCombatAfterDeath(Creature creature)
	{
		return creature != Owner;
	}

	public override bool ShouldPowerBeRemovedAfterOwnerDeath()
	{
		return false;
	}

	// 一阶段倒下不是真正的击杀：不触发“斩杀”类卡牌的致命奖励。
	public override bool ShouldOwnerDeathTriggerFatal()
	{
		return false;
	}
}

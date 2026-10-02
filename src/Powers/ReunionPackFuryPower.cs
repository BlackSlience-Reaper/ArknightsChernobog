using ArknightsChernobog.Monsters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Scaffolding.Content;

namespace ArknightsChernobog.Powers;

/// <summary>
/// 狼群：猎犬与猎犬pro 的同伴猎犬倒下时，持有者获得层数点力量。
/// PRTS：猎犬pro 是“攻击欲望更强的高级战犬”。逐个点杀会把最后一只喂大，奖励同回合收掉多只或先想好击杀顺序。
/// 写法照原版“蟹之怒”（CrabRagePower，监听同伴 AfterDeath），但不在触发后移除，每倒下一只都会再触发。
/// AfterDeath 在原版移除死者能力之前分发，所以按死者的模型类型判断是不是猎犬，不看它身上的能力。
/// </summary>
public sealed class ReunionPackFuryPower : ModPowerTemplate
{
	public override PowerAssetProfile AssetProfile => ChernobogAssets.PowerIcon("reunion_pack_fury_power");

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<StrengthPower>()];

	public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
	{
		if (wasRemovalPrevented || creature == Owner || creature.Side != Owner.Side || Owner.IsDead
			|| creature.Monster is not (ReunionGuerrillaHound or ReunionGuerrillaHoundPro))
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, Amount, Owner, null);
	}
}

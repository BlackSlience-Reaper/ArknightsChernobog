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
/// 旧部之誓：爱国者的护卫（游击队“盾”）倒下时，爱国者获得层数点力量。杀护卫能停掉领袖气质的加伤，代价是把爱国者激怒。
/// 写法照原版“蟹之怒”（CrabRagePower，监听同伴 AfterDeath），但每倒下一名都触发、不移除。
/// 爱国者自己一阶段“倒下”（转阶段）时死者是持有者，不触发。
/// </summary>
public sealed class ReunionPatriotOathPower : ModPowerTemplate
{
	public override PowerAssetProfile AssetProfile => ChernobogAssets.PowerIcon("reunion_patriot_oath_power");

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<StrengthPower>()];

	public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
	{
		if (wasRemovalPrevented || creature == Owner || creature.Side != Owner.Side || Owner.IsDead)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, Amount, Owner, null);
	}
}

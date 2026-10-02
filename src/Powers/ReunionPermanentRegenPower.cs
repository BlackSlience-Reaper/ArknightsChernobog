using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace ArknightsChernobog.Powers;

/// <summary>
/// 永续再生：持有者回合结束时回复层数点生命，层数不衰减（原版“再生”会每回合减 1 层）。
/// 给宿主一族表现 PRTS 的“能快速自然恢复生命”：拖得越久回得越多，逼玩家集中火力速杀。
/// 经 RitsuLib 注册，模型 id 为 ARKNIGHTS_CHERNOBOG_POWER_REUNION_PERMANENT_REGEN_POWER；本地化在 powers 表，图标见 AssetProfile。
/// </summary>
public sealed class ReunionPermanentRegenPower : ModPowerTemplate
{
	public override PowerAssetProfile AssetProfile => ChernobogAssets.PowerIcon("reunion_permanent_regen_power");

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override bool ShouldScaleInMultiplayer => true;

	// 结算时机与原版 RegenPower 相同（回合结束前），只是不调用 PowerCmd.Decrement。
	public override async Task BeforeSideTurnEndEarly(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		if (participants.Contains(Owner) && !Owner.IsDead)
		{
			Flash();
			await CreatureCmd.Heal(Owner, Amount);
		}
	}
}

using MegaCrit.Sts2.Core.Combat;
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
/// 毁灭姿态：爱国者二阶段的被动（PRTS：重生与毁灭姿态持续对周围造成真实伤害）。
/// 每个玩家回合开始时给每名玩家层数点原版“瓦解”。瓦解不衰减，于是每回合末的伤害 2、4、6… 越叠越高，逼玩家抢血。
/// 瓦解在玩家回合结束时结算、能被格挡吸收，所以防御仍然有用，只是回报越来越低。
/// 时点照原版“盾墙”（RampartPower）：玩家额外回合不再叠。持有者死亡时原版移除本能力，已经叠上的瓦解留在玩家身上直到战斗结束。
/// </summary>
public sealed class ReunionPatriotWrathPower : ModPowerTemplate
{
	public override PowerAssetProfile AssetProfile => ChernobogAssets.PowerIcon("reunion_patriot_wrath_power");

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<DisintegrationPower>()];

	public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		if (side != CombatSide.Player || CombatManager.Instance.PlayersTakingExtraTurn.Count > 0 || Owner.IsDead)
		{
			return;
		}

		List<Creature> players = combatState.PlayerCreatures.Where(c => c.IsAlive).ToList();
		if (players.Count == 0)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<DisintegrationPower>(new ThrowingPlayerChoiceContext(), players, Amount, Owner, null);
	}
}

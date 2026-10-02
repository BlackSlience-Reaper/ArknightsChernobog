using ArknightsChernobog.Powers;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace ArknightsChernobog.Monsters;

/// <summary>
/// 普通宿主（梅菲斯特牧群里带永续再生的一族）的公共部分：开场挂永续再生和残存（<see cref="ReunionHostRemnantPower"/>），
/// 被击倒时只要还有其他宿主活着，就倒地一回合、下一回合重新站起并挂回永续再生，然后从 <c>resume</c> 招式接着打。
/// 狂暴宿主不在这一族：它们总是单独出场，残存永远不会触发，用的是瓦解而不是再生。
/// 骨骼都没有复活动画，站起时直接切回 Idle。
/// </summary>
public abstract class ReunionHostMonster : ReunionMonster, IReunionRevivable
{
	public const string DownedMoveId = "DOWNED_MOVE";
	public const string ReviveMoveId = "REVIVE_MOVE";

	private const string ReviveTrigger = "Revive";

	private MoveState? _downedState;

	/// <summary>永续再生层数。</summary>
	protected abstract int HostRegen { get; }

	/// <summary>残存站起时的生命（约为最大生命的四成）。</summary>
	protected abstract int ReviveHp { get; }

	public MoveState DownedState
	{
		get => _downedState!;
		private set
		{
			AssertMutable();
			_downedState = value;
		}
	}

	public override async Task AfterAddedToRoom()
	{
		await base.AfterAddedToRoom();
		await ApplyHostRegen(HostRegen);
		await ApplyPower<ReunionHostRemnantPower>([Creature], ReviveHp);
	}

	/// <summary>子类建好正常招式循环后用它代替 <see cref="ReunionMonster.Machine"/>：补上“倒地 → 重新站起 → <paramref name="resume"/>”。</summary>
	protected MonsterMoveStateMachine HostMachine(IReadOnlyList<MonsterState> cycle, MonsterState initialState, MoveState resume)
	{
		return Machine([.. cycle, .. DownedChain(resume)], initialState);
	}

	/// <summary>随机出招的宿主（见 <see cref="ReunionMonster.RandomMachine"/>）：站起来后回到随机分支，照常不连用同一招。</summary>
	protected MonsterMoveStateMachine HostRandomMachine(string branchId, IReadOnlyList<MoveState> moves)
	{
		RandomBranchState loop = RandomLoop(branchId, moves);
		return Build([.. moves, loop, .. DownedChain(loop)], RandomOpening(moves));
	}

	private MoveState[] DownedChain(MonsterState resume)
	{
		DownedState = new MoveState(DownedMoveId, DownedMove);
		MoveState revive = new(ReviveMoveId, ReviveMove, new HealIntent()) { MustPerformOnceBeforeTransitioning = true };
		DownedState.FollowUpState = revive;
		revive.FollowUpState = resume;
		return [DownedState, revive];
	}

	private static Task DownedMove(IReadOnlyList<Creature> targets)
	{
		return Task.CompletedTask;
	}

	private async Task ReviveMove(IReadOnlyList<Creature> targets)
	{
		if (!CombatState.IsLiveCombat() || Creature.GetPower<ReunionHostRemnantPower>() is not { IsDowned: true } remnant)
		{
			return;
		}

		SfxCmd.Play(BuffSfx);
		// PRTS 的 Die 会把插槽淡出/隐藏，Idle 不一定把这些插槽都打上关键帧；先回到初始姿势，否则站起来可能是透明的。
		Creature.GetCreatureNode()?.Visuals.SpineBody?.GetSkeleton()?.SetSlotsToSetupPose();
		await remnant.Revive();
		await ApplyHostRegen(HostRegen);
	}

	/// <summary>宿主的动画机：待机、攻击、死亡，外加残存站起用的 Revive 触发器（切回待机）。</summary>
	protected static CreatureAnimator BuildHostAnimator(MegaSprite controller)
	{
		return BuildAnimator(controller, "Idle", "Die",
			(CreatureAnimator.attackTrigger, "Attack"),
			(ReviveTrigger, "Idle"));
	}
}

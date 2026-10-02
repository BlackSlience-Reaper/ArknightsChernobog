using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace ArknightsChernobog.Monsters;

/// <summary>
/// 游击队传令兵（PRTS enemy_1080_sotidp），普通辅助。PRTS 定位：在场时强化敌军攻击力与防御力。
/// 动画：Idle / Attack（OnAttack 0.4s）/ Die；号令没有专门动画，只保持待机。
/// 招式与数值见 docs/战斗设计.md（初版）：开场获得原版“领袖气质”，其他盟友每段攻击额外造成伤害；
/// 原版在持有者死亡后移除其能力，所以传令兵一倒加成立刻消失，不需要自己清理。
/// </summary>
public sealed class ReunionGuerrillaHerald : ReunionMonster
{
	public const string RallyMoveId = "RALLY_MOVE";
	public const string StrikeMoveId = "STRIKE_MOVE";

	public override string SceneName => "reunion_guerrilla_herald";

	public override IReadOnlyList<string> RequiredAnimations => ["Idle", "Attack", "Die"];

	public override int MinInitialHp => ToughValue(62, 60);

	public override int MaxInitialHp => ToughValue(66, 64);

	public override DamageSfxType TakeDamageSfxType => DamageSfxType.Fur;

	private int LeadershipAmount => DeadlyValue(3, 2);

	private int RallyBlock => ToughValue(7, 6);

	private int StrikeDamage => DeadlyValue(7, 6);

	public override async Task AfterAddedToRoom()
	{
		await base.AfterAddedToRoom();
		await ApplyPower<LeadershipPower>([Creature], LeadershipAmount);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		MoveState strike = new(StrikeMoveId, StrikeMove, new SingleAttackIntent(StrikeDamage));
		MoveState rally = new(RallyMoveId, RallyMove, new DefendIntent());
		strike.FollowUpState = rally;
		rally.FollowUpState = strike;
		return Machine([strike, rally], strike);
	}

	private async Task RallyMove(IReadOnlyList<Creature> targets)
	{
		SfxCmd.Play(BuffSfx);
		await GiveBlock(LivingTeammates(), RallyBlock);
	}

	private async Task StrikeMove(IReadOnlyList<Creature> targets)
	{
		await DamageCmd.Attack(StrikeDamage)
			.FromMonster(this)
			.WithAttackerAnim(CreatureAnimator.attackTrigger, 0.4f)
			.WithHitFx(BluntVfx, MeleeHitSfx)
			.Execute(null);
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		return BuildAnimator(controller, "Idle", "Die", (CreatureAnimator.attackTrigger, "Attack"));
	}
}

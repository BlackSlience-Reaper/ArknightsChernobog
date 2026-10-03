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
/// 游击队狙击手组长（PRTS enemy_1079_sotisp_2），狙击手的头目版。PRTS 定位：受到强化时同时攻击两个目标。
/// skel 与本体逐字节相同，场景复用本体转换好的骨骼，只换组长自己的图集与贴图。
/// 动画：Idle / Attack（OnAttack 0.7s）/ Die；标定目标没有专门动画，只保持待机。
/// 招式与数值见 docs/战斗设计.md（初版）：狙击 → 标定目标（玩家易伤，全队都吃到加成）→ 双重狙击 → 循环。
/// </summary>
public sealed class ReunionGuerrillaSniperLeader : ReunionMonster
{
	public const string AimedShotMoveId = "AIMED_SHOT_MOVE";
	public const string MarkTargetMoveId = "MARK_TARGET_MOVE";
	public const string DoubleShotMoveId = "DOUBLE_SHOT_MOVE";

	private const int DoubleShotHits = 2;
	private const int MarkVulnerable = 1;
	private const int MarkBlock = 6;

	public override string SceneName => "reunion_guerrilla_sniper_leader";

	public override IReadOnlyList<string> RequiredAnimations => ["Idle", "Attack", "Die"];

	public override int MinInitialHp => ToughValue(53, 50);

	public override int MaxInitialHp => ToughValue(57, 54);

	public override DamageSfxType TakeDamageSfxType => DamageSfxType.Fur;

	private int ShotDamage => DeadlyValue(10, 9);

	private int DoubleShotDamage => DeadlyValue(5, 4);

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		MoveState shot = new(AimedShotMoveId, AimedShotMove, new SingleAttackIntent(ShotDamage));
		MoveState mark = new(MarkTargetMoveId, MarkTargetMove, new DebuffIntent(), new DefendIntent());
		MoveState doubleShot = new(DoubleShotMoveId, DoubleShotMove, new MultiAttackIntent(DoubleShotDamage, DoubleShotHits));
		shot.FollowUpState = mark;
		mark.FollowUpState = doubleShot;
		doubleShot.FollowUpState = shot;
		return Machine([shot, mark, doubleShot], shot);
	}

	private async Task AimedShotMove(IReadOnlyList<Creature> targets)
	{
		await DamageCmd.Attack(ShotDamage)
			.FromMonster(this)
			.WithAttackerAnim(CreatureAnimator.attackTrigger, 0.7f)
			.WithHitFx(BluntVfx, RangedHitSfx)
			.Execute(null);
	}

	private async Task MarkTargetMove(IReadOnlyList<Creature> targets)
	{
		SfxCmd.Play(BuffSfx);
		await ApplyPower<VulnerablePower>(targets, MarkVulnerable);
		await GainBlock(MarkBlock);
	}

	private async Task DoubleShotMove(IReadOnlyList<Creature> targets)
	{
		await DamageCmd.Attack(DoubleShotDamage).WithHitCount(DoubleShotHits).OnlyPlayAnimOnce()
			.FromMonster(this)
			.WithAttackerAnim(CreatureAnimator.attackTrigger, 0.7f)
			.WithHitFx(BluntVfx, RangedHitSfx)
			.Execute(null);
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		return BuildAnimator(controller, "Idle", "Die", (CreatureAnimator.attackTrigger, "Attack"));
	}
}

using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace ArknightsChernobog.Monsters;

/// <summary>
/// 游击队狙击手（PRTS enemy_1079_sotisp），普通远程弩手。PRTS 定位：受到强化时额外攻击一名目标。
/// 动画：Idle / Attack（OnAttack 0.7s）/ Die；瞄准没有专门动画，只保持待机。
/// 招式与数值见 docs/战斗设计.md（初版）：两发狙击后瞄准，力量逐轮叠加，不优先处理会越打越痛。
/// </summary>
public sealed class ReunionGuerrillaSniper : ReunionMonster
{
	public const string AimedShotMoveId = "AIMED_SHOT_MOVE";
	public const string TakeAimMoveId = "TAKE_AIM_MOVE";

	public override string SceneName => "reunion_guerrilla_sniper";

	public override IReadOnlyList<string> RequiredAnimations => ["Idle", "Attack", "Die"];

	public override int MinInitialHp => ToughValue(40, 38);

	public override int MaxInitialHp => ToughValue(44, 42);

	public override DamageSfxType TakeDamageSfxType => DamageSfxType.Fur;

	private int ShotDamage => DeadlyValue(10, 9);

	private int AimStrength => DeadlyValue(3, 2);

	private const int AimBlock = 6;

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		MoveState firstShot = new(AimedShotMoveId, AimedShotMove, new SingleAttackIntent(ShotDamage));
		MoveState secondShot = new(AimedShotMoveId + "_2", AimedShotMove, new SingleAttackIntent(ShotDamage));
		MoveState aim = new(TakeAimMoveId, TakeAimMove, new BuffIntent(), new DefendIntent());
		firstShot.FollowUpState = secondShot;
		secondShot.FollowUpState = aim;
		aim.FollowUpState = firstShot;
		return Machine([firstShot, secondShot, aim], firstShot);
	}

	private async Task AimedShotMove(IReadOnlyList<Creature> targets)
	{
		await DamageCmd.Attack(ShotDamage)
			.FromMonster(this)
			.WithAttackerAnim(CreatureAnimator.attackTrigger, 0.7f)
			.WithHitFx(BluntVfx, RangedHitSfx)
			.Execute(null);
	}

	private async Task TakeAimMove(IReadOnlyList<Creature> targets)
	{
		SfxCmd.Play(BuffSfx);
		await ApplyStrengthToSelf(AimStrength);
		await GainBlock(AimBlock);
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		return BuildAnimator(controller, "Idle", "Die", (CreatureAnimator.attackTrigger, "Attack"));
	}
}

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
/// 特战术师（PRTS enemy_1038_lunmag），整合运动特战单位（7-2）。PRTS 定位：法术作战人员，配备的护甲使他拥有一定防御。
/// 动画：Idle / Attack（OnAttack 0.7s）/ Die；Move 未用。
/// 招式与数值见 docs/战斗设计.md（初版）：开场原版“覆甲”当护甲；源石冲击（1 层虚弱）→ 腐蚀法术（1 层脆弱）→ 源石屏障（自身格挡）→ 循环。
/// </summary>
public sealed class ReunionSpecOpsCaster : ReunionMonster
{
	public const string ArtsBlastMoveId = "ARTS_BLAST_MOVE";
	public const string CorrosionMoveId = "CORROSION_MOVE";
	public const string ArtsBarrierMoveId = "ARTS_BARRIER_MOVE";

	private const int BlastWeak = 1;
	private const int CorrosionFrail = 1;

	public override string SceneName => "reunion_spec_ops_caster";

	public override IReadOnlyList<string> RequiredAnimations => ["Idle", "Attack", "Die"];

	public override int MinInitialHp => ToughValue(53, 50);

	public override int MaxInitialHp => ToughValue(57, 54);

	public override DamageSfxType TakeDamageSfxType => DamageSfxType.Magic;

	private int PlatingAmount => ToughValue(5, 4);

	private int BlastDamage => DeadlyValue(9, 8);

	private int CorrosionDamage => DeadlyValue(7, 6);

	private int BarrierBlock => ToughValue(14, 12);

	public override async Task AfterAddedToRoom()
	{
		await base.AfterAddedToRoom();
		await ApplyPower<PlatingPower>([Creature], PlatingAmount);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		MoveState blast = new(ArtsBlastMoveId, ArtsBlastMove, new SingleAttackIntent(BlastDamage), new DebuffIntent());
		MoveState corrosion = new(CorrosionMoveId, CorrosionMove, new SingleAttackIntent(CorrosionDamage), new DebuffIntent());
		MoveState barrier = new(ArtsBarrierMoveId, ArtsBarrierMove, new DefendIntent());
		blast.FollowUpState = corrosion;
		corrosion.FollowUpState = barrier;
		barrier.FollowUpState = blast;
		return Machine([blast, corrosion, barrier], blast);
	}

	private async Task ArtsBlastMove(IReadOnlyList<Creature> targets)
	{
		await DamageCmd.Attack(BlastDamage)
			.FromMonster(this)
			.WithAttackerAnim(CreatureAnimator.attackTrigger, 0.7f)
			.WithHitFx(ArtsVfx, ArtsHitSfx)
			.Execute(null);
		await ApplyPower<WeakPower>(targets, BlastWeak);
	}

	private async Task ArtsBarrierMove(IReadOnlyList<Creature> targets)
	{
		SfxCmd.Play(BuffSfx);
		await GainBlock(BarrierBlock);
	}

	private async Task CorrosionMove(IReadOnlyList<Creature> targets)
	{
		await DamageCmd.Attack(CorrosionDamage)
			.FromMonster(this)
			.WithAttackerAnim(CreatureAnimator.attackTrigger, 0.7f)
			.WithHitFx(ArtsVfx, ArtsHitSfx)
			.Execute(null);
		await ApplyPower<FrailPower>(targets, CorrosionFrail);
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		return BuildAnimator(controller, "Idle", "Die", (CreatureAnimator.attackTrigger, "Attack"));
	}
}

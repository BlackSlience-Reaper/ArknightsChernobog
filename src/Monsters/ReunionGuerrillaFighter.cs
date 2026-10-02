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
/// 游击队战士（PRTS enemy_1078_sotisc），普通近战。PRTS 定位：受到强化时加速，会“全力冲锋”。
/// 动画：Idle / Attack（OnAttack 0.4s）/ Die；Move 未用。
/// 招式与数值见 docs/战斗设计.md：劈砍 → 全力冲锋（自身力量）→ 压制（虚弱）。出现在游击队突击组（组长督战）与增援信号（传令兵组长召来的增援）。
/// </summary>
public sealed class ReunionGuerrillaFighter : ReunionMonster
{
	public const string SlashMoveId = "SLASH_MOVE";
	public const string ChargeMoveId = "CHARGE_MOVE";
	public const string SuppressMoveId = "SUPPRESS_MOVE";

	public override string SceneName => "reunion_guerrilla_fighter";

	public override IReadOnlyList<string> RequiredAnimations => ["Idle", "Attack", "Die"];

	public override int MinInitialHp => ToughValue(42, 40);

	public override int MaxInitialHp => ToughValue(46, 44);

	public override DamageSfxType TakeDamageSfxType => DamageSfxType.Fur;

	private int SlashDamage => DeadlyValue(10, 9);

	private int ChargeDamage => DeadlyValue(7, 6);

	private const int ChargeStrength = 2;

	private int SuppressDamage => DeadlyValue(6, 5);

	private const int SuppressWeak = 1;

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		MoveState slash = new(SlashMoveId, SlashMove, new SingleAttackIntent(SlashDamage));
		MoveState charge = new(ChargeMoveId, ChargeMove, new SingleAttackIntent(ChargeDamage), new BuffIntent());
		MoveState suppress = new(SuppressMoveId, SuppressMove, new SingleAttackIntent(SuppressDamage), new DebuffIntent());
		slash.FollowUpState = charge;
		charge.FollowUpState = suppress;
		suppress.FollowUpState = slash;
		return Machine([slash, charge, suppress], slash);
	}

	private async Task SlashMove(IReadOnlyList<Creature> targets)
	{
		await DamageCmd.Attack(SlashDamage)
			.FromMonster(this)
			.WithAttackerAnim(CreatureAnimator.attackTrigger, 0.4f)
			.WithHitFx(SlashVfx, MeleeHitSfx)
			.Execute(null);
	}

	private async Task ChargeMove(IReadOnlyList<Creature> targets)
	{
		await DamageCmd.Attack(ChargeDamage)
			.FromMonster(this)
			.WithAttackerAnim(CreatureAnimator.attackTrigger, 0.4f)
			.WithHitFx(BluntVfx, MeleeHitSfx)
			.Execute(null);
		await ApplyStrengthToSelf(ChargeStrength);
	}

	private async Task SuppressMove(IReadOnlyList<Creature> targets)
	{
		await DamageCmd.Attack(SuppressDamage)
			.FromMonster(this)
			.WithAttackerAnim(CreatureAnimator.attackTrigger, 0.4f)
			.WithHitFx(BluntVfx, MeleeHitSfx)
			.Execute(null);
		await ApplyPower<WeakPower>(targets, SuppressWeak);
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		return BuildAnimator(controller, "Idle", "Die", (CreatureAnimator.attackTrigger, "Attack"));
	}
}

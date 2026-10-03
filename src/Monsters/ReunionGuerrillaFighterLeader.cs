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
/// 游击队战士组长（PRTS enemy_1078_sotisc_2），战士的头目版。PRTS 定位：受到传令兵或爱国者强化时移动速度提升。
/// 与本体同一套骨骼和动画（附件网格随贴图重新打包，skel 单独转换）：Idle / Attack（OnAttack 0.4s）/ Die；Move 未用。
/// 招式与数值见 docs/战斗设计.md（初版）：劈砍 → 全力冲锋（自身力量）→ 督战：自己得格挡、玩家 1 层脆弱。
/// 督战原来给其他盟友力量，组长站最左先出手，会让两名战士本回合已亮出的意图涨伤害，所以改掉。
/// </summary>
public sealed class ReunionGuerrillaFighterLeader : ReunionMonster
{
	public const string SlashMoveId = "SLASH_MOVE";
	public const string ChargeMoveId = "CHARGE_MOVE";
	public const string CommandMoveId = "COMMAND_MOVE";

	private const int ChargeStrength = 2;
	private const int CommandBlock = 8;

	public override string SceneName => "reunion_guerrilla_fighter_leader";

	public override IReadOnlyList<string> RequiredAnimations => ["Idle", "Attack", "Die"];

	public override int MinInitialHp => ToughValue(53, 50);

	public override int MaxInitialHp => ToughValue(57, 54);

	public override DamageSfxType TakeDamageSfxType => DamageSfxType.Fur;

	private int SlashDamage => DeadlyValue(11, 10);

	private int ChargeDamage => DeadlyValue(8, 7);

	private const int CommandFrail = 1;

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		MoveState slash = new(SlashMoveId, SlashMove, new SingleAttackIntent(SlashDamage));
		MoveState charge = new(ChargeMoveId, ChargeMove, new SingleAttackIntent(ChargeDamage), new BuffIntent());
		MoveState command = new(CommandMoveId, CommandMove, new DefendIntent(), new DebuffIntent());
		slash.FollowUpState = charge;
		charge.FollowUpState = command;
		command.FollowUpState = slash;
		return Machine([slash, charge, command], slash);
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

	private async Task CommandMove(IReadOnlyList<Creature> targets)
	{
		SfxCmd.Play(BuffSfx);
		await GainBlock(CommandBlock);
		await ApplyPower<FrailPower>(targets, CommandFrail);
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		return BuildAnimator(controller, "Idle", "Die", (CreatureAnimator.attackTrigger, "Attack"));
	}
}

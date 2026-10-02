using ArknightsChernobog.Powers;
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
/// 游击队盾卫（PRTS enemy_1081_sotisd），重甲前排。PRTS 定位：使自身容易受到攻击（嘲讽）。
/// 动画：Idle / Attack（OnAttack 0.933s）/ Die；Move_Begin / Move_Loop / Move_End 未用。
/// 招式与数值见 docs/战斗设计.md：开场带原版“覆甲”和掩护（<see cref="ReunionGuardCoverPower"/>，
/// 在场时每个玩家回合开始给其他盟友格挡，逼玩家先打它）；推进 → 盾击（脆弱）→ 盾墙（自身格挡）。
/// 盾墙原来给全体盟友格挡，有了掩护后只给自己，免得盟友的格挡叠两份。
/// </summary>
public sealed class ReunionGuerrillaShieldGuard : ReunionMonster
{
	public const string ShieldWallMoveId = "SHIELD_WALL_MOVE";
	public const string ShieldBashMoveId = "SHIELD_BASH_MOVE";
	public const string PressForwardMoveId = "PRESS_FORWARD_MOVE";

	public override string SceneName => "reunion_guerrilla_shield_guard";

	public override IReadOnlyList<string> RequiredAnimations => ["Idle", "Attack", "Die"];

	public override int MinInitialHp => ToughValue(80, 76);

	public override int MaxInitialHp => ToughValue(84, 80);

	public override DamageSfxType TakeDamageSfxType => DamageSfxType.ArmorBig;

	private int PlatingAmount => ToughValue(10, 8);

	private int PressDamage => DeadlyValue(10, 9);

	private const int PressBlock = 8;

	private int BashDamage => DeadlyValue(16, 15);

	private const int BashFrail = 1;

	private int ShieldWallBlock => ToughValue(10, 8);

	private int CoverBlock => ToughValue(12, 10);

	public override async Task AfterAddedToRoom()
	{
		await base.AfterAddedToRoom();
		await ApplyPower<PlatingPower>([Creature], PlatingAmount);
		await ApplyPower<ReunionGuardCoverPower>([Creature], CoverBlock);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		MoveState press = new(PressForwardMoveId, PressForwardMove, new SingleAttackIntent(PressDamage), new DefendIntent());
		MoveState bash = new(ShieldBashMoveId, ShieldBashMove, new SingleAttackIntent(BashDamage), new DebuffIntent());
		MoveState shieldWall = new(ShieldWallMoveId, ShieldWallMove, new DefendIntent());
		press.FollowUpState = bash;
		bash.FollowUpState = shieldWall;
		shieldWall.FollowUpState = press;
		return Machine([press, bash, shieldWall], press);
	}

	private async Task PressForwardMove(IReadOnlyList<Creature> targets)
	{
		await DamageCmd.Attack(PressDamage)
			.FromMonster(this)
			.WithAttackerAnim(CreatureAnimator.attackTrigger, 0.9f)
			.WithHitFx(BluntVfx, MeleeHitSfx)
			.Execute(null);
		await GainBlock(PressBlock);
	}

	private async Task ShieldBashMove(IReadOnlyList<Creature> targets)
	{
		await DamageCmd.Attack(BashDamage)
			.FromMonster(this)
			.WithAttackerAnim(CreatureAnimator.attackTrigger, 0.9f)
			.WithHitFx(HeavyBluntVfx, HeavyHitSfx)
			.Execute(null);
		await ApplyPower<FrailPower>(targets, BashFrail);
	}

	private async Task ShieldWallMove(IReadOnlyList<Creature> targets)
	{
		SfxCmd.Play(BuffSfx);
		await GainBlock(ShieldWallBlock);
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		return BuildAnimator(controller, "Idle", "Die", (CreatureAnimator.attackTrigger, "Attack"));
	}
}

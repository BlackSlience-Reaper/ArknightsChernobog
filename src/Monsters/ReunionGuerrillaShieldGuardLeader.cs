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
/// 游击队盾卫组长（PRTS enemy_1081_sotisd_2），盾卫的头目版。PRTS 定位：使自身容易受到攻击，掩护敌军前进。
/// 与本体同一套骨骼和动画（附件网格随贴图重新打包，skel 单独转换）：Idle / Attack（OnAttack 0.933s）/ Die；Move_* 未用。
/// 招式与数值见 docs/战斗设计.md：开场原版“覆甲”“人工制品”和掩护（<see cref="ReunionGuardCoverPower"/>，比盾卫更厚）；
/// 推进 → 盾击（2 层脆弱）→ 盾墙（自身格挡）。掩护取代了原来盾墙给全体盟友的格挡和覆甲。
/// 覆甲和掩护的格挡会吃掉宿主“瓦解”的自损，不要和宿主编在一起。
/// </summary>
public sealed class ReunionGuerrillaShieldGuardLeader : ReunionMonster
{
	public const string PressForwardMoveId = "PRESS_FORWARD_MOVE";
	public const string ShieldBashMoveId = "SHIELD_BASH_MOVE";
	public const string ShieldWallMoveId = "SHIELD_WALL_MOVE";

	private const int PressBlock = 10;
	private const int BashFrail = 2;

	public override string SceneName => "reunion_guerrilla_shield_guard_leader";

	public override IReadOnlyList<string> RequiredAnimations => ["Idle", "Attack", "Die"];

	public override int MinInitialHp => ToughValue(108, 104);

	public override int MaxInitialHp => ToughValue(112, 108);

	public override DamageSfxType TakeDamageSfxType => DamageSfxType.ArmorBig;

	private int PlatingAmount => ToughValue(12, 10);

	private int ArtifactAmount => ToughValue(2, 1);

	private int PressDamage => DeadlyValue(11, 10);

	private int BashDamage => DeadlyValue(16, 15);

	private int ShieldWallBlock => ToughValue(14, 12);

	private int CoverBlock => ToughValue(16, 14);

	public override async Task AfterAddedToRoom()
	{
		await base.AfterAddedToRoom();
		await ApplyPower<PlatingPower>([Creature], PlatingAmount);
		await ApplyPower<ArtifactPower>([Creature], ArtifactAmount);
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

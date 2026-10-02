using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace ArknightsChernobog.Monsters;

/// <summary>
/// 游击队突袭战士组长（PRTS enemy_1083_sotiab_2），突袭战士的头目版。PRTS 定位：能从战场中降落，受到强化时攻击力极大幅提升。
/// 与本体同一套骨骼和动画（附件网格随贴图重新打包，skel 单独转换）：Idle / Die / Start（空降，见 <see cref="ReunionAirborneAnimation"/>）；
/// Attack、Move 未用。
/// 招式与数值见 docs/战斗设计.md：开场悬空（原版“翱翔”），降落突袭（触地重击，同时给其他盟友格挡、掩护后续突入）↔
/// 起飞（重新翱翔 + 力量 + 格挡）交替。
/// </summary>
public sealed class ReunionGuerrillaAssaulterLeader : ReunionMonster
{
	public const string DiveAssaultMoveId = "DIVE_ASSAULT_MOVE";
	public const string TakeOffMoveId = "TAKE_OFF_MOVE";

	private const int TakeOffBlock = 8;

	public override string SceneName => "reunion_guerrilla_assaulter_leader";

	public override IReadOnlyList<string> RequiredAnimations => ["Idle", ReunionAirborneAnimation.StartAnimation, "Die"];

	public override int MinInitialHp => ToughValue(100, 96);

	public override int MaxInitialHp => ToughValue(104, 100);

	public override DamageSfxType TakeDamageSfxType => DamageSfxType.Armor;

	private int DiveDamage => DeadlyValue(20, 18);

	private int DiveCoverBlock => ToughValue(10, 8);

	private int TakeOffStrength => DeadlyValue(4, 3);

	public override async Task AfterAddedToRoom()
	{
		await base.AfterAddedToRoom();
		await ApplyPower<SoarPower>([Creature], 1);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		MoveState dive = new(DiveAssaultMoveId, DiveAssaultMove, new SingleAttackIntent(DiveDamage), new DefendIntent());
		MoveState takeOff = new(TakeOffMoveId, TakeOffMove, new BuffIntent(), new DefendIntent());
		dive.FollowUpState = takeOff;
		takeOff.FollowUpState = dive;
		return Machine([dive, takeOff], dive);
	}

	private async Task DiveAssaultMove(IReadOnlyList<Creature> targets)
	{
		await PowerCmd.Remove<SoarPower>(Creature);
		await Cmd.Wait(ReunionAirborneAnimation.Land(Creature));
		await DamageCmd.Attack(DiveDamage)
			.FromMonster(this)
			.WithHitFx(HeavyBluntVfx, HeavyHitSfx)
			.Execute(null);
		await GiveBlock(OtherLivingTeammates(), DiveCoverBlock);
	}

	private async Task TakeOffMove(IReadOnlyList<Creature> targets)
	{
		SfxCmd.Play(BuffSfx);
		await Cmd.Wait(ReunionAirborneAnimation.TakeOff(Creature));
		await ApplyPower<SoarPower>([Creature], 1);
		await ApplyStrengthToSelf(TakeOffStrength);
		await GainBlock(TakeOffBlock);
	}

	public override Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
	{
		if (creature == Creature)
		{
			ReunionAirborneAnimation.Stop(Creature);
		}

		return base.AfterDeath(choiceContext, creature, wasRemovalPrevented, deathAnimLength);
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		return ReunionAirborneAnimation.BuildAnimator(controller);
	}
}

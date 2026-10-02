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
/// 游击队猎犬（PRTS enemy_1077_sotihd），普通近战感染生物，血薄、行动快。
/// 动画：Idle / Attack（OnAttack 0.6s）/ Die；Run_Loop 未用。
/// 招式与数值见 docs/战斗设计.md：扑咬多段、撕咬附带虚弱、环伺（格挡 + 力量）三招随机、不连用；带狼群（<see cref="ReunionPackFuryPower"/>），同伴猎犬倒下时获得力量。
/// </summary>
public sealed class ReunionGuerrillaHound : ReunionMonster
{
	public const string RushMoveId = "RUSH_MOVE";
	public const string BiteMoveId = "BITE_MOVE";
	public const string ProwlMoveId = "PROWL_MOVE";

	private const int RushHits = 2;

	public override string SceneName => "reunion_guerrilla_hound";

	public override IReadOnlyList<string> RequiredAnimations => ["Idle", "Attack", "Die"];

	public override int MinInitialHp => ToughValue(44, 42);

	public override int MaxInitialHp => ToughValue(48, 46);

	public override DamageSfxType TakeDamageSfxType => DamageSfxType.Fur;

	private int RushDamage => DeadlyValue(5, 4);

	private int BiteDamage => DeadlyValue(8, 7);

	private const int BiteWeak = 1;

	private const int ProwlBlock = 5;

	private const int ProwlStrength = 1;

	private int PackFury => DeadlyValue(4, 3);

	public override async Task AfterAddedToRoom()
	{
		await base.AfterAddedToRoom();
		await ApplyPower<ReunionPackFuryPower>([Creature], PackFury);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		MoveState rush = new(RushMoveId, RushMove, new MultiAttackIntent(RushDamage, RushHits));
		MoveState bite = new(BiteMoveId, BiteMove, new SingleAttackIntent(BiteDamage), new DebuffIntent());
		MoveState prowl = new(ProwlMoveId, ProwlMove, new BuffIntent(), new DefendIntent());
		return RandomMachine("HOUND_RANDOM", [rush, bite, prowl]);
	}

	private async Task ProwlMove(IReadOnlyList<Creature> targets)
	{
		SfxCmd.Play(BuffSfx);
		await GainBlock(ProwlBlock);
		await ApplyStrengthToSelf(ProwlStrength);
	}

	private async Task RushMove(IReadOnlyList<Creature> targets)
	{
		await DamageCmd.Attack(RushDamage).WithHitCount(RushHits).OnlyPlayAnimOnce()
			.FromMonster(this)
			.WithAttackerAnim(CreatureAnimator.attackTrigger, 0.55f)
			.WithHitFx(BiteVfx, MeleeHitSfx)
			.Execute(null);
	}

	private async Task BiteMove(IReadOnlyList<Creature> targets)
	{
		await DamageCmd.Attack(BiteDamage)
			.FromMonster(this)
			.WithAttackerAnim(CreatureAnimator.attackTrigger, 0.55f)
			.WithHitFx(BiteVfx, MeleeHitSfx)
			.Execute(null);
		await ApplyPower<WeakPower>(targets, BiteWeak);
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		return BuildAnimator(controller, "Idle", "Die", (CreatureAnimator.attackTrigger, "Attack"));
	}
}

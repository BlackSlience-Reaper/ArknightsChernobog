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
/// 特战士兵（PRTS enemy_1037_lunsabr，资源目录 enemy_1037_lunsbr），整合运动特战单位（7-2）。
/// PRTS 定位：整合运动的近身作战人员，配备了和其他战士稍有不同的装备。
/// 动画：Idle / Attack（OnAttack 0.4s）/ Die；Move_* / Run_Loop* / x_Move_Loop 未用。
/// 招式与数值见 docs/战斗设计.md（初版）：战术动作用原版“滑溜”表现——开场隐匿（下一次失去生命只失去 1），
/// 伏击 → 连刺（两段）→ 隐蔽（再得滑溜）→ 伏击；隐蔽回合不出手，逼玩家用一次小伤害先“破隐”。
/// </summary>
public sealed class ReunionSpecOpsSoldier : ReunionMonster
{
	public const string AmbushMoveId = "AMBUSH_MOVE";
	public const string TwinStabMoveId = "TWIN_STAB_MOVE";
	public const string ConcealMoveId = "CONCEAL_MOVE";

	private const int SlipperyAmount = 1;
	private const int TwinStabHits = 2;

	public override string SceneName => "reunion_spec_ops_soldier";

	public override IReadOnlyList<string> RequiredAnimations => ["Idle", "Attack", "Die"];

	public override int MinInitialHp => ToughValue(47, 44);

	public override int MaxInitialHp => ToughValue(51, 48);

	public override DamageSfxType TakeDamageSfxType => DamageSfxType.Armor;

	private int AmbushDamage => DeadlyValue(15, 14);

	private int TwinStabDamage => DeadlyValue(7, 6);

	public override async Task AfterAddedToRoom()
	{
		await base.AfterAddedToRoom();
		await ApplyPower<SlipperyPower>([Creature], SlipperyAmount);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		MoveState ambush = new(AmbushMoveId, AmbushMove, new SingleAttackIntent(AmbushDamage));
		MoveState twinStab = new(TwinStabMoveId, TwinStabMove, new MultiAttackIntent(TwinStabDamage, TwinStabHits));
		MoveState conceal = new(ConcealMoveId, ConcealMove, new BuffIntent());
		ambush.FollowUpState = twinStab;
		twinStab.FollowUpState = conceal;
		conceal.FollowUpState = ambush;
		return Machine([ambush, twinStab, conceal], ambush);
	}

	private async Task AmbushMove(IReadOnlyList<Creature> targets)
	{
		await DamageCmd.Attack(AmbushDamage)
			.FromMonster(this)
			.WithAttackerAnim(CreatureAnimator.attackTrigger, 0.4f)
			.WithHitFx(SlashVfx, MeleeHitSfx)
			.Execute(null);
	}

	private async Task TwinStabMove(IReadOnlyList<Creature> targets)
	{
		await DamageCmd.Attack(TwinStabDamage).WithHitCount(TwinStabHits).OnlyPlayAnimOnce()
			.FromMonster(this)
			.WithAttackerAnim(CreatureAnimator.attackTrigger, 0.4f)
			.WithHitFx(SlashVfx, MeleeHitSfx)
			.Execute(null);
	}

	private async Task ConcealMove(IReadOnlyList<Creature> targets)
	{
		SfxCmd.Play(BuffSfx);
		await ApplyPower<SlipperyPower>([Creature], SlipperyAmount);
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		return BuildAnimator(controller, "Idle", "Die", (CreatureAnimator.attackTrigger, "Attack"));
	}
}

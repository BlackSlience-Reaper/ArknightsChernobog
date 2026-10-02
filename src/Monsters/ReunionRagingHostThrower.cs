using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace ArknightsChernobog.Monsters;

/// <summary>
/// 狂暴宿主投掷手（PRTS enemy_1063_rageth），失去控制后的远程宿主（7-3）。
/// PRTS 定位：逐渐陷入狂乱的敌方投掷手，攻击力很高，会持续损失生命。
/// 动画：Idle / Attack（2.5s，OnAttack 1.467s，出手慢）/ Die；Move 未用。
/// 招式与数值见 docs/战斗设计.md（初版）：带原版“瓦解”；投掷、乱掷（三段）、砸石（弃牌堆塞晕眩）三招随机、不连用。
/// </summary>
public sealed class ReunionRagingHostThrower : ReunionMonster
{
	public const string HurlMoveId = "HURL_MOVE";
	public const string VolleyMoveId = "VOLLEY_MOVE";
	public const string RockMoveId = "ROCK_MOVE";

	private const int RockDazed = 1;

	private const int LossOfControl = 14;
	private const int VolleyHits = 3;

	public override string SceneName => "reunion_raging_host_thrower";

	public override IReadOnlyList<string> RequiredAnimations => ["Idle", "Attack", "Die"];

	public override int MinInitialHp => ToughValue(148, 140);

	public override int MaxInitialHp => ToughValue(154, 146);

	public override DamageSfxType TakeDamageSfxType => DamageSfxType.Fur;

	private int HurlDamage => DeadlyValue(15, 14);

	private int VolleyDamage => DeadlyValue(6, 5);

	private int RockDamage => DeadlyValue(9, 8);

	public override async Task AfterAddedToRoom()
	{
		await base.AfterAddedToRoom();
		await ApplyLossOfControl(LossOfControl);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		MoveState hurl = new(HurlMoveId, HurlMove, new SingleAttackIntent(HurlDamage));
		MoveState volley = new(VolleyMoveId, VolleyMove, new MultiAttackIntent(VolleyDamage, VolleyHits));
		MoveState rock = new(RockMoveId, RockMove, new SingleAttackIntent(RockDamage), new StatusIntent(RockDazed));
		return RandomMachine("RAGING_THROWER_RANDOM", [hurl, volley, rock]);
	}

	private async Task HurlMove(IReadOnlyList<Creature> targets)
	{
		await DamageCmd.Attack(HurlDamage)
			.FromMonster(this)
			.WithAttackerAnim(CreatureAnimator.attackTrigger, 1.45f)
			.WithHitFx(HeavyBluntVfx, BombHitSfx)
			.Execute(null);
	}

	private async Task RockMove(IReadOnlyList<Creature> targets)
	{
		await DamageCmd.Attack(RockDamage)
			.FromMonster(this)
			.WithAttackerAnim(CreatureAnimator.attackTrigger, 1.45f)
			.WithHitFx(HeavyBluntVfx, BombHitSfx)
			.Execute(null);
		await AddStatusCards<Dazed>(targets, RockDazed);
	}

	private async Task VolleyMove(IReadOnlyList<Creature> targets)
	{
		await DamageCmd.Attack(VolleyDamage).WithHitCount(VolleyHits).OnlyPlayAnimOnce()
			.FromMonster(this)
			.WithAttackerAnim(CreatureAnimator.attackTrigger, 1.45f)
			.WithHitFx(BluntVfx, BombHitSfx)
			.Execute(null);
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		return BuildAnimator(controller, "Idle", "Die", (CreatureAnimator.attackTrigger, "Attack"));
	}
}

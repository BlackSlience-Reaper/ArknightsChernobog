using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace ArknightsChernobog.Monsters;

/// <summary>
/// 游击队迫击炮兵（PRTS enemy_1082_soticn），后排重火力。PRTS 定位：受到强化时攻击速度大幅提升。
/// 动画：Idle_1 待机 / Attack（OnAttack 0.533s）/ Die；装填借用 Idle_2 放一遍再回 Idle_1。Move 未用。
/// 招式与数值见 docs/战斗设计.md（初版）：装填预告下一回合的重炮，炮击往弃牌堆塞一张原版“灼烧”。
/// </summary>
public sealed class ReunionGuerrillaMortarGunner : ReunionMonster
{
	public const string ReloadMoveId = "RELOAD_MOVE";
	public const string BombardMoveId = "BOMBARD_MOVE";
	public const string BarrageMoveId = "BARRAGE_MOVE";

	private const string ReloadTrigger = "Reload";
	private const int BarrageHits = 3;
	private const int BombardBurns = 1;

	public override string SceneName => "reunion_guerrilla_mortar_gunner";

	public override IReadOnlyList<string> RequiredAnimations => ["Idle_1", "Idle_2", "Attack", "Die"];

	public override int MinInitialHp => ToughValue(60, 58);

	public override int MaxInitialHp => ToughValue(64, 62);

	public override DamageSfxType TakeDamageSfxType => DamageSfxType.Armor;

	private int ReloadBlock => ToughValue(10, 8);

	private int BombardDamage => DeadlyValue(18, 16);

	private int BarrageDamage => DeadlyValue(5, 4);

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		MoveState reload = new(ReloadMoveId, ReloadMove, new DefendIntent());
		MoveState bombard = new(BombardMoveId, BombardMove, new SingleAttackIntent(BombardDamage), new StatusIntent(BombardBurns));
		MoveState barrage = new(BarrageMoveId, BarrageMove, new MultiAttackIntent(BarrageDamage, BarrageHits));
		reload.FollowUpState = bombard;
		bombard.FollowUpState = barrage;
		barrage.FollowUpState = reload;
		return Machine([reload, bombard, barrage], reload);
	}

	private async Task ReloadMove(IReadOnlyList<Creature> targets)
	{
		SfxCmd.Play(BuffSfx);
		await CreatureCmd.TriggerAnim(Creature, ReloadTrigger, 0.4f);
		await GainBlock(ReloadBlock);
	}

	private async Task BombardMove(IReadOnlyList<Creature> targets)
	{
		await DamageCmd.Attack(BombardDamage)
			.FromMonster(this)
			.WithAttackerAnim(CreatureAnimator.attackTrigger, 0.55f)
			.WithHitFx(HeavyBluntVfx, BombHitSfx)
			.Execute(null);
		await CardPileCmd.AddToCombatAndPreview<Burn>(targets, PileType.Discard, BombardBurns, null);
	}

	private async Task BarrageMove(IReadOnlyList<Creature> targets)
	{
		await DamageCmd.Attack(BarrageDamage).WithHitCount(BarrageHits).OnlyPlayAnimOnce()
			.FromMonster(this)
			.WithAttackerAnim(CreatureAnimator.attackTrigger, 0.55f)
			.WithHitFx(BluntVfx, BombHitSfx)
			.Execute(null);
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		return BuildAnimator(controller, "Idle_1", "Die",
			(CreatureAnimator.attackTrigger, "Attack"),
			(ReloadTrigger, "Idle_2"));
	}
}

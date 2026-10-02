using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace ArknightsChernobog.Monsters;

/// <summary>
/// 雇佣军萨卡兹战士（PRTS enemy_1084_sotidm_3），7-10~7-12 的萨卡兹雇佣军。PRTS 定位同游击队版：接触脉冲波时不受伤害、攻击转为法术伤害；
/// 7-10 里攻击速度大幅提升。
/// 与游击队萨卡兹战士同一套骨骼和动画，但贴图是雇佣军装束、附件网格不同，单独导入并转换。
/// 动画：Idle / Attack（OnAttack 0.533s）/ Die；裂斩借用 Attack_2（OnAttack 0.967s）放一遍再回 Idle，不切形态；
/// Idle_2 / Die_2 / Move* 未用。
/// 招式与数值见 docs/战斗设计.md（初版）：偏单兵强度，不做祭坛仪式也不强化队友——斩击 → 裂斩（两段）→ 佣兵战意（自身力量 + 格挡）。
/// </summary>
public sealed class ReunionMercenarySarkazWarrior : ReunionMonster
{
	public const string CleaveMoveId = "CLEAVE_MOVE";
	public const string RendingSlashMoveId = "RENDING_SLASH_MOVE";
	public const string BattleWillMoveId = "BATTLE_WILL_MOVE";

	private const string HeavyAttackTrigger = "HeavyAttack";
	private const int RendingHits = 2;
	private const int BattleWillBlock = 12;

	public override string SceneName => "reunion_mercenary_sarkaz_warrior";

	public override IReadOnlyList<string> RequiredAnimations => ["Idle", "Attack", "Attack_2", "Die"];

	public override int MinInitialHp => ToughValue(106, 100);

	public override int MaxInitialHp => ToughValue(110, 104);

	public override DamageSfxType TakeDamageSfxType => DamageSfxType.Armor;

	private int CleaveDamage => DeadlyValue(17, 16);

	private int RendingDamage => DeadlyValue(9, 8);

	private int BattleWillStrength => DeadlyValue(4, 3);

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		MoveState cleave = new(CleaveMoveId, CleaveMove, new SingleAttackIntent(CleaveDamage));
		MoveState rending = new(RendingSlashMoveId, RendingSlashMove, new MultiAttackIntent(RendingDamage, RendingHits));
		MoveState battleWill = new(BattleWillMoveId, BattleWillMove, new BuffIntent(), new DefendIntent());
		cleave.FollowUpState = rending;
		rending.FollowUpState = battleWill;
		battleWill.FollowUpState = cleave;
		return Machine([cleave, rending, battleWill], cleave);
	}

	private async Task CleaveMove(IReadOnlyList<Creature> targets)
	{
		await DamageCmd.Attack(CleaveDamage)
			.FromMonster(this)
			.WithAttackerAnim(CreatureAnimator.attackTrigger, 0.55f)
			.WithHitFx(SlashVfx, MeleeHitSfx)
			.Execute(null);
	}

	private async Task RendingSlashMove(IReadOnlyList<Creature> targets)
	{
		await DamageCmd.Attack(RendingDamage).WithHitCount(RendingHits).OnlyPlayAnimOnce()
			.FromMonster(this)
			.WithAttackerAnim(HeavyAttackTrigger, 0.95f)
			.WithHitFx(ArtsVfx, MeleeHitSfx)
			.Execute(null);
	}

	private async Task BattleWillMove(IReadOnlyList<Creature> targets)
	{
		SfxCmd.Play(BuffSfx);
		await ApplyStrengthToSelf(BattleWillStrength);
		await GainBlock(BattleWillBlock);
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		return BuildAnimator(controller, "Idle", "Die",
			(CreatureAnimator.attackTrigger, "Attack"),
			(HeavyAttackTrigger, "Attack_2"));
	}
}

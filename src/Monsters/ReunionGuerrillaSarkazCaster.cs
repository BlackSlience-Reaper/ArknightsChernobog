using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace ArknightsChernobog.Monsters;

/// <summary>
/// 游击队萨卡兹术师（PRTS enemy_1085_sotiwz），精英辅助。PRTS 定位：周期性对周围造成法术伤害（“献祭”），
/// 接触脉冲波后不受伤害且范围扩大。
/// skel 没有攻击动画，只有 Idle / Move / Die 与强化后的 Idle_2 / Move_2（无 Die_2）：
/// 源石法术不播施法者动画，只在目标身上放特效；第一次献祭仪式后待机切到 Idle_2。
/// 招式与数值见 docs/战斗设计.md（初版）：源石法术 → 献祭仪式（自损生命，自身力量）→ 源石洪流（三段）→ 循环。
/// </summary>
public sealed class ReunionGuerrillaSarkazCaster : ReunionMonster
{
	public const string ArtsBoltMoveId = "ARTS_BOLT_MOVE";
	public const string SacrificeRitualMoveId = "SACRIFICE_RITUAL_MOVE";
	public const string ArtsTorrentMoveId = "ARTS_TORRENT_MOVE";

	private const int TorrentHits = 3;

	private const string EmpowerTrigger = "Empower";

	private bool _isEmpowered;

	public override string SceneName => "reunion_guerrilla_sarkaz_caster";

	public override IReadOnlyList<string> RequiredAnimations => ["Idle", "Idle_2", "Die"];

	public override int MinInitialHp => ToughValue(70, 66);

	public override int MaxInitialHp => ToughValue(74, 70);

	public override DamageSfxType TakeDamageSfxType => DamageSfxType.Magic;

	private int ArtsBoltDamage => DeadlyValue(10, 9);

	private const int SacrificeHpLoss = 6;

	private int RitualStrength => DeadlyValue(4, 3);

	private int TorrentDamage => DeadlyValue(4, 3);

	/// <summary>是否已切到强化待机；动画状态机的条件分支读它。</summary>
	public bool IsEmpowered
	{
		get => _isEmpowered;
		private set
		{
			AssertMutable();
			_isEmpowered = value;
		}
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		MoveState artsBolt = new(ArtsBoltMoveId, ArtsBoltMove, new SingleAttackIntent(ArtsBoltDamage));
		MoveState ritual = new(SacrificeRitualMoveId, SacrificeRitualMove, new BuffIntent());
		MoveState torrent = new(ArtsTorrentMoveId, ArtsTorrentMove, new MultiAttackIntent(TorrentDamage, TorrentHits));
		artsBolt.FollowUpState = ritual;
		ritual.FollowUpState = torrent;
		torrent.FollowUpState = artsBolt;
		return Machine([artsBolt, ritual, torrent], artsBolt);
	}

	private async Task ArtsBoltMove(IReadOnlyList<Creature> targets)
	{
		await DamageCmd.Attack(ArtsBoltDamage)
			.FromMonster(this)
			.WithNoAttackerAnim()
			.WithWaitBeforeHit(0.3f, 0.5f)
			.WithHitFx(ArtsVfx, ArtsHitSfx)
			.Execute(null);
	}

	private async Task ArtsTorrentMove(IReadOnlyList<Creature> targets)
	{
		await DamageCmd.Attack(TorrentDamage).WithHitCount(TorrentHits)
			.FromMonster(this)
			.WithNoAttackerAnim()
			.WithWaitBeforeHit(0.3f, 0.5f)
			.WithHitFx(ArtsVfx, ArtsHitSfx)
			.Execute(null);
	}

	private async Task SacrificeRitualMove(IReadOnlyList<Creature> targets)
	{
		SfxCmd.Play(BuffSfx);
		IsEmpowered = true;
		await CreatureCmd.TriggerAnim(Creature, EmpowerTrigger, 0.4f);
		await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Creature, SacrificeHpLoss,
			ValueProp.Unblockable | ValueProp.Unpowered, Creature);
		if (Creature.IsDead)
		{
			return;
		}

		await ApplyStrengthToSelf(RitualStrength);
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		AnimState idle = new("Idle", isLooping: true);
		AnimState idle2 = new("Idle_2", isLooping: true);

		// 同一触发器按注册顺序取第一个条件成立的分支，强化形态的分支必须先注册。
		CreatureAnimator animator = new(idle, controller);
		animator.AddAnyState(CreatureAnimator.idleTrigger, idle2, () => IsEmpowered);
		animator.AddAnyState(CreatureAnimator.idleTrigger, idle);
		animator.AddAnyState(EmpowerTrigger, idle2);
		animator.AddAnyState(CreatureAnimator.deathTrigger, new AnimState("Die"));
		return animator;
	}
}

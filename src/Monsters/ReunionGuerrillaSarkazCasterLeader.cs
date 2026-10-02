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
/// 游击队萨卡兹术师组长（PRTS enemy_1085_sotiwz_2），萨卡兹术师的头目版。PRTS 定位同本体：周期性对周围造成法术伤害，
/// 接触脉冲波后不受伤害且范围扩大。
/// 与本体同一套骨骼和动画（附件网格随贴图重新打包，skel 单独转换）：只有 Idle / Move / Die 与 Idle_2 / Move_2，没有攻击动画，
/// 法术只在目标身上放特效；第一次献祭仪式后待机切到 Idle_2。
/// 招式与数值见 docs/战斗设计.md（初版）：源石法术 → 献祭仪式（自损，自身力量）→ 源石洪流（多段）。
/// </summary>
public sealed class ReunionGuerrillaSarkazCasterLeader : ReunionMonster
{
	public const string ArtsBoltMoveId = "ARTS_BOLT_MOVE";
	public const string SacrificeRitualMoveId = "SACRIFICE_RITUAL_MOVE";
	public const string ArtsTorrentMoveId = "ARTS_TORRENT_MOVE";

	private const string EmpowerTrigger = "Empower";
	private const int TorrentHits = 3;
	private const int SacrificeHpLoss = 8;

	private bool _isEmpowered;

	public override string SceneName => "reunion_guerrilla_sarkaz_caster_leader";

	public override IReadOnlyList<string> RequiredAnimations => ["Idle", "Idle_2", "Die"];

	public override int MinInitialHp => ToughValue(84, 80);

	public override int MaxInitialHp => ToughValue(88, 84);

	public override DamageSfxType TakeDamageSfxType => DamageSfxType.Magic;

	private int ArtsBoltDamage => DeadlyValue(12, 11);

	private int TorrentDamage => DeadlyValue(6, 5);

	private int RitualStrength => DeadlyValue(4, 3);

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

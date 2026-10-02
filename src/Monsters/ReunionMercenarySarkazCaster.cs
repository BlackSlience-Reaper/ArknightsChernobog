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
/// 雇佣军萨卡兹术师（PRTS enemy_1085_sotiwz_3），7-11/7-12 的萨卡兹雇佣军。PRTS 定位同游击队版：周期性对周围造成法术伤害，
/// 接触脉冲波时不受伤害且攻击范围扩大。
/// 与游击队萨卡兹术师同一套骨骼和动画，但贴图是雇佣军装束、附件网格不同，单独导入并转换。
/// skel 没有攻击动画：Idle / Die，法术只在目标身上放特效；Idle_2 / Move* 未用。
/// 招式与数值见 docs/战斗设计.md（初版）：偏单兵强度，不献祭、不强化队友——源石法术 → 源石洪流（三段 + 1 层虚弱）→ 凝聚（自身力量 + 格挡）。
/// </summary>
public sealed class ReunionMercenarySarkazCaster : ReunionMonster
{
	public const string ArtsBoltMoveId = "ARTS_BOLT_MOVE";
	public const string ArtsTorrentMoveId = "ARTS_TORRENT_MOVE";
	public const string FocusMoveId = "FOCUS_MOVE";

	private const int TorrentHits = 3;
	private const int TorrentWeak = 1;
	private const int FocusBlock = 10;

	public override string SceneName => "reunion_mercenary_sarkaz_caster";

	public override IReadOnlyList<string> RequiredAnimations => ["Idle", "Die"];

	public override int MinInitialHp => ToughValue(68, 64);

	public override int MaxInitialHp => ToughValue(72, 68);

	public override DamageSfxType TakeDamageSfxType => DamageSfxType.Magic;

	private int ArtsBoltDamage => DeadlyValue(13, 12);

	private int TorrentDamage => DeadlyValue(6, 5);

	private int FocusStrength => DeadlyValue(4, 3);

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		MoveState artsBolt = new(ArtsBoltMoveId, ArtsBoltMove, new SingleAttackIntent(ArtsBoltDamage));
		MoveState torrent = new(ArtsTorrentMoveId, ArtsTorrentMove, new MultiAttackIntent(TorrentDamage, TorrentHits), new DebuffIntent());
		MoveState focus = new(FocusMoveId, FocusMove, new BuffIntent(), new DefendIntent());
		artsBolt.FollowUpState = torrent;
		torrent.FollowUpState = focus;
		focus.FollowUpState = artsBolt;
		return Machine([artsBolt, torrent, focus], artsBolt);
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
		await ApplyPower<WeakPower>(targets, TorrentWeak);
	}

	private async Task FocusMove(IReadOnlyList<Creature> targets)
	{
		SfxCmd.Play(BuffSfx);
		await ApplyStrengthToSelf(FocusStrength);
		await GainBlock(FocusBlock);
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		AnimState idle = new("Idle", isLooping: true);
		CreatureAnimator animator = new(idle, controller);
		animator.AddAnyState(CreatureAnimator.idleTrigger, idle);
		animator.AddAnyState(CreatureAnimator.deathTrigger, new AnimState("Die"));
		return animator;
	}
}

using ArknightsChernobog.Powers;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace ArknightsChernobog.Monsters;

/// <summary>
/// 法术大师A1（PRTS enemy_1041_lazerd），与特战士兵、特战术师同关（7-2）的无人机，后排法术输出。
/// PRTS 定位：【飞行单位】飞行速度非常快，使用远程武器造成法术伤害。类名不带 A1，避免数字进模型 id；显示名仍是“法术大师A1”。
/// 动画：Idle / Attack（0.567s，OnAttack 0.233s）/ Die；Move_Begin / Move_Loop / Move_End 未用。
/// 招式与数值见 docs/战斗设计.md：血薄；法术射线（三段）两次后超载（自身力量），多段吃力量，拖久了很痛。
/// 带悬浮（<see cref="ReunionHoverPower"/>）：被打落后击晕一回合，下一招固定起飞（重新悬浮 + 1 力量），再回到法术射线。
/// </summary>
public sealed class ReunionArtsMaster : ReunionMonster, IReunionHovering
{
	public const string ArtsBeamMoveId = "ARTS_BEAM_MOVE";
	public const string OverchargeMoveId = "OVERCHARGE_MOVE";
	public const string TakeoffMoveId = "TAKE_OFF_MOVE";

	private const int BeamHits = 3;

	public override string SceneName => "reunion_arts_master";

	public override IReadOnlyList<string> RequiredAnimations => ["Idle", "Attack", "Die"];

	public override int MinInitialHp => ToughValue(42, 40);

	public override int MaxInitialHp => ToughValue(46, 44);

	public override DamageSfxType TakeDamageSfxType => DamageSfxType.Armor;

	private int BeamDamage => DeadlyValue(4, 3);

	private int OverchargeStrength => DeadlyValue(3, 2);

	private const int HoverHits = 3;
	private const int TakeoffStrength = 1;

	// 悬空和落地时 SpineSprite 的高度。悬空值必须与 reunion_arts_master.tscn 里 %Visuals 的 position.y 一致。
	private const float HoverY = -110f;
	private const float GroundY = -12f;
	private const float CrashTilt = 0.18f;
	private const float CrashSeconds = 0.45f;
	private const float TakeoffSeconds = 0.6f;

	// 悬浮：攻击伤害减半，被未格挡的攻击命中 3 次后坠落并被击晕，之后固定起飞（PRTS：飞行单位只能被远程攻击）。
	public override async Task AfterAddedToRoom()
	{
		await base.AfterAddedToRoom();
		await ApplyPower<ReunionHoverPower>([Creature], HoverHits);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		MoveState beam = new(ArtsBeamMoveId, ArtsBeamMove, new MultiAttackIntent(BeamDamage, BeamHits));
		MoveState secondBeam = new(ArtsBeamMoveId + "_2", ArtsBeamMove, new MultiAttackIntent(BeamDamage, BeamHits));
		MoveState overcharge = new(OverchargeMoveId, OverchargeMove, new BuffIntent());
		// 起飞只能由坠落后的击晕接上（Stun 的 nextMoveId），不在常规循环里。
		MoveState takeoff = new(TakeoffMoveId, TakeoffMove, new BuffIntent()) { MustPerformOnceBeforeTransitioning = true };
		beam.FollowUpState = secondBeam;
		secondBeam.FollowUpState = overcharge;
		overcharge.FollowUpState = beam;
		takeoff.FollowUpState = beam;
		return Machine([beam, secondBeam, overcharge, takeoff], beam);
	}

	/// <summary>悬浮层数扣完：机体摔到地面并歪斜，击晕一回合，醒来后的下一招固定是起飞。</summary>
	public async Task Crash()
	{
		SfxCmd.Play(HeavyHitSfx);
		TweenBody(GroundY, CrashTilt, CrashSeconds, Tween.TransitionType.Bounce);
		await CreatureCmd.Stun(Creature, TakeoffMoveId);
	}

	private async Task ArtsBeamMove(IReadOnlyList<Creature> targets)
	{
		await DamageCmd.Attack(BeamDamage).WithHitCount(BeamHits).OnlyPlayAnimOnce()
			.FromMonster(this)
			.WithAttackerAnim(CreatureAnimator.attackTrigger, 0.25f)
			.WithHitFx(ArtsVfx, ArtsHitSfx)
			.Execute(null);
	}

	private async Task OverchargeMove(IReadOnlyList<Creature> targets)
	{
		SfxCmd.Play(BuffSfx);
		await ApplyStrengthToSelf(OverchargeStrength);
	}

	private async Task TakeoffMove(IReadOnlyList<Creature> targets)
	{
		SfxCmd.Play(BuffSfx);
		TweenBody(HoverY, 0f, TakeoffSeconds, Tween.TransitionType.Back);
		await Cmd.Wait(TakeoffSeconds);
		await ApplyPower<ReunionHoverPower>([Creature], HoverHits);
		await ApplyStrengthToSelf(TakeoffStrength);
	}

	/// <summary>
	/// 骨骼没有起降动画，用补间移动 SpineSprite 本身（不动生物节点，判定框、血条和意图位置不变）。
	/// 没有生物节点（测试模式、图鉴）时跳过；只是表现，不影响联机同步。
	/// </summary>
	private void TweenBody(float y, float rotation, float seconds, Tween.TransitionType transition)
	{
		if (Creature.GetCreatureNode()?.Visuals.SpineBody?.BoundObject is not Node2D body)
		{
			return;
		}

		Tween tween = body.CreateTween().SetParallel();
		tween.TweenProperty(body, "position:y", y, seconds).SetTrans(transition).SetEase(Tween.EaseType.Out);
		tween.TweenProperty(body, "rotation", rotation, seconds).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		return BuildAnimator(controller, "Idle", "Die", (CreatureAnimator.attackTrigger, "Attack"));
	}
}

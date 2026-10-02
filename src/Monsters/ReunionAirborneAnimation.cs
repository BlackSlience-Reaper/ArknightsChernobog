using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace ArknightsChernobog.Monsters;

/// <summary>
/// 突袭战士的空降表现，全部从 PRTS 的 Start 动画里截取。Start 是完整的空降：0–0.4s 从画面上方落下，
/// 0.4–0.87s 背包喷射悬停（0.72s 左右最高），约 1.0s 触地，之后收势（时间点由逐帧量骨骼包围盒得出）。
/// - 悬空：Start 定速为 0，由补间在 <see cref="HoverFrom"/>–<see cref="HoverTo"/> 之间来回驱动播放时刻（首→尾→首），
///   这段双腿自然下垂，来回播自带上下浮动。不能整段循环：悬停段两端双腿一收一张，首尾相接像在空中踢腿。
/// - 起飞：从触地时刻倒着播回悬停最高点（等于把落地倒放），再进入来回悬浮。
/// - 降落：停掉悬浮补间，从当前悬停时刻接着正常播 Start 到触地，伤害在触地时结算；不从 0 重播，否则会先闪回天上。
/// 补间绑在 SpineSprite 上，节点释放时随之结束；只动 Spine 轨道时间，判定框、血条、意图位置不变；纯表现，不参与联机同步。
/// </summary>
internal static class ReunionAirborneAnimation
{
	public const string StartAnimation = "Start";
	private const string IdleAnimation = "Idle";
	private const string DeathAnimation = "Die";
	private const string HoverTrigger = "Hover";
	private const string LandTrigger = "Land";

	private const float HoverFrom = 0.55f;
	private const float HoverTo = 0.72f;
	private const float HoverHalfCycle = 0.55f;
	private const float TouchdownTime = 1.0f;

	/// <summary>空投入场从画面上方落到触地的时长，召唤方等这么久再继续，让落地先演完。</summary>
	public const float DropInSeconds = TouchdownTime;
	private const float TakeOffSeconds = 0.5f;
	private const float TakeOffBlend = 0.1f;

	private const string TweenMeta = "reunion_hover_tween";

	/// <summary>
	/// 空降兵的动画机：开场悬空，Hover 触发器回到悬空，Land 触发器播落地后回到待机。
	/// <paramref name="dropIn"/> 为真时开场改为从头播一遍 Start（从画面上方落下、触地收势）再接待机，即空投入场后站在地上。
	/// </summary>
	public static CreatureAnimator BuildAnimator(MegaSprite controller, bool dropIn = false)
	{
		AnimState idle = new(IdleAnimation, isLooping: true);
		AnimState hover = new(StartAnimation, isLooping: true);
		CreatureAnimator animator = new(dropIn ? new AnimState(StartAnimation) { NextState = idle } : hover, controller);
		animator.AddAnyState(CreatureAnimator.idleTrigger, idle);
		animator.AddAnyState(HoverTrigger, hover);
		animator.AddAnyState(LandTrigger, new AnimState(StartAnimation) { NextState = idle });
		animator.AddAnyState(CreatureAnimator.deathTrigger, new AnimState(DeathAnimation));
		if (dropIn)
		{
			return animator;
		}

		// 先定格在悬停段，避免入树前的第一帧露出 Start 开头（人在画面外）；补间要等节点入树后才能建。
		FreezeAt(controller, HoverTo);
		Callable.From(() => StartHover(controller, takeOff: false)).CallDeferred();
		return animator;
	}

	/// <summary>起飞：从地面倒放落地动作升回悬停，返回动作时长。没有生物节点（测试模式）时返回 0。</summary>
	public static float TakeOff(Creature creature)
	{
		NCreature? node = creature.GetCreatureNode();
		MegaSprite? body = node?.Visuals.SpineBody;
		if (node == null || body == null)
		{
			return 0f;
		}

		node.SetAnimationTrigger(HoverTrigger);
		StartHover(body, takeOff: true);
		return TakeOffSeconds;
	}

	/// <summary>
	/// 降落：停掉悬浮，从当前悬停时刻继续播 Start 到落地收势，返回距离触地还要等的秒数（伤害在触地时结算）。
	/// 没有生物节点（测试模式）时返回 0；当前不在悬停（例如被其他动画打断）时从头播完整空降。
	/// </summary>
	public static float Land(Creature creature)
	{
		NCreature? node = creature.GetCreatureNode();
		MegaSprite? body = node?.Visuals.SpineBody;
		if (node == null || body == null)
		{
			return 0f;
		}

		StopHover(body);
		float hoverTime = 0f;
		using (MegaTrackEntry? current = body.GetAnimationState().GetCurrent(0))
		{
			if (current != null && current.IsLoop() && current.GetAnimationName() == StartAnimation)
			{
				hoverTime = current.GetTrackTime();
			}
		}

		node.SetAnimationTrigger(LandTrigger);
		using (MegaTrackEntry? landing = body.GetAnimationState().GetCurrent(0))
		{
			if (landing != null && hoverTime > 0f)
			{
				landing.SetMixDuration(0f);
				landing.SetTrackTime(hoverTime);
			}
		}

		return Math.Max(0f, TouchdownTime - hoverTime);
	}

	/// <summary>死亡等场合停掉悬浮补间（否则补间会一直空转到节点释放）。</summary>
	public static void Stop(Creature creature)
	{
		MegaSprite? body = creature.GetCreatureNode()?.Visuals.SpineBody;
		if (body != null)
		{
			StopHover(body);
		}
	}

	private static void StartHover(MegaSprite body, bool takeOff)
	{
		if (body.BoundObject is not Node2D sprite || !GodotObject.IsInstanceValid(sprite) || !sprite.IsInsideTree())
		{
			return;
		}

		StopHover(body);
		Tween tween = sprite.CreateTween();
		Callable setTime = Callable.From<float>(time => SetHoverTime(body, time));
		if (takeOff)
		{
			FreezeAt(body, TouchdownTime, TakeOffBlend);
			tween.TweenMethod(setTime, TouchdownTime, HoverTo, TakeOffSeconds).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
		}
		else
		{
			// 开场从随机时刻起步，同场多名空降兵不会整齐划一地摆动。
			float start = HoverFrom + (HoverTo - HoverFrom) * GD.Randf();
			FreezeAt(body, start);
			tween.TweenMethod(setTime, start, HoverTo, HoverHalfCycle * (HoverTo - start) / (HoverTo - HoverFrom));
		}

		tween.TweenCallback(Callable.From(() => PingPong(body, sprite)));
		sprite.SetMeta(TweenMeta, tween);
	}

	private static void PingPong(MegaSprite body, Node2D sprite)
	{
		if (!GodotObject.IsInstanceValid(sprite) || !sprite.IsInsideTree())
		{
			return;
		}

		Callable setTime = Callable.From<float>(time => SetHoverTime(body, time));
		Tween tween = sprite.CreateTween().SetLoops();
		tween.TweenMethod(setTime, HoverTo, HoverFrom, HoverHalfCycle).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
		tween.TweenMethod(setTime, HoverFrom, HoverTo, HoverHalfCycle).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
		sprite.SetMeta(TweenMeta, tween);
	}

	private static void StopHover(MegaSprite body)
	{
		if (body.BoundObject is not Node2D sprite || !GodotObject.IsInstanceValid(sprite) || !sprite.HasMeta(TweenMeta))
		{
			return;
		}

		sprite.GetMeta(TweenMeta).As<Tween>()?.Kill();
		sprite.RemoveMeta(TweenMeta);
	}

	/// <summary>把当前的悬空 Start 轨道停住并定在指定时刻；当前轨道不是悬空（已落地、死亡）时不动。</summary>
	private static void FreezeAt(MegaSprite body, float time, float? mixDuration = null)
	{
		using MegaTrackEntry? track = HoverTrack(body);
		if (track == null)
		{
			return;
		}

		// 动画机给循环动画随机了起点和 0.9–1.1 的速度，这里覆盖掉，之后完全由补间驱动。
		track.SetTimeScale(0f);
		track.SetTrackTime(time);
		if (mixDuration is { } mix)
		{
			track.SetMixDuration(mix);
		}
	}

	private static void SetHoverTime(MegaSprite body, float time)
	{
		using MegaTrackEntry? track = HoverTrack(body);
		track?.SetTrackTime(time);
	}

	private static MegaTrackEntry? HoverTrack(MegaSprite body)
	{
		MegaTrackEntry? track = body.GetAnimationState().GetCurrent(0);
		if (track != null && track.IsLoop() && track.GetAnimationName() == StartAnimation)
		{
			return track;
		}

		track?.Dispose();
		return null;
	}
}

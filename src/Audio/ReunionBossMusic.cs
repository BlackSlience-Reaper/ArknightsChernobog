using ArknightsChernobog.Encounters;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Audio;
using STS2RitsuLib;
using STS2RitsuLib.Audio;

namespace ArknightsChernobog.Audio;

/// <summary>
/// 爱国者 Boss 战音乐（塞壬唱片）：一阶段《Непоколебимость（不屈）》，一阶段倒下时淡出静音，重生时换《视死如归》，打完后淡出。
/// 原版 EncounterModel.CustomBgm 只接受 FMOD Studio 事件（RitsuLib 也只把自带音频库的事件接进这条路），
/// 所以由这里在 RitsuLib 的战斗开始事件里接管：只停原版本幕音乐（环境音不动），经 RitsuLib 音频服务流式播放模组里的文件。
///
/// 与原版 Boss 的对照（克隆里实测凯撒蟹）：原版 PlayCustomMusic 播独立的 Boss 事件，战斗结束后不恢复本幕音乐，
/// Boss 事件自己放完结尾段，直到下一幕的 UpdateMusic 换曲。这里照做：胜利后只淡出，不碰原版控制器，
/// 进下一幕由原版正常起曲。不能用 NRunMusicController.StopMusic：它连环境音事件一起拆掉，而 C# 侧记着的环境音名不清，
/// 之后同名环境音不会重建，进火堆时原版脚本对空环境音事件调用 update_campfire_ambience 直接闪退（RPEFinale 第四幕火堆闪退的根因）。
///
/// 流式文件在 FMOD 底层通道上播放，不经过 Studio 的 bus:/master/music，游戏的音乐音量滑条管不到它
/// （原版把滑条值平方后写进这条总线，滑条 0.5 就是 0.25，约 -12 dB），所以每帧把音乐总线音量乘进句柄音量；主音量总线就是底层主通道组，本来就生效。
/// 句柄挂在房间作用域上，离开 Boss 房间时 RitsuLib 必定停掉；阵亡、退回主菜单时原版会调 StopMusic，这里跟着停。
/// 文件必须以原文件打进包（assets/audio/music/*.import 为 keep）：FMOD 读的是原始字节，Godot 导入产物它不认。
/// 必须用 StreamingMusic 直接把 res:// 路径交给 FMOD，不能用 StreamingResourceMusic：RitsuLib 0.6.4 会先把文件复制到系统缓存目录
/// （Windows 上是 %LOCALAPPDATA%，路径带用户名），而游戏自带的 FMOD 插件按 Latin-1 解码路径、打开失败后不判空，
/// 用户名含中文的玩家一进 Boss 战就闪退。
/// 转阶段的两个入口由爱国者在真实战斗里调用（模拟推演里 IsLiveCombat 为假，不会碰音乐）。
/// </summary>
internal static class ReunionBossMusic
{
	public const string PhaseOneTrackPath = ChernobogAssets.MusicRoot + "reunion_patriot_boss.mp3";
	public const string PhaseTwoTrackPath = ChernobogAssets.MusicRoot + "reunion_patriot_boss_phase2.mp3";

	// 对齐原版第二幕 Boss 音乐的响度：从 act2_a1.bank 解出暴食者、凯撒蟹、知识恶魔的分轨，按游戏里同时播放的组合叠加，
	// 高潮段电平（3 秒窗口 RMS 的 90 分位）平均 -14.0 dBFS；本模组两首原件是 -8.2 / -8.8 dBFS，按差值换算成倍率。
	// 这是总线之前的倍率，实际音量还要乘音乐总线音量，与原版事件走同一条滑条。
	private const float PhaseOneVolume = 0.51f;
	private const float PhaseTwoVolume = 0.55f;

	// FMOD 流式播放的句柄不支持自带淡出（TryStop 带淡出也是立刻停），淡出由每帧压音量实现。
	private const double RebirthFadeSeconds = 2.0;
	private const double VictoryFadeSeconds = 3.0;
	private const double SwitchFadeSeconds = 0.5;

	private sealed class Track(AudioMusicHandle handle, float volume)
	{
		public AudioMusicHandle Handle { get; } = handle;
		public float Volume { get; } = volume;
		public double FadeSeconds { get; private set; }
		public double FadeElapsed { get; set; }
		public bool IsFading => FadeSeconds > 0;

		public void BeginFade(double seconds)
		{
			if (!IsFading)
			{
				FadeSeconds = seconds;
			}
		}

		// 正弦缓入：开头降得慢、结尾降得快，与原先补间的 Sine/EaseIn 同一条曲线。
		public float Gain => IsFading ? (float)Math.Cos(Math.Min(1.0, FadeElapsed / FadeSeconds) * Math.PI / 2) : 1f;

		public void Stop()
		{
			Handle.TryStop(allowFadeOut: false);
			Handle.Dispose();
		}
	}

	private static readonly List<Track> Tracks = [];
	private static Track? _current;
	private static ulong _lastTickUsec;
	private static bool _ticking;

	/// <summary>本模组正在接管 Boss 战音乐（从战斗开始到结束），包括一阶段倒下后的静音段。</summary>
	private static bool _active;

	public static void Install(Harmony harmony)
	{
		RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>(OnCombatStarting, replayCurrentState: false);
		RitsuLibFramework.SubscribeLifecycle<CombatEndedEvent>(OnCombatEnded, replayCurrentState: false);
		harmony.Patch(
			AccessTools.Method(typeof(NRunMusicController), nameof(NRunMusicController.StopMusic)),
			postfix: new HarmonyMethod(typeof(ReunionBossMusic), nameof(StopAll)));
	}

	/// <summary>爱国者一阶段倒下：淡出后静音，等重生（原版音乐不回来）。</summary>
	public static void FadeOutForRebirth()
	{
		if (_active)
		{
			FadeCurrent(RebirthFadeSeconds);
		}
	}

	/// <summary>爱国者重生为毁灭姿态：从头播放二阶段曲目。</summary>
	public static void PlayPhaseTwo()
	{
		if (_active)
		{
			Play(PhaseTwoTrackPath, PhaseTwoVolume);
		}
	}

	private static bool IsPatriotBoss(EncounterModel? encounter)
	{
		return encounter is ReunionPatriotBoss || encounter?.CanonicalInstance is ReunionPatriotBoss;
	}

	private static bool IsLivePatriotCombat(ICombatState? combatState)
	{
		return combatState is CombatState state && state.IsLiveCombat() && IsPatriotBoss(state.Encounter);
	}

	private static void OnCombatStarting(CombatStartingEvent e)
	{
		if (!IsLivePatriotCombat(e.CombatState))
		{
			return;
		}

		_active = true;
		// 与原版 PlayCustomMusic 相同，只对音乐代理（场景里的 Proxy 节点）调 stop_music，环境音继续播放。
		NRunMusicController.Instance?.GetNodeOrNull<Node>("Proxy")?.Call("stop_music");
		Play(PhaseOneTrackPath, PhaseOneVolume);
	}

	private static void OnCombatEnded(CombatEndedEvent e)
	{
		if (!_active || !IsLivePatriotCombat(e.CombatState))
		{
			return;
		}

		_active = false;
		FadeCurrent(VictoryFadeSeconds);
	}

	/// <summary>原版 StopMusic（阵亡、退出本局时调用）之后，立即停掉本模组的所有曲目。</summary>
	private static void StopAll()
	{
		_active = false;
		_current = null;
		foreach (Track track in Tracks)
		{
			track.Stop();
		}

		Tracks.Clear();
		SetTicking(false);
	}

	private static void FadeCurrent(double seconds)
	{
		_current?.BeginFade(seconds);
		_current = null;
	}

	private static void Play(string trackPath, float volume)
	{
		FadeCurrent(SwitchFadeSeconds);
		AudioMusicHandle? handle = GameAudioService.Shared.PlayMusic(
			AudioSource.StreamingMusic(trackPath),
			new AudioPlaybackOptions { Volume = volume * MusicBusVolume(), Scope = AudioLifecycleScope.Room });
		if (handle == null)
		{
			Log.Warn($"[{ModEntry.ModId}] Could not start boss music {trackPath}");
			return;
		}

		_current = new Track(handle, volume);
		Tracks.Add(_current);
		SetTicking(true);
	}

	private static float MusicBusVolume()
	{
		// 找不到总线（音频库没加载）时按 1 处理；RitsuLib 的 TryGetVolume 在这种情况下返回 0，直接用会把音乐静音。
		return FmodStudioBusAccess.TryGetBus(FmodStudioRouting.MusicBus) == null
			? 1f
			: Math.Max(0f, FmodStudioBusAccess.TryGetVolume(FmodStudioRouting.MusicBus));
	}

	private static void SetTicking(bool on)
	{
		if (on == _ticking || Engine.GetMainLoop() is not SceneTree tree)
		{
			return;
		}

		_ticking = on;
		_lastTickUsec = Time.GetTicksUsec();
		if (on)
		{
			tree.ProcessFrame += Tick;
		}
		else
		{
			tree.ProcessFrame -= Tick;
		}
	}

	private static void Tick()
	{
		ulong now = Time.GetTicksUsec();
		double delta = (now - _lastTickUsec) / 1_000_000.0;
		_lastTickUsec = now;
		float bus = MusicBusVolume();
		for (int i = Tracks.Count - 1; i >= 0; i--)
		{
			Track track = Tracks[i];
			if (track.Handle.IsReleased)
			{
				Tracks.RemoveAt(i);
				continue;
			}

			if (track.IsFading)
			{
				track.FadeElapsed += delta;
				if (track.FadeElapsed >= track.FadeSeconds)
				{
					track.Stop();
					Tracks.RemoveAt(i);
					continue;
				}
			}

			track.Handle.TrySetVolume(track.Volume * track.Gain * bus);
		}

		if (_current is { } current && !Tracks.Contains(current))
		{
			_current = null;
		}

		if (Tracks.Count == 0)
		{
			SetTicking(false);
		}
	}
}

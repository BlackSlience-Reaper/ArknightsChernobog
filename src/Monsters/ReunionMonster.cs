using ArknightsChernobog.Powers;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Scaffolding.Content;

namespace ArknightsChernobog.Monsters;

/// <summary>带 <see cref="ReunionHostRemnantPower"/> 的怪物（见 <see cref="ReunionHostMonster"/>）：被击倒后切到的“倒地”招式，其后续招式负责站起。</summary>
public interface IReunionRevivable
{
	MoveState DownedState { get; }
}

/// <summary>带 <see cref="ReunionPatriotRebirthPower"/> 的怪物：一阶段倒下时切到重生招式，重生招式负责换形态并移除该能力。</summary>
public interface IReunionRebirth
{
	Task BeginRebirth();
}

/// <summary>带 <see cref="ReunionHoverPower"/> 的怪物：悬浮层数扣完时由它自己处理坠落表现、击晕和之后的起飞。</summary>
public interface IReunionHovering
{
	Task Crash();
}

/// <summary>
/// 第七章整合运动敌人的公共部分（基于 RitsuLib 的 ModMonsterTemplate）：PRTS Spine 场景路径、音效与动画状态机的搭法。
/// 全部具体子类经 RitsuLib 注册（ModEntry），模型 id 形如 ARKNIGHTS_CHERNOBOG_MONSTER_REUNION_PATRIOT，
/// 存档、联机和控制台都按它引用，不要改类名。
/// </summary>
public abstract class ReunionMonster : ModMonsterTemplate
{
	public const string SceneRoot = $"res://{ModEntry.ModId}/scenes/creature_visuals/";

	// 原版默认音效路径按模型 id 拼出，模组 id 在 FMOD 里没有对应事件；这里只用原版代码里出现过的事件。
	protected const string MeleeHitSfx = "event:/sfx/enemy/enemy_attacks/fossil_stalker/fossil_stalker_attack_single";
	protected const string RangedHitSfx = "event:/sfx/enemy/enemy_attacks/turret_operator/turret_operator_attack";
	protected const string HeavyHitSfx = "event:/sfx/enemy/enemy_attacks/kaiser_crab/kaiser_crab_attack_slam";
	protected const string BombHitSfx = "event:/sfx/enemy/enemy_attacks/magi_knight/magi_knight_attack_bomb";
	protected const string ArtsHitSfx = "event:/sfx/enemy/enemy_attacks/lagavulin_matriarch/lagavulin_matriarch_cast";
	protected const string BuffSfx ="event:/sfx/enemy/enemy_attacks/punch_construct/punch_construct_buff";

	protected const string SlashVfx = "vfx/vfx_attack_slash";
	protected const string BluntVfx = "vfx/vfx_attack_blunt";
	protected const string HeavyBluntVfx = "vfx/vfx_heavy_blunt";
	protected const string BiteVfx = "vfx/vfx_bite";
	protected const string ArtsVfx = "vfx/vfx_attack_lightning";

	/// <summary>creature_visuals 场景文件名（不含扩展名），与 <see cref="SceneRoot"/> 拼成完整路径。</summary>
	public abstract string SceneName { get; }

	/// <summary>本模型用到的全部 Spine 动画名，自检按它核对 skel 里是否真的存在。</summary>
	public abstract IReadOnlyList<string> RequiredAnimations { get; }

	public override MonsterAssetProfile AssetProfile => new(VisualsScenePath: CreatureScenePath);

	/// <summary>生物场景路径：RitsuLib 按 AssetProfile 接到原版 VisualsPath；召唤型遭遇战也要把中途才召来的怪物场景列进预载。</summary>
	internal string CreatureScenePath => SceneRoot + SceneName + ".tscn";

	public override bool HasDeathSfx => false;

	/// <summary>
	/// 灾厄击杀前原版 DoomPower.PlayVfx 先问这一项：为真就把生物节点淡出并 QueueFree，之后才调 Kill。
	/// 不屈（爱国者一阶段）和残存（宿主）死后留在战斗里要重生、站起，节点被销毁就再也回不来，表现为灾厄打死后不进二阶段。
	/// 原版实验体用 ShouldDisappearFromDoom => Respawns >= 2 处理同一问题；这里直接问“死后是否移出战斗”的钩子，
	/// 与紧接着的 Kill 用同一个判断，新增同类能力时不用再改这里。
	/// </summary>
	public override bool ShouldDisappearFromDoom =>
		Creature.CombatState is not { } combatState || Hook.ShouldCreatureBeRemovedFromCombatAfterDeath(combatState, Creature);

	private int _openingOffset;

	/// <summary>
	/// 起手偏移：状态机建好后沿招式循环往后走这么多步作为第一招。由遭遇战在生成怪物时设置，用来错开
	/// 同场怪物的出手节奏、把开局伤害压到原版第二幕的区间（取值见 docs/tools/opening_sim.py）。
	/// 状态机在怪物加入战斗时才建立，晚于遭遇战生成怪物，所以在 ConfigureMonsters 里设置即可生效。
	/// </summary>
	public int OpeningOffset
	{
		get => _openingOffset;
		set
		{
			AssertMutable();
			_openingOffset = value;
		}
	}

	/// <summary>最近一次建立状态机时的起手招式 id，只供自检核对起手偏移。</summary>
	internal string? OpeningStateId { get; private set; }

	/// <summary>各怪物用它代替 new MonsterMoveStateMachine，以应用 <see cref="OpeningOffset"/>。</summary>
	protected MonsterMoveStateMachine Machine(IEnumerable<MonsterState> states, MonsterState initialState)
	{
		MonsterState start = initialState;
		for (int step = 0; step < OpeningOffset && start is MoveState { FollowUpState: { } next }; step++)
		{
			start = next;
		}

		return Build(states, start);
	}

	/// <summary>
	/// 随机出招（原版外骨骼虫、猎杀者的写法）：每招之后从 <paramref name="moves"/> 里等权随机挑一招，不连用同一招。
	/// 第一招不随机，取第 <see cref="OpeningOffset"/> 招，同场同种怪靠它错开，开局伤害也照旧由模拟器算。
	/// 随机数走原版 RandomBranchState 的怪物 AI 随机源，联机两端一致；下一招在上一回合末就定好，玩家看得到意图。
	/// </summary>
	protected MonsterMoveStateMachine RandomMachine(string branchId, IReadOnlyList<MoveState> moves, params MonsterState[] extraStates)
	{
		RandomBranchState loop = RandomLoop(branchId, moves);
		return Build([.. moves, loop, .. extraStates], RandomOpening(moves));
	}

	/// <summary><see cref="RandomMachine"/> 的随机分支本身：各招之后都回到它。需要自己拼状态机时（宿主的倒地链）单独用。</summary>
	protected static RandomBranchState RandomLoop(string branchId, IReadOnlyList<MoveState> moves)
	{
		RandomBranchState loop = new(branchId);
		foreach (MoveState move in moves)
		{
			loop.AddBranch(move, MoveRepeatType.CannotRepeat, 1f);
			move.FollowUpState = loop;
		}

		return loop;
	}

	/// <summary>随机出招怪的第一招：按 <see cref="OpeningOffset"/> 取。</summary>
	protected MoveState RandomOpening(IReadOnlyList<MoveState> moves)
	{
		return moves[OpeningOffset % moves.Count];
	}

	protected MonsterMoveStateMachine Build(IEnumerable<MonsterState> states, MonsterState start)
	{
		OpeningStateId = start.Id;
		return new MonsterMoveStateMachine(states, start);
	}

	/// <summary>往玩家弃牌堆塞状态牌（原版怪物的晕眩、伤口、灼伤都放弃牌堆）。</summary>
	protected static Task AddStatusCards<TCard>(IReadOnlyList<Creature> targets, int count)
		where TCard : CardModel
	{
		return CardPileCmd.AddToCombatAndPreview<TCard>(targets, PileType.Discard, count, null);
	}

	protected static int ToughValue(int ascension, int normal)
	{
		return AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, ascension, normal);
	}

	protected static int DeadlyValue(int ascension, int normal)
	{
		return AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, ascension, normal);
	}

	protected Task ApplyStrengthToSelf(int amount)
	{
		return PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Creature, amount, Creature, null);
	}

	protected Task GainBlock(int amount)
	{
		return CreatureCmd.GainBlock(Creature, amount, ValueProp.Move, null);
	}

	protected Task ApplyPower<TPower>(IEnumerable<Creature> targets, int amount)
		where TPower : PowerModel
	{
		return PowerCmd.Apply<TPower>(new ThrowingPlayerChoiceContext(), targets, amount, Creature, null);
	}

	protected async Task GiveBlock(IEnumerable<Creature> creatures, int amount)
	{
		foreach (Creature creature in creatures)
		{
			await CreatureCmd.GainBlock(creature, amount, ValueProp.Move, null);
		}
	}

	protected IReadOnlyList<Creature> LivingTeammates()
	{
		return CombatState.GetTeammatesOf(Creature).Append(Creature).Where(c => c.IsAlive).Distinct().ToList();
	}

	protected IReadOnlyList<Creature> OtherLivingTeammates()
	{
		return LivingTeammates().Where(c => c != Creature).ToList();
	}

	/// <summary>
	/// 普通宿主的“快速自然恢复”（PRTS 定位）：给自己挂 <see cref="ReunionPermanentRegenPower"/>，
	/// 每个敌方回合结束回复层数点生命且不衰减，拖得越久回得越多。狂暴宿主改用 <see cref="ApplyLossOfControl"/>。
	/// </summary>
	protected Task ApplyHostRegen(int amount)
	{
		return ApplyPower<ReunionPermanentRegenPower>([Creature], amount);
	}

	/// <summary>
	/// 狂暴宿主的“失控”（PRTS 定位：持续损失生命）：给自己挂原版“瓦解”（DisintegrationPower），每个敌方回合结束时按层数受到伤害，不会自然衰减。
	/// 伤害是 Unpowered 而非 Unblockable，回合内拿到的格挡（包括覆甲在回合结束前给的格挡）会先吃掉它，
	/// 所以宿主招式不给自己格挡，也不要和给格挡的单位编在一起指望它自损。
	/// </summary>
	protected Task ApplyLossOfControl(int amount)
	{
		return ApplyPower<DisintegrationPower>([Creature], amount);
	}

	/// <summary>
	/// 搭一个“待机循环 + 若干一次性动作回到待机”的状态机。PRTS 的 Default 是空姿势，待机一律用 Idle 系列。
	/// </summary>
	protected static CreatureAnimator BuildAnimator(
		MegaSprite controller,
		string idleAnimation,
		string deathAnimation,
		params (string Trigger, string Animation)[] actions)
	{
		return BuildAnimator(controller, null, idleAnimation, deathAnimation, actions);
	}

	/// <summary>同上，但开场先播 <paramref name="initialState"/>（例如空降兵悬空），之后的动作仍回到待机。</summary>
	protected static CreatureAnimator BuildAnimator(
		MegaSprite controller,
		AnimState? initialState,
		string idleAnimation,
		string deathAnimation,
		params (string Trigger, string Animation)[] actions)
	{
		AnimState idle = new(idleAnimation, isLooping: true);
		CreatureAnimator animator = new(initialState ?? idle, controller);
		animator.AddAnyState(CreatureAnimator.idleTrigger, idle);
		foreach ((string trigger, string animation) in actions)
		{
			animator.AddAnyState(trigger, new AnimState(animation) { NextState = idle });
		}

		animator.AddAnyState(CreatureAnimator.deathTrigger, new AnimState(deathAnimation));
		return animator;
	}
}

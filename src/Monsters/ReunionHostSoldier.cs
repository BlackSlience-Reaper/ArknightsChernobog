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
/// 宿主士兵（PRTS enemy_1043_zomsabr，资源目录 enemy_1043_zomsbr），梅菲斯特“牧群”的普通单位（7-2/7-3）。
/// PRTS 定位：被不明意识控制身体的士兵，能快速自然恢复生命。
/// 动画：Idle / Attack（OnAttack 0.4s）/ Die；Run_Loop 未用。
/// 招式与数值见 docs/战斗设计.md：永续再生 + 残存（见 <see cref="ReunionHostMonster"/>），劈砍、猛扑（两段）、溃烂撕咬（弃牌堆塞伤口）三招随机、不连用，站起后照常随机。
/// </summary>
public sealed class ReunionHostSoldier : ReunionHostMonster
{
	public const string HackMoveId = "HACK_MOVE";
	public const string LungeMoveId = "LUNGE_MOVE";
	public const string FesteringBiteMoveId = "FESTERING_BITE_MOVE";

	private const int LungeHits = 2;

	public override string SceneName => "reunion_host_soldier";

	public override IReadOnlyList<string> RequiredAnimations => ["Idle", "Attack", "Die"];

	public override int MinInitialHp => ToughValue(43, 40);

	public override int MaxInitialHp => ToughValue(47, 44);

	public override DamageSfxType TakeDamageSfxType => DamageSfxType.Fur;

	protected override int HostRegen => 2;

	protected override int ReviveHp => ToughValue(18, 16);

	private int HackDamage => DeadlyValue(10, 9);

	private int LungeDamage => DeadlyValue(6, 5);

	private int FesteringBiteDamage => DeadlyValue(7, 6);

	private const int FesteringBiteWounds = 1;

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		MoveState hack = new(HackMoveId, HackMove, new SingleAttackIntent(HackDamage));
		MoveState lunge = new(LungeMoveId, LungeMove, new MultiAttackIntent(LungeDamage, LungeHits));
		MoveState bite = new(FesteringBiteMoveId, FesteringBiteMove, new SingleAttackIntent(FesteringBiteDamage), new StatusIntent(FesteringBiteWounds));
		return HostRandomMachine("HOST_SOLDIER_RANDOM", [hack, lunge, bite]);
	}

	private async Task HackMove(IReadOnlyList<Creature> targets)
	{
		await DamageCmd.Attack(HackDamage)
			.FromMonster(this)
			.WithAttackerAnim(CreatureAnimator.attackTrigger, 0.4f)
			.WithHitFx(SlashVfx, MeleeHitSfx)
			.Execute(null);
	}

	private async Task FesteringBiteMove(IReadOnlyList<Creature> targets)
	{
		await DamageCmd.Attack(FesteringBiteDamage)
			.FromMonster(this)
			.WithAttackerAnim(CreatureAnimator.attackTrigger, 0.4f)
			.WithHitFx(BiteVfx, MeleeHitSfx)
			.Execute(null);
		await AddStatusCards<Wound>(targets, FesteringBiteWounds);
	}

	private async Task LungeMove(IReadOnlyList<Creature> targets)
	{
		await DamageCmd.Attack(LungeDamage).WithHitCount(LungeHits).OnlyPlayAnimOnce()
			.FromMonster(this)
			.WithAttackerAnim(CreatureAnimator.attackTrigger, 0.4f)
			.WithHitFx(SlashVfx, MeleeHitSfx)
			.Execute(null);
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller) => BuildHostAnimator(controller);
}

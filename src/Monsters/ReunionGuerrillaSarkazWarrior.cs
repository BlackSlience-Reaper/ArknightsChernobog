using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace ArknightsChernobog.Monsters;

/// <summary>
/// 游击队萨卡兹战士（PRTS enemy_1084_sotidm），精英近战。PRTS 定位：接触源石祭坛脉冲波后不受伤害、攻击转为法术伤害。
/// skel 有两套形态：Idle / Attack（OnAttack 0.533s）/ Die 与强化后的 Idle_2 / Attack_2（OnAttack 0.967s）/ Die_2。
/// 第一次“仪式强化”后切到第二套动画，之后的攻击、待机和死亡都走 _2。
/// 招式与数值见 docs/战斗设计.md（初版）：两次斩击后仪式强化，每轮叠一次力量。
/// </summary>
public sealed class ReunionGuerrillaSarkazWarrior : ReunionMonster
{
	public const string CleaveMoveId = "CLEAVE_MOVE";
	public const string RitualEmpowerMoveId = "RITUAL_EMPOWER_MOVE";

	private const string EmpowerTrigger = "Empower";

	private bool _isEmpowered;

	public override string SceneName => "reunion_guerrilla_sarkaz_warrior";

	public override IReadOnlyList<string> RequiredAnimations => ["Idle", "Attack", "Die", "Idle_2", "Attack_2", "Die_2"];

	public override int MinInitialHp => ToughValue(76, 72);

	public override int MaxInitialHp => ToughValue(80, 76);

	public override DamageSfxType TakeDamageSfxType => DamageSfxType.Armor;

	private int CleaveDamage => DeadlyValue(13, 12);

	private int EmpowerStrength => DeadlyValue(4, 3);

	/// <summary>是否已切到强化形态；动画状态机的条件分支读它，改动会立即影响下一次触发。</summary>
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
		MoveState cleave = new(CleaveMoveId, CleaveMove, new SingleAttackIntent(CleaveDamage));
		MoveState secondCleave = new(CleaveMoveId + "_2", CleaveMove, new SingleAttackIntent(CleaveDamage));
		MoveState empower = new(RitualEmpowerMoveId, RitualEmpowerMove, new BuffIntent());
		cleave.FollowUpState = secondCleave;
		secondCleave.FollowUpState = empower;
		empower.FollowUpState = cleave;
		return Machine([cleave, secondCleave, empower], cleave);
	}

	private async Task CleaveMove(IReadOnlyList<Creature> targets)
	{
		await DamageCmd.Attack(CleaveDamage)
			.FromMonster(this)
			.WithAttackerAnim(CreatureAnimator.attackTrigger, IsEmpowered ? 0.95f : 0.55f)
			.WithHitFx(IsEmpowered ? ArtsVfx : SlashVfx, MeleeHitSfx)
			.Execute(null);
	}

	private async Task RitualEmpowerMove(IReadOnlyList<Creature> targets)
	{
		SfxCmd.Play(BuffSfx);
		IsEmpowered = true;
		await CreatureCmd.TriggerAnim(Creature, EmpowerTrigger, 0.4f);
		await ApplyStrengthToSelf(EmpowerStrength);
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		AnimState idle = new("Idle", isLooping: true);
		AnimState idle2 = new("Idle_2", isLooping: true);
		AnimState attack = new("Attack") { NextState = idle };
		AnimState attack2 = new("Attack_2") { NextState = idle2 };

		// 同一触发器按注册顺序取第一个条件成立的分支，强化形态的分支必须先注册。
		CreatureAnimator animator = new(idle, controller);
		animator.AddAnyState(CreatureAnimator.idleTrigger, idle2, () => IsEmpowered);
		animator.AddAnyState(CreatureAnimator.idleTrigger, idle);
		animator.AddAnyState(EmpowerTrigger, idle2);
		animator.AddAnyState(CreatureAnimator.attackTrigger, attack2, () => IsEmpowered);
		animator.AddAnyState(CreatureAnimator.attackTrigger, attack);
		animator.AddAnyState(CreatureAnimator.deathTrigger, new AnimState("Die_2"), () => IsEmpowered);
		animator.AddAnyState(CreatureAnimator.deathTrigger, new AnimState("Die"));
		return animator;
	}
}

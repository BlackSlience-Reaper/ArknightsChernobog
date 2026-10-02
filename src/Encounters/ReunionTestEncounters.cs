using ArknightsChernobog.Monsters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Scaffolding.Content;

namespace ArknightsChernobog.Encounters;

/// <summary>
/// 只供控制台测试的遭遇战：不进任何幕的战斗池，也不是 ChernobogAct 的 Boss。
/// 原版 `fight &lt;id&gt;` 直接按 ModelDb.GetById 找遭遇战，模组遭遇战由 ModelDb 自动注册，
/// 所以 `fight reunion_patriot_test` 这类命令不需要补丁；但补全列表只来自各幕的遭遇战，这里的 id 不会出现在 Tab 补全里。
/// 类名即 id（ReunionPatriotTest → REUNION_PATRIOT_TEST），改名会让 README 里的测试命令失效。
/// </summary>
public abstract class ReunionTestEncounter : ReunionEncounter
{
}

public sealed class ReunionPatriotTest : ReunionTestEncounter
{
	public override RoomType RoomType => RoomType.Boss;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<ReunionPatriot>()];
}

public sealed class ReunionGuerrillaFighterTest : ReunionTestEncounter
{
	public override RoomType RoomType => RoomType.Monster;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<ReunionGuerrillaFighter>()];
}

public sealed class ReunionGuerrillaHoundTest : ReunionTestEncounter
{
	public override RoomType RoomType => RoomType.Monster;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<ReunionGuerrillaHound>(), M<ReunionGuerrillaHound>()];

	protected override void ConfigureMonsters(IReadOnlyList<MonsterModel> monsters) => SetOpenings(monsters, 0, 1);
}

public sealed class ReunionGuerrillaSniperTest : ReunionTestEncounter
{
	public override RoomType RoomType => RoomType.Monster;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<ReunionGuerrillaSniper>()];
}

public sealed class ReunionGuerrillaHeraldTest : ReunionTestEncounter
{
	public override RoomType RoomType => RoomType.Monster;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<ReunionGuerrillaHerald>()];
}

public sealed class ReunionGuerrillaShieldGuardTest : ReunionTestEncounter
{
	public override RoomType RoomType => RoomType.Elite;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<ReunionGuerrillaShieldGuard>()];
}

public sealed class ReunionGuerrillaMortarGunnerTest : ReunionTestEncounter
{
	public override RoomType RoomType => RoomType.Elite;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<ReunionGuerrillaMortarGunner>()];
}

public sealed class ReunionGuerrillaAssaulterTest : ReunionTestEncounter
{
	public override RoomType RoomType => RoomType.Elite;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<ReunionGuerrillaAssaulter>()];
}

public sealed class ReunionGuerrillaSarkazWarriorTest : ReunionTestEncounter
{
	public override RoomType RoomType => RoomType.Elite;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<ReunionGuerrillaSarkazWarrior>()];
}

public sealed class ReunionGuerrillaSarkazCasterTest : ReunionTestEncounter
{
	public override RoomType RoomType => RoomType.Elite;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<ReunionGuerrillaSarkazCaster>()];
}

/// <summary>战士 + 传令兵 + 狙击手，用来看传令兵给盟友加力量/格挡和三体站位。</summary>
public sealed class ReunionGuerrillaSquadTest : ReunionTestEncounter
{
	public override RoomType RoomType => RoomType.Monster;

	protected override IReadOnlyList<MonsterModel> Lineup =>
		[M<ReunionGuerrillaFighter>(), M<ReunionGuerrillaHerald>(), M<ReunionGuerrillaSniper>()];
}

/// <summary>萨卡兹战士 + 术师，看两套强化形态动画切换与双精英站位。</summary>
public sealed class ReunionSarkazRitualTest : ReunionTestEncounter
{
	public override RoomType RoomType => RoomType.Elite;

	protected override IReadOnlyList<MonsterModel> Lineup =>
		[M<ReunionGuerrillaSarkazWarrior>(), M<ReunionGuerrillaSarkazCaster>()];
}

// 游击队组长：头目版。组长只给同伴格挡（迫击炮兵组长、突袭战士组长），单独测试时这部分落空。

public sealed class ReunionGuerrillaHoundProTest : ReunionTestEncounter
{
	public override RoomType RoomType => RoomType.Monster;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<ReunionGuerrillaHoundPro>()];
}

public sealed class ReunionGuerrillaFighterLeaderTest : ReunionTestEncounter
{
	public override RoomType RoomType => RoomType.Monster;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<ReunionGuerrillaFighterLeader>()];
}

public sealed class ReunionGuerrillaSniperLeaderTest : ReunionTestEncounter
{
	public override RoomType RoomType => RoomType.Monster;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<ReunionGuerrillaSniperLeader>()];
}

/// <summary>传令兵组长单独出场：用与增援信号相同的槽位布局，前排和两个增援槽都空着，看召唤填位。</summary>
public sealed class ReunionGuerrillaHeraldLeaderTest : ReunionTestEncounter
{
	public override RoomType RoomType => RoomType.Monster;

	public override EncounterAssetProfile AssetProfile { get; } = new(EncounterScenePath: ChernobogAssets.EncounterScene("reunion_guerrilla_herald_leader_test"));

	public override IReadOnlyList<string> Slots => ReunionGuerrillaHeraldLeader.FormationSlots;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<ReunionGuerrillaHeraldLeader>()];

	protected override IReadOnlyList<string> LineupSlots => [ReunionGuerrillaHeraldLeader.HeraldSlot];
}

public sealed class ReunionGuerrillaShieldGuardLeaderTest : ReunionTestEncounter
{
	public override RoomType RoomType => RoomType.Elite;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<ReunionGuerrillaShieldGuardLeader>()];
}

public sealed class ReunionGuerrillaMortarGunnerLeaderTest : ReunionTestEncounter
{
	public override RoomType RoomType => RoomType.Elite;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<ReunionGuerrillaMortarGunnerLeader>()];
}

public sealed class ReunionGuerrillaAssaulterLeaderTest : ReunionTestEncounter
{
	public override RoomType RoomType => RoomType.Elite;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<ReunionGuerrillaAssaulterLeader>()];
}

public sealed class ReunionGuerrillaSarkazWarriorLeaderTest : ReunionTestEncounter
{
	public override RoomType RoomType => RoomType.Elite;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<ReunionGuerrillaSarkazWarriorLeader>()];
}

public sealed class ReunionGuerrillaSarkazCasterLeaderTest : ReunionTestEncounter
{
	public override RoomType RoomType => RoomType.Elite;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<ReunionGuerrillaSarkazCasterLeader>()];
}

// 宿主一族：单独测试时能看到“瓦解”每回合自损把宿主耗死。

public sealed class ReunionHostSoldierTest : ReunionTestEncounter
{
	public override RoomType RoomType => RoomType.Monster;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<ReunionHostSoldier>()];
}

public sealed class ReunionHostScavengerTest : ReunionTestEncounter
{
	public override RoomType RoomType => RoomType.Monster;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<ReunionHostScavenger>()];
}

public sealed class ReunionHostWandererTest : ReunionTestEncounter
{
	public override RoomType RoomType => RoomType.Monster;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<ReunionHostWanderer>()];
}

public sealed class ReunionHostSoldierLeaderTest : ReunionTestEncounter
{
	public override RoomType RoomType => RoomType.Monster;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<ReunionHostSoldierLeader>()];
}

public sealed class ReunionRagingHostSoldierTest : ReunionTestEncounter
{
	public override RoomType RoomType => RoomType.Monster;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<ReunionRagingHostSoldier>()];
}

public sealed class ReunionRagingHostThrowerTest : ReunionTestEncounter
{
	public override RoomType RoomType => RoomType.Monster;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<ReunionRagingHostThrower>()];
}

public sealed class ReunionRagingHostLeaderTest : ReunionTestEncounter
{
	public override RoomType RoomType => RoomType.Elite;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<ReunionRagingHostLeader>()];
}

/// <summary>宿主士兵 + 拾荒者 + 士兵组长，看牧群号令给同伴加力量与三只宿主各自“瓦解”自损。</summary>
public sealed class ReunionHostHerdTest : ReunionTestEncounter
{
	public override RoomType RoomType => RoomType.Monster;

	protected override IReadOnlyList<MonsterModel> Lineup =>
		[M<ReunionHostSoldier>(), M<ReunionHostSoldierLeader>(), M<ReunionHostScavenger>()];
}

// 整合运动特战。

public sealed class ReunionSpecOpsSoldierTest : ReunionTestEncounter
{
	public override RoomType RoomType => RoomType.Monster;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<ReunionSpecOpsSoldier>()];
}

public sealed class ReunionSpecOpsCasterTest : ReunionTestEncounter
{
	public override RoomType RoomType => RoomType.Monster;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<ReunionSpecOpsCaster>()];
}

public sealed class ReunionArtsMasterTest : ReunionTestEncounter
{
	public override RoomType RoomType => RoomType.Monster;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<ReunionArtsMaster>()];
}

/// <summary>特战士兵 + 特战术师 + 法术大师A1（7-2 同关），看三体站位与无人机的飞行高度。</summary>
public sealed class ReunionSpecOpsTeamTest : ReunionTestEncounter
{
	public override RoomType RoomType => RoomType.Monster;

	protected override IReadOnlyList<MonsterModel> Lineup =>
		[M<ReunionSpecOpsSoldier>(), M<ReunionSpecOpsCaster>(), M<ReunionArtsMaster>()];
}

// 萨卡兹雇佣军：偏单兵强度，不和游击队萨卡兹的仪式联动。

public sealed class ReunionMercenarySarkazWarriorTest : ReunionTestEncounter
{
	public override RoomType RoomType => RoomType.Elite;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<ReunionMercenarySarkazWarrior>()];
}

public sealed class ReunionMercenarySarkazCasterTest : ReunionTestEncounter
{
	public override RoomType RoomType => RoomType.Elite;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<ReunionMercenarySarkazCaster>()];
}

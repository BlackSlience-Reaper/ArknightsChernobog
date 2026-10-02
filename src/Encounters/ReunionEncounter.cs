using ArknightsChernobog.Monsters;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using STS2RitsuLib.Scaffolding.Content;

namespace ArknightsChernobog.Encounters;

/// <summary>
/// 整合运动遭遇战的公共部分，基于 RitsuLib 的 ModEncounterTemplate。阵容顺序即站位顺序（不给槽位名时原版按顺序自动排开，
/// 和 ChompersNormal 一样），近战放前、远程放后。固定阵容覆盖 <see cref="Lineup"/>；随机阵容（照原版碗虫、史莱姆）覆盖
/// <see cref="RollLineup"/> 与 <see cref="Candidates"/>。
/// 战斗池里的遭遇经 RitsuLib 按幕注册（ModEntry.ActEncounterTypes），模型 id 形如 ARKNIGHTS_CHERNOBOG_ENCOUNTER_&lt;类名&gt;；
/// 只供控制台测试或事件发起的遭遇不注册，保留原版按类名得出的 id。存档与 `fight` 命令都按 id 引用，不要改类名。
/// </summary>
public abstract class ReunionEncounter : ModEncounterTemplate
{
	/// <summary>固定阵容。</summary>
	protected virtual IReadOnlyList<MonsterModel> Lineup => [];

	/// <summary>随机阵容在这里抽；原版在生成怪物前已为本场播好种子（EncounterModel.Rng）。</summary>
	protected virtual IReadOnlyList<MonsterModel> RollLineup(Rng rng) => Lineup;

	/// <summary>本场可能出现的全部怪物（原版按它预载资源），随机阵容要列全。</summary>
	protected virtual IEnumerable<MonsterModel> Candidates => Lineup;

	public override IEnumerable<MonsterModel> AllPossibleMonsters => Candidates.Distinct();

	/// <summary>
	/// 阵容各怪的站位槽名，与阵容等长。只有要中途召唤的遭遇战需要（照原版卵翼虫）：同时覆盖 Slots 与 AssetProfile 的
	/// EncounterScenePath（assets/scenes/encounters/ 下放同名 Marker2D 的场景，模板据此把 HasScene 置真）。
	/// 预留的空槽由召唤按 EncounterModel.GetNextSlot 填上，已有怪物不移动。null 表示不用槽位，原版按判定框宽度自动排开。
	/// </summary>
	protected virtual IReadOnlyList<string>? LineupSlots => null;

	protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() => CreateMonstersWithSlots(Rng);

	/// <summary><see cref="CreateMonsters"/> 再配上 <see cref="LineupSlots"/>；自检也用它核对槽位。</summary>
	internal IReadOnlyList<(MonsterModel Monster, string? Slot)> CreateMonstersWithSlots(Rng rng)
	{
		IReadOnlyList<string>? slots = LineupSlots;
		return CreateMonsters(rng).Select((monster, i) => (monster, slots?[i])).ToList();
	}

	/// <summary>阵容 → 可变怪物 → <see cref="ConfigureMonsters"/>。自检直接调用它（给定种子），不需要进行中的对局。</summary>
	internal IReadOnlyList<MonsterModel> CreateMonsters(Rng rng)
	{
		List<MonsterModel> monsters = RollLineup(rng).Select(monster => monster.ToMutable()).ToList();
		ConfigureMonsters(monsters);
		return monsters;
	}

	/// <summary>在状态机建立前调整可变怪物（例如错开同种怪的开场招式）。</summary>
	protected virtual void ConfigureMonsters(IReadOnlyList<MonsterModel> monsters)
	{
	}

	/// <summary>按阵容顺序设置各怪的起手偏移（招式循环里往后走几步作为第一招），取值由 docs/tools/opening_sim.py 算出。</summary>
	protected static void SetOpenings(IReadOnlyList<MonsterModel> monsters, params int[] offsets)
	{
		for (int i = 0; i < offsets.Length; i++)
		{
			((ReunionMonster)monsters[i]).OpeningOffset = offsets[i];
		}
	}

	/// <summary>随机阵容按种类设置起手偏移（模拟器对随机阵容也是按种类挑的）；没列出的种类从第一招开始。</summary>
	protected static void SetOpeningsByType(IReadOnlyList<MonsterModel> monsters, params (Type Monster, int Offset)[] offsets)
	{
		foreach (MonsterModel monster in monsters)
		{
			foreach ((Type type, int offset) in offsets)
			{
				if (monster.GetType() == type)
				{
					((ReunionMonster)monster).OpeningOffset = offset;
				}
			}
		}
	}

	protected static MonsterModel M<TMonster>()
		where TMonster : MonsterModel
	{
		return ModelDb.Monster<TMonster>();
	}
}

/// <summary>
/// 遭遇战标签：原版抽下一场时避开与上一场同标签的遭遇战（ActModel.AddWithoutRepeatingTags），
/// 弱怪版和普通版同一群怪打同一个标签，就不会前后脚连着遇到。原版 EncounterTag 是封闭枚举，
/// 这里用枚举范围外的值；原版只拿它做相等比较（EncounterModel.SharesTagsWith），不存档、不显示。
/// </summary>
internal static class ReunionEncounterTags
{
	public const EncounterTag Hounds = (EncounterTag)7001;
	public const EncounterTag Hosts = (EncounterTag)7002;
	public const EncounterTag Paratroopers = (EncounterTag)7003;
	public const EncounterTag SpecOps = (EncounterTag)7004;
}

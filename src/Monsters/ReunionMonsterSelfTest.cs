using ArknightsChernobog.Encounters;
using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Random;

namespace ArknightsChernobog.Monsters;

/// <summary>
/// 整合运动敌人的 headless 自检（由 <see cref="ChernobogSelfTest"/> 调用）：模型与测试遭遇战已注册、
/// 本地化键存在、creature_visuals 场景能实例化、Spine 骨架真正加载（版本不对时 find_animation 全部为空）、
/// 图集贴图非空、代码里用到的动画名都在 skel 里。场景缩放、站位和动作时机仍需实机看。
/// </summary>
internal static class ReunionMonsterSelfTest
{
	public static IEnumerable<Type> MonsterTypes => ConcreteSubtypes(typeof(ReunionMonster));

	public static IEnumerable<Type> EncounterTypes => ConcreteSubtypes(typeof(ReunionTestEncounter));

	public static string CheckMonster(Type type)
	{
		ReunionMonster canonical = (ReunionMonster)ModelDb.GetById<MonsterModel>(ModelDb.GetId(type));
		string entry = canonical.Id.Entry;
		Require(canonical.Title.Exists(), $"missing monsters loc {entry}.name");

		MonsterModel mutable = canonical.ToMutable();
		mutable.SetUpForCombat();
		List<string> moves = [];
		foreach (MonsterState state in mutable.MoveStateMachine!.States.Values)
		{
			if (state is not MoveState || !state.Id.EndsWith("_MOVE", StringComparison.Ordinal))
			{
				continue;
			}

			// 与原版图鉴一致：招式名键去掉 _MOVE 后缀；_2 这类重复状态不查。
			string moveKey = state.Id[..^"_MOVE".Length];
			Require(LocString.Exists("monsters", $"{entry}.moves.{moveKey}.title"), $"missing monsters loc {entry}.moves.{moveKey}.title");
			moves.Add(moveKey);
		}

		string scenePath = ReunionMonster.SceneRoot + canonical.SceneName + ".tscn";
		PackedScene? scene = ResourceLoader.Load<PackedScene>(scenePath);
		Require(scene != null, $"cannot load {scenePath}");
		Node root = scene!.Instantiate();
		try
		{
			Node visuals = root.GetNode("%Visuals");
			Require(visuals.GetClass() == "SpineSprite", $"%Visuals is {visuals.GetClass()}");
			foreach (string marker in new[] { "%Bounds", "%IntentPos", "%CenterPos", "%OrbPos", "%TalkPos" })
			{
				Require(root.GetNodeOrNull(marker) != null, $"missing {marker}");
			}

			GodotObject? skeletonData = visuals.Get("skeleton_data_res").AsGodotObject();
			Require(skeletonData != null, "SpineSprite has no skeleton_data_res");
			Require(skeletonData!.Call("is_skeleton_data_loaded").AsBool(), "skeleton data not loaded (Spine version mismatch?)");

			GodotObject? atlas = skeletonData.Get("atlas_res").AsGodotObject();
			Require(atlas != null, "skeleton data has no atlas_res");
			Godot.Collections.Array textures = atlas!.Call("get_textures").AsGodotArray();
			Require(textures.Count > 0 && textures.All(t => t.AsGodotObject() is Texture2D { } tex && tex.GetWidth() > 0),
				$"atlas textures missing ({textures.Count})");

			MegaSkeletonDataResource data = new(skeletonData);
			List<string> missing = canonical.RequiredAnimations.Where(anim => !data.HasAnimation(anim)).ToList();
			Require(missing.Count == 0, $"animations not in skel: {string.Join(", ", missing)}");

			return $"{entry}: moves=[{string.Join(", ", moves)}], anims=[{string.Join(", ", canonical.RequiredAnimations)}], "
				+ $"textures={textures.Count}, hp={canonical.MinInitialHp}-{canonical.MaxInitialHp}";
		}
		finally
		{
			root.Free();
		}
	}

	public static string CheckEncounter(Type type)
	{
		// 与原版 FightConsoleCmd 相同的查法：分类 + 大写条目。
		string entry = ModelDb.GetId(type).Entry;
		EncounterModel encounter = ModelDb.GetById<EncounterModel>(new ModelId(ModelId.SlugifyCategory<EncounterModel>(), entry.ToUpperInvariant()));
		Require(encounter.Title.Exists(), $"missing encounters loc {entry}.title");
		Require(!ModelDb.AllEncounters.Contains(encounter), $"{entry} leaked into an act encounter pool");
		// 同一场里的同种怪起手必须错开（和正式战斗池同一条规则）。
		List<string> openings = ((ReunionEncounter)encounter).CreateMonsters(new Rng(1)).Cast<ReunionMonster>().Select(monster =>
		{
			monster.SetUpForCombat();
			return $"{monster.Id.Entry}:{monster.OpeningStateId}";
		}).ToList();
		Require(openings.Count == openings.Distinct().Count(), $"duplicate openings: {string.Join(" + ", openings)}");
		return $"fight {entry.ToLowerInvariant()} -> {encounter.RoomType}: {string.Join(" + ", openings)}";
	}

	private static IEnumerable<Type> ConcreteSubtypes(Type parent)
	{
		return typeof(ReunionMonsterSelfTest).Assembly.GetTypes()
			.Where(t => !t.IsAbstract && t.IsSubclassOf(parent))
			.OrderBy(t => t.Name, StringComparer.Ordinal);
	}

	private static void Require(bool condition, string detail)
	{
		if (!condition)
		{
			throw new InvalidOperationException(detail);
		}
	}
}

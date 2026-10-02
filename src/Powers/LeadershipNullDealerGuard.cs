using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ArknightsChernobog.Powers;

/// <summary>
/// 原版领导力（传令兵与传令兵头目会给自己上）在伤害修正里先比较 <c>Owner.Side != dealer.Side</c>，之后才判断是不是攻击伤害，
/// 没有对 dealer 判空。原版怪物都不带领导力，原版也就从没遇到过“场上有领导力 + 无来源伤害”的组合；
/// 但其他模组常用 dealer 为空的伤害（如海克斯的腐化之心、灼烧），一结算就在这里抛空引用。
/// 这个异常出在出牌后钩子里，会让原版跳过把牌移出出牌区，表现为牌悬在半空、此后每张牌都报错。
/// 无来源的伤害本就不属于“友方造成的攻击”，这里直接按 0 处理，与原版对非攻击伤害的结果一致；两端结算相同，联机安全。
/// </summary>
internal static class LeadershipNullDealerGuard
{
	public static void Install(Harmony harmony)
	{
		harmony.Patch(
			AccessTools.Method(typeof(LeadershipPower), nameof(LeadershipPower.ModifyDamageAdditive)),
			prefix: new HarmonyMethod(typeof(LeadershipNullDealerGuard), nameof(Prefix)));
	}

	private static bool Prefix(Creature? dealer, ref decimal __result)
	{
		if (dealer != null)
		{
			return true;
		}

		__result = 0m;
		return false;
	}
}

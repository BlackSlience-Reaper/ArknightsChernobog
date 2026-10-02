using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Scaffolding.Content;

namespace ArknightsChernobog.Powers;

/// <summary>
/// 残甲：爱国者行军姿态的铠甲（PRTS：行军姿态防御力与法术抗性大幅提升；“年久失修的铠甲多处破损”）。
/// 受到的每次伤害固定减少层数点，层数不因受击衰减，只随爱国者的招式涨落（行军加固、盾击崩落，见 <see cref="Monsters.ReunionPatriot"/>）。
/// 写法照集成战略事件里博卓卡斯替的圣卫盾：减伤走 ModifyDamageAdditive（原版先加减再乘易伤等倍率，最后不低于 0），
/// 卡牌上的伤害预览直接显示减后的数；只跳过 Unpowered（荆棘、瓦解之类的无源伤害），攻击以外的有源伤害也减。
/// 爱国者一阶段倒下时原版会移除它，毁灭姿态不带甲。
/// </summary>
public sealed class ReunionPatriotArmorPower : ModPowerTemplate
{
	public override PowerAssetProfile AssetProfile => ChernobogAssets.PowerIcon("reunion_patriot_armor_power");

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
	{
		if (target != Owner || amount <= 0m || Amount <= 0 || props.HasFlag(ValueProp.Unpowered))
		{
			return 0m;
		}

		return -Math.Min(amount, Amount);
	}
}

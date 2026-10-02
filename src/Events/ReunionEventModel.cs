using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Scaffolding.Content;

namespace ArknightsChernobog.Events;

/// <summary>
/// 本幕事件的基类，写法照集成战略事件（RitsuLib 的 ModEventTemplate + 一组选项/效果助手），只留本幕用到的部分。
/// - 事件经 RitsuLib 注册（ModEntry），模型 id 带模组前缀，本地化键按真实 Id.Entry 写在 localization/*/events.json。
/// - 选项文本里的数值直接写在本地化里（与集成战略事件一致），改数值要两边同步。
/// - 立绘在 res://ArknightsChernobog/images/events/&lt;文件名&gt;，由 art/process_generated.py 规整到原版 3440×1616。
/// </summary>
public abstract class ReunionEventModel : ModEventTemplate
{
	protected const string InitialPage = "INITIAL";

	private const string PortraitRoot = $"res://{ModEntry.ModId}/images/events/";

	/// <summary>立绘文件名（不含目录）。</summary>
	protected abstract string PortraitFile { get; }

	public override string? CustomInitialPortraitPath => PortraitRoot + PortraitFile;

	public override bool IsShared => false;

	protected Player OwnerOrThrow => Owner ?? throw new InvalidOperationException($"{GetType().Name} has no owner.");

	/// <summary>原版按 id 拼的立绘路径不存在，预载时换成自己的，否则进事件前报“资源未预载”。</summary>
	public override IEnumerable<string> GetAssetPaths(IRunState runState)
	{
		IEnumerable<string> original = base.GetAssetPaths(runState);
		if (TestMode.IsOn)
		{
			return original;
		}

		string vanillaPortrait = $"res://images/events/{Id.Entry.ToLowerInvariant()}.png";
		return original.Where(path => !string.Equals(path, vanillaPortrait, StringComparison.Ordinal))
			.Append(CustomInitialPortraitPath!).Distinct(StringComparer.Ordinal).ToArray();
	}

	// ---- 选项 ----

	protected EventOption Choice(Func<Task> onChosen, string optionKey, string pageKey = InitialPage)
	{
		return new EventOption(this, onChosen, $"{Id.Entry}.pages.{pageKey}.options.{optionKey}");
	}

	/// <summary>onChosen 为 null 即锁定选项（原版 ZenWeaver 同款）。</summary>
	protected EventOption LockedChoice(string optionKey, string pageKey = InitialPage)
	{
		return new EventOption(this, null, $"{Id.Entry}.pages.{pageKey}.options.{optionKey}");
	}

	protected EventOption CardPreviewChoice<TCard>(Func<Task> onChosen, string optionKey, string pageKey = InitialPage)
		where TCard : CardModel
	{
		EventOption option = Choice(onChosen, optionKey, pageKey);
		option.HoverTips = HoverTipFactory.FromCardWithCardHoverTips<TCard>();
		return option;
	}

	protected EventOption GoldChoice(int cost, Func<Task> onChosen, string optionKey, string lockedOptionKey)
	{
		return OwnerOrThrow.Gold >= cost ? Choice(onChosen, optionKey) : LockedChoice(lockedOptionKey);
	}

	/// <summary>扣血选项：血量不够扣完还剩 1 点时锁定。</summary>
	protected EventOption HpChoice(int hpLoss, Func<Task> onChosen, string optionKey, string lockedOptionKey)
	{
		return OwnerOrThrow.Creature.CurrentHp > hpLoss
			? Choice(onChosen, optionKey).ThatDoesDamage(hpLoss)
			: LockedChoice(lockedOptionKey);
	}

	protected void ShowPage(string pageKey, IReadOnlyList<EventOption> options)
	{
		SetEventState(PageDescription(pageKey), options);
	}

	protected void Finish(string pageKey)
	{
		SetEventFinished(PageDescription(pageKey));
	}

	/// <summary>进入事件战斗、打完直接离开（原版灯笼钥匙同款）。原版要求事件是共享事件。</summary>
	protected Task EnterEventCombat<TEncounter>(IReadOnlyList<Reward> extraRewards)
		where TEncounter : EncounterModel
	{
		EnterCombatWithoutExitingEvent<TEncounter>(extraRewards, shouldResumeAfterCombat: false);
		return Task.CompletedTask;
	}

	// ---- 生命与金币 ----

	protected Task Heal(int amount) => CreatureCmd.Heal(OwnerOrThrow.Creature, amount);

	protected Task LoseHp(int amount)
	{
		return CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), OwnerOrThrow.Creature, amount, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
	}

	protected Task GainMaxHp(int amount) => CreatureCmd.GainMaxHp(OwnerOrThrow.Creature, amount);

	protected Task LoseMaxHp(int amount)
	{
		return CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), OwnerOrThrow.Creature, amount, isFromCard: false);
	}

	protected Task GainGold(int amount) => PlayerCmd.GainGold(amount, OwnerOrThrow);

	protected Task SpendGold(int amount) => PlayerCmd.LoseGold(amount, OwnerOrThrow, GoldLossType.Spent);

	// ---- 牌组 ----

	protected async Task UpgradeCards(int count)
	{
		List<CardModel> cards = (await CardSelectCmd.FromDeckForUpgrade(OwnerOrThrow, new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, count))).ToList();
		CardCmd.Upgrade(cards, CardPreviewStyle.MessyLayout);
		if (cards.Count > 0)
		{
			await Cmd.CustomScaledWait(0.4f, 0.8f);
		}
	}

	protected async Task RemoveCards(int count)
	{
		List<CardModel> cards = (await CardSelectCmd.FromDeckForRemoval(OwnerOrThrow,
			new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, count) { RequireManualConfirmation = true })).ToList();
		await CardPileCmd.RemoveFromDeck(cards);
	}

	protected async Task TransformCards(int count)
	{
		Player owner = OwnerOrThrow;
		List<CardModel> cards = (await CardSelectCmd.FromDeckForTransformation(owner, new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, count))).ToList();
		foreach (CardModel card in cards)
		{
			await CardCmd.TransformToRandom(card, owner.RunState.Rng.Niche, CardPreviewStyle.EventLayout);
		}
	}

	protected bool HasRemovableCards(int count) => PileType.Deck.GetPile(OwnerOrThrow).Cards.Count(card => card.IsRemovable) >= count;

	protected bool HasTransformableCards(int count) =>
		PileType.Deck.GetPile(OwnerOrThrow).Cards.Count(card => card.Type != CardType.Quest && card.IsTransformable) >= count;

	protected Task AddCurse<TCurse>()
		where TCurse : CardModel
	{
		return CardPileCmd.AddCursesToDeck([ModelDb.Card<TCurse>()], OwnerOrThrow);
	}

	// ---- 奖励 ----

	/// <summary>本职业卡池里按条件筛的卡牌奖励（三选一等）。</summary>
	protected Task OfferCardReward(int optionCount, CardRarityOddsType odds, Func<CardModel, bool>? filter = null)
	{
		Player owner = OwnerOrThrow;
		CardCreationOptions options = filter == null
			? new CardCreationOptions([owner.Character.CardPool], CardCreationSource.Other, odds)
			: new CardCreationOptions([owner.Character.CardPool], CardCreationSource.Other, odds, filter);
		return RewardsCmd.OfferCustom(owner, [new CardReward(options, optionCount, owner)]);
	}

	protected Task OfferRandomPotions(int count)
	{
		Player owner = OwnerOrThrow;
		List<PotionModel> pool = PotionFactory.GetPotionOptions(owner).ToList();
		List<Reward> rewards = [];
		for (int i = 0; i < count && pool.Count > 0; i++)
		{
			PotionModel potion = owner.PlayerRng.Rewards.NextItem(pool)!;
			rewards.Add(new PotionReward(potion.ToMutable(), owner));
		}

		return RewardsCmd.OfferCustom(owner, rewards);
	}

	protected Task ObtainRandomRelic()
	{
		Player owner = OwnerOrThrow;
		return RelicCmd.Obtain(RelicFactory.PullNextRelicFromFront(owner).ToMutable(), owner);
	}
}

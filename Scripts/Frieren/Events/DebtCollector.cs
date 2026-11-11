using BaseLib.Utils;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Enchantments;
using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;

namespace FrierenMod.Scripts.Events;

public sealed class DebtCollector : FrierenStoryEvent
{
    public static readonly SavedSpireField<Player,bool> Settled = new(() => false,"FrierenDebtSettled");
    protected override string[] Portraits => ["DebtCollector"];
    public override bool IsAllowed(IRunState run) => base.IsAllowed(run) && run.CurrentActIndex > 0 &&
        run.Players.Any(p => !Settled.Get(p));
    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        if(Settled.Get(Owner!))
            return [Choice("LEAVE",()=>{Finish("LEAVE",0);return Task.CompletedTask;})];
        int debt = DebtStore.Read(Owner!);
        var repay = new EventOption(this,debt > 0 && Owner!.Gold >= debt && Owner.Deck.Cards.Any(c => c is HonestLiving) ? Repay : null,
            $"{Id.Entry}.pages.INITIAL.options.REPAY",HoverTipFactory.FromEnchantment<LoadedDice>());
        repay.Description.Add("Debt",debt);
        var avoid=CardChoice<HonestLiving>("DEFAULT",Default,Owner!.Deck.Cards.Any(c => c.Type == CardType.Attack && c.IsTransformable));
        avoid.HoverTips=avoid.HoverTips.Concat(HoverTipFactory.FromCardWithCardHoverTips<Debt>());
        return [Choice("INNOCENT",Innocent,debt == 0),repay,avoid];
    }
    private async Task Innocent() { await PlayerCmd.GainGold(Rng.NextInt(100,126),Owner!); Finish("INNOCENT",0); }
    private async Task Repay()
    {
        var cards = Owner!.Deck.Cards.Where(c => c is HonestLiving).ToArray();
        var chosen = (await CardSelectCmd.FromSimpleGrid(new BlockingPlayerChoiceContext(),cards,Owner,
            new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt,1))).SingleOrDefault();
        int debt = DebtStore.Read(Owner);
        if (chosen is not HonestLiving || debt <= 0 || Owner.Gold < debt) return;
        await PlayerCmd.LoseGold(debt,Owner,GoldLossType.Spent); DebtStore.Add(Owner,-debt);
        CardCmd.ClearEnchantment(chosen); CardCmd.Enchant<LoadedDice>(chosen,1);
        Settled.Set(Owner,true); Finish("REPAY",0);
    }
    private async Task Default()
    {
        var cards = Owner!.Deck.Cards.Where(c => c.Type == CardType.Attack && c.IsTransformable).ToArray();
        var chosen = (await CardSelectCmd.FromSimpleGrid(new BlockingPlayerChoiceContext(),cards,Owner,
            new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt,1))).SingleOrDefault();
        if (chosen == null) return;
        await CardCmd.TransformTo<HonestLiving>(chosen,CardPreviewStyle.EventLayout);
        await CardPileCmd.AddCurseToDeck<Debt>(Owner); Finish("DEFAULT",0);
    }
}

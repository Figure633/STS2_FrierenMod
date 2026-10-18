using FrierenMod.Core;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Cards;

public sealed class FolkGrimoire() : FrierenCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override void AddExtraArgsToDescription(LocString description) => description.Add("Discount", IsUpgraded ? 2 : 1);
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var chants = ModelDb.CardPool<FrierenMod.Scripts.Characters.FrierenCardPool>().AllCards
            .Where(c => c is IChantCard && c.Rarity != CardRarity.Token).OrderBy(c => c.Id.Entry).ToArray();
        var chant = CombatState!.CreateCard(chants[Owner.RunState.Rng.CombatCardGeneration.NextInt(chants.Length)], Owner);
        foreach (var card in new[] { chant, LegendaryCards.CreateRandom(Owner) })
        {
            card.EnergyCost.AddThisTurn(IsUpgraded ? -2 : -1);
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
        }
    }
}

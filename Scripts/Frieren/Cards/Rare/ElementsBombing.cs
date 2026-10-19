using FrierenMod.Core;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Cards;

public sealed class ElementsBombing() : FrierenCard(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var hand = Owner.PlayerCombatState!.Hand.Cards.Where(c => c != this).ToArray();
        foreach (var c in hand) await CardCmd.Exhaust(context, c);
        foreach (var c in hand)
        {
            int roll = Owner.RunState.Rng.CombatCardGeneration.NextInt(101);
            Type type = roll < 25 ? typeof(Ice) : roll < 50 ? typeof(Fire) : roll < 75 ? typeof(Thunder) : roll < 90 ? typeof(Dark) : typeof(Light);
            var element = CombatState!.CreateCard(ModelDb.GetById<CardModel>(ModelDb.GetId(type)), Owner);
            if (IsUpgraded) element.UpgradeInternal();
            await CardPileCmd.AddGeneratedCardToCombat(element, PileType.Hand, Owner);
        }
    }
}

using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace FrierenMod.Scripts.Cards;

public sealed class ProspectingSpell() : NormalChantCard(0,CardRarity.Uncommon,1)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Intensity",1),new DynamicVar("Choices",2)];
    protected override async Task OnPlay(PlayerChoiceContext context,CardPlay play)
    {
        await ChantCmd.Choose(context,play,Intensity);
        var cards=CardFactory.GetDistinctForCombat(Owner,
            ModelDb.CardPool<ColorlessCardPool>().GetUnlockedCards(Owner.UnlockState,Owner.RunState.CardMultiplayerConstraint),
            DynamicVars["Choices"].IntValue,Owner.RunState.Rng.CombatCardGeneration).ToArray();
        var selected=await CardSelectCmd.FromChooseACardScreen(context,cards,Owner);
        if(selected!=null)await CardPileCmd.AddGeneratedCardToCombat(selected,PileType.Hand,Owner);
    }
    protected override void OnUpgrade()=>DynamicVars["Choices"].UpgradeValueBy(1);
}

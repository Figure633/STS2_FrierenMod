using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Cards;

public sealed class RapidChant() : FrierenCard(0, CardType.Skill, CardRarity.Common, TargetType.Self), IChantCard
{
    protected override bool IsPlayable => Owner != null && ChantCmd.Options(Owner, 1).Count > 0;
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await ChantCmd.Choose(context, play, 1);
        await ManaCmd.Generate(Owner, PileType.Draw, 1);
    }
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}

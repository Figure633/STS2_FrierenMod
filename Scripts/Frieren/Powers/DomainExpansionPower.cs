using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace FrierenMod.Scripts.Powers;

public sealed class DomainExpansionPower : FrierenPower
{
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player != Owner.Player || player.Creature.HasPower<BanManaGainPower>()) return;
        for (int i = 0; i < Amount; i++) await CardFlowCmd.Generate<HighDensityMana>(player, PileType.Hand);
    }
}

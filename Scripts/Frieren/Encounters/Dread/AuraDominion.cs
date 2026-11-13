using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Dread;

namespace FrierenMod.Scripts.Encounters.Dread;

public sealed class AuraDominion : DominionModel, ICustomModel
{
    public override Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player) =>
        player.PlayerCombatState?.TurnNumber == 3 && player.Creature.IsAlive
            ? PlayerCmd.LoseEnergy(2, player)
            : Task.CompletedTask;
}

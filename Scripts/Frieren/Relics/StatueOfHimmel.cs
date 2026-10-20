using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace FrierenMod.Scripts.Relics;

public sealed class StatueOfHimmel : CountingFrierenRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;
    public override Task BeforeCombatStart() { Counter = 0; return Task.CompletedTask; }
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player != Owner) return;
        Counter++;
        if (Counter > 3 || Owner.Creature.HasPower<ArtifactPower>()) return;
        Flash(); await PowerCmd.Apply<ArtifactPower>(context, Owner.Creature, 1, Owner.Creature, null);
    }
    public override Task AfterCombatEnd(CombatRoom room) { Counter = 0; return Task.CompletedTask; }
}

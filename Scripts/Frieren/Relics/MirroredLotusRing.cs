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

public sealed class MirroredLotusRing : CountingFrierenRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;
    public override Task BeforeCombatStart() { Counter = 0; Status = RelicStatus.Normal; return Task.CompletedTask; }
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player != Owner) return;
        Counter++;
        if (Counter != 4) return;
        Flash(); await CardFlowCmd.Generate<Himmel>(Owner, PileType.Hand); Status = RelicStatus.Disabled;
    }
    public override Task AfterCombatEnd(CombatRoom room) { Counter = 0; Status = RelicStatus.Normal; return Task.CompletedTask; }
}

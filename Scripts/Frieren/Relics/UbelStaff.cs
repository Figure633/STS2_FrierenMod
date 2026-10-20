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

public sealed class UbelStaff : CountingFrierenRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    protected override void UpdateDisplay()
    {
        Status = Counter == 5 ? RelicStatus.Active : RelicStatus.Normal;
        base.UpdateDisplay();
    }
    public override async Task BeforeCardPlayed(CardPlay play)
    {
        if (play.Card.Owner != Owner || !ChantCmd.IsChant(play.Card)) return;
        Counter++;
        if (Counter >= 6) { Counter = 0; Flash(); await PlayerCmd.GainEnergy(1, Owner); }
    }
}

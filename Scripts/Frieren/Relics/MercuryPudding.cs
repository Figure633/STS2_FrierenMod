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

public sealed class MercuryPudding : FrierenRelic
{
    private bool _used;
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    [SavedProperty]
    public bool Used {
        get => _used;
        set { AssertMutable(); _used = value; Status = value ? RelicStatus.Disabled : RelicStatus.Normal; }
    }
    public override bool IsUsedUp => Used;
    public override async Task AfterCombatVictory(CombatRoom room)
    {
        if (Used || Owner.Creature.IsDead || Owner.Creature.CurrentHp > 10) return;
        Used = true; Flash();
        await CreatureCmd.Heal(Owner.Creature, decimal.Floor(Owner.Creature.MaxHp / 2m));
    }
}

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

public sealed class ImmovableCloak : FrierenRelic
{
    private bool _lostHp;
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    public override Task BeforeCombatStart() { _lostHp = false; Status = RelicStatus.Active; return Task.CompletedTask; }
    public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature == Owner.Creature && delta < 0) { _lostHp = true; Status = RelicStatus.Disabled; }
        return Task.CompletedTask;
    }
    public override decimal ModifyHandDraw(Player player, decimal count) => player == Owner && !_lostHp ? count + 1 : count;
    public override Task AfterCombatEnd(CombatRoom room) { Status = RelicStatus.Normal; return Task.CompletedTask; }
}

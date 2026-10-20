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

public sealed class HaitaaWand : FrierenRelic
{
    // 一代角色切换原型使用的初始遗物，不混入普通掉落。
    public override RelicRarity Rarity => RelicRarity.Starter;
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player != Owner) return;
        Flash(); await PowerCmd.Apply<ConcentrationPower>(context, Owner.Creature, 1, Owner.Creature, null);
    }
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || !participants.Contains(Owner.Creature)) return;
        var mana = Enum.GetValues<ManaSlot>().SelectMany(slot => ManaCmd.InPile(Owner, ManaCmd.Pile(slot))).ToArray();
        if (mana.Length == 0) return;
        Flash();
        foreach (var card in mana) await CardCmd.Exhaust(context, card);
        await PowerCmd.Apply<ConcentrationPower>(context, Owner.Creature, mana.Length, Owner.Creature, null);
    }
}

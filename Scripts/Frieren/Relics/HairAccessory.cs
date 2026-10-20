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

public sealed class HairAccessory : FrierenRelic
{
    // 一代菲伦的初始遗物；保持 Starter，暂不进入芙莉莲普通奖励池。
    public override RelicRarity Rarity => RelicRarity.Starter;
    public override async Task AfterCardExhausted(PlayerChoiceContext context, CardModel card, bool causedByEthereal)
    {
        if (card.Owner != Owner || !ManaCmd.IsMana(card)) return;
        Flash(); await PowerCmd.Apply<ConcentrationPower>(context, Owner.Creature, 1, Owner.Creature, null);
    }
}

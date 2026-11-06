using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Events.Custom;
using Godot;

namespace FrierenMod.Scripts.Mechanics;

public static class SealingOreCmd
{
    public static bool HasOre(Player player) => MagicDeckStore.Read(player).Props.Any(p => p.PropId == "SealingOre");
    // 库存是权威状态；净化能力不能绕过仍持有封魔矿的限制。
    public static bool BlocksChant(Player player) => HasOre(player);
    public static async Task Refresh(Player player)
    {
        if (player.Creature.CombatState == null || !player.Creature.IsAlive) return;
        var power = player.Creature.GetPower<SealingOrePower>();
        if (HasOre(player)) {
            if (power == null) await PowerCmd.Apply<SealingOrePower>(new ThrowingPlayerChoiceContext(), player.Creature, 1, player.Creature, null);
        } else if (power != null) await PowerCmd.Remove(power);
    }
    public static async Task Visit(Player player, AbstractRoom room)
    {
        if (room is not MerchantRoom && room is not EventRoom { CanonicalEvent: FakeMerchant }) return;
        var deck = MagicDeckStore.Read(player);
        var ore = deck.Props.Where(p => p.PropId == "SealingOre").ToArray();
        if (ore.Length == 0) return;
        foreach (var item in ore) deck.Remove(item.InstanceId);
        // 先提交移除，读档及重开商店均不会再次兑现。
        MagicDeckStore.Write(player, deck, ore[0].InstanceId);
        await PlayerCmd.GainGold(300 * ore.Length, player);
        if (!LocalContext.IsMe(player) || MegaCrit.Sts2.Core.TestSupport.TestMode.IsOn) return;
        Callable.From(() => {
            var button = NMerchantRoom.Instance?.MerchantButton ?? NEventRoom.Instance?
                .FindChildren("*", "", true, false).OfType<NFakeMerchant>().FirstOrDefault()?.MerchantButton;
            button?.PlayDialogue(new LocString("frieren", "SEALING_ORE.MERCHANT"), 5);
        }).CallDeferred();
    }
}

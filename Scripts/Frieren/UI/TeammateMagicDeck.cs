using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;

namespace FrierenMod.Scripts.UI;

[HarmonyPatch(typeof(NMultiplayerPlayerExpandedState), nameof(NMultiplayerPlayerExpandedState._Ready))]
public static class TeammateMagicDeck
{
    private static void Postfix(NMultiplayerPlayerExpandedState __instance, Player ____player) =>
        Callable.From(() => {
            if (GodotObject.IsInstanceValid(__instance) && __instance.IsInsideTree()) Attach(__instance, ____player);
        }).CallDeferred();

    public static MagicBookScreen? Attach(Control root, Player player)
    {
        if (player.Character is not Frieren) return null;
        // 原生队友详情仍留在底下，关闭魔法书便返回详情，不替换 Capstone。
        var book = new MagicBookScreen(root, player, () => null, readOnly: true, readOnlyHint: "TEAMMATE_BOOK_HINT");
        var button = MagicFactorButton.Create(root);
        button.Released += _ => book.Open();
        book.View.VisibilityChanged += () => button.SetBookOpen(book.View.Visible);
        void Refresh() {
            var deck = MagicDeckStore.Read(player);
            button.SetItemCount(deck.Items.Count + deck.Props.Count);
            book.Refresh();
        }
        var timer = new Godot.Timer { Name = "TeammateMagicDeckRefresh", WaitTime = .2, Autostart = true };
        timer.Timeout += Refresh;
        root.AddChild(timer);
        Refresh();
        return book;
    }
}

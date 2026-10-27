using FrierenMod.Scripts.Cards;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;

namespace FrierenMod.Scripts.UI;

// 原生简单网格忽略 Cancelable；只为魔具店补上原生返回按钮。
[HarmonyPatch(typeof(NSimpleCardSelectScreen), nameof(NSimpleCardSelectScreen._Ready))]
public static class MagicPropShopCancel
{
    private static void Postfix(NSimpleCardSelectScreen __instance, CardSelectorPrefs ____prefs,
        IReadOnlyList<CardModel> ____cards, TaskCompletionSource<IEnumerable<CardModel>> ____completionSource)
    {
        if (!____prefs.Cancelable || ____cards.Count == 0 || !____cards.All(c => c is MagicPropCard)) return;
        var back = GD.Load<PackedScene>("res://scenes/ui/back_button.tscn").Instantiate<NBackButton>();
        back.Name = "FrierenShopCancel";
        __instance.AddChild(back);
        back.Connect(NButton.SignalName.Released, Callable.From<NButton>(_ => {
            back.Disable();
            if (____completionSource.TrySetResult([]))
                NOverlayStack.Instance?.Remove(__instance);
        }));
        back.Enable();
    }
}

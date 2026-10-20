using FrierenMod.Scripts.Cards;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;

namespace FrierenMod.Scripts.Presentation;

[HarmonyPatch]
public static class ChantSelectionPresentation
{
    [HarmonyPatch(typeof(NChooseACardSelectionScreen), nameof(NChooseACardSelectionScreen._Ready))]
    [HarmonyPostfix]
    private static void Ready(NChooseACardSelectionScreen __instance, IReadOnlyList<CardModel> ____cards)
    {
        if (____cards.Count == 0 || ____cards.Any(card => card is not FactorCard { Option: not null })) return;
        __instance.GetNode<NCommonBanner>("Banner").label.SetTextAutoSize(
            new LocString("frieren", "CHANT_CHOOSE").GetFormattedText());
        foreach (var holder in __instance.GetNode<Control>("CardRow").GetChildren().OfType<NGridCardHolder>())
            Decorate(holder);
    }

    public static void Decorate(NGridCardHolder holder)
    {
        Clear(holder);
        if (holder.CardModel is not FactorCard { Option: not null } factor || holder.CardNode is not { } card) return;
        holder.SetMeta("FrierenChantChoice", true);
        holder.SetClickable(factor.CanSelectForChant);
        // 原生入场补间作用于 holder；仅淡化卡面，避免补间把禁用透明度恢复成白色。
        card.Modulate = new Color(1, 1, 1, factor.CanSelectForChant ? 1 : 0.6f);
        var header = new Label {
            Name = "ChantSlotHeader",
            Text = new LocString("frieren", $"CHANT_SLOT.{factor.Slot}").GetFormattedText(),
            Position = new Vector2(-150, -280), Size = new Vector2(300, 50),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        header.AddThemeFontSizeOverride("font_size", 34);
        header.AddThemeColorOverride("font_color", new Color("f2e4bc"));
        header.AddThemeColorOverride("font_shadow_color", Colors.Black);
        header.AddThemeConstantOverride("shadow_offset_y", 3);
        holder.AddChild(header);
    }

    private static void Clear(NGridCardHolder holder)
    {
        var header = holder.GetNodeOrNull<Label>("ChantSlotHeader");
        if (header == null && !holder.HasMeta("FrierenChantChoice")) return;
        if (header != null) {
            holder.RemoveChild(header);
            header.Free();
        }
        if (holder.HasMeta("FrierenChantChoice")) holder.RemoveMeta("FrierenChantChoice");
        holder.SetClickable(true);
        if (holder.CardNode is { } card) card.Modulate = Colors.White;
    }

    // 同时保护鼠标、手柄和直接发出信号的路径；不会改变普通发现/选牌界面。
    [HarmonyPatch(typeof(NChooseACardSelectionScreen), "SelectHolder")]
    [HarmonyPrefix]
    private static bool CanSelect(NCardHolder cardHolder) =>
        cardHolder.CardModel is not FactorCard { Option: not null, CanSelectForChant: false };

    // QueueFreeSafely 会归还对象池而不销毁子节点，故必须主动移除本界面的附加标题。
    [HarmonyPatch]
    private static class HolderCleanup
    {
        private static IEnumerable<MethodBase> TargetMethods() => [
            AccessTools.Method(typeof(NGridCardHolder), nameof(NGridCardHolder.OnFreedToPool)),
            AccessTools.Method(typeof(NGridCardHolder), nameof(NGridCardHolder.OnReturnedFromPool)),
            AccessTools.DeclaredMethod(typeof(NGridCardHolder), "SetCard"),
            AccessTools.DeclaredMethod(typeof(NGridCardHolder), "OnCardReassigned")
        ];
        private static void Prefix(NGridCardHolder __instance) => Clear(__instance);
    }
}

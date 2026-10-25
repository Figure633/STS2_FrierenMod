using FrierenMod.Scripts.Cards;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;

namespace FrierenMod.Scripts.Presentation;

[HarmonyPatch]
public static class ChantSelectionPresentation
{
    [HarmonyPatch(typeof(NChooseACardSelectionScreen), nameof(NChooseACardSelectionScreen._Ready))]
    [HarmonyPostfix]
    private static void Ready(NChooseACardSelectionScreen __instance, IReadOnlyList<CardModel> ____cards, ref Tween? ____cardTween)
    {
        if (____cards.Count == 0 || ____cards.Any(card => card is not FactorCard { Option: not null })) return;
        __instance.GetNode<NCommonBanner>("Banner").label.SetTextAutoSize(
            new LocString("frieren", "CHANT_CHOOSE").GetFormattedText());
        // 一代槽位背景在原卡后独立绘制，比因子卡大；为三张背景留出间距。
        ____cardTween?.Kill();
        ____cardTween = __instance.CreateTween().SetParallel();
        var holders = __instance.GetNode<Control>("CardRow").GetChildren().OfType<NGridCardHolder>().ToArray();
        for (int i = 0; i < holders.Length; i++) {
            var holder = holders[i];
            Decorate(holder);
            ____cardTween.TweenProperty(holder, "position", new Vector2((i - (holders.Length - 1) / 2f) * 460, 0), .5)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
            ____cardTween.TweenProperty(holder, "modulate", Colors.White, .5);
        }
    }

    // 原版选择界面沿用了奖励稀有金光；咏唱只是发动已装载的因子。
    [HarmonyPatch(typeof(NCard), nameof(NCard.ActivateRewardScreenGlow))]
    [HarmonyPrefix]
    private static bool RewardGlow(NCard __instance) =>
        __instance.Model is not FactorCard { Option: not null, Rarity: CardRarity.Rare };

    public static void Decorate(NGridCardHolder holder)
    {
        Clear(holder);
        if (holder.CardModel is not FactorCard { Option: not null } factor || holder.CardNode is not { } card) return;
        holder.SetMeta("FrierenChantChoice", true);
        holder.SetClickable(factor.CanSelectForChant);
        // 原生入场补间作用于 holder；仅淡化卡面，避免补间把禁用透明度恢复成白色。
        card.Modulate = new Color(1, 1, 1, factor.CanSelectForChant ? .85f : .35f);
        var slotVisual = new ChantSlotVisual { Name = "ChantSlotVisual", MouseFilter = Control.MouseFilterEnum.Ignore,
            ShowBehindParent = true };
        holder.AddChild(slotVisual); holder.MoveChild(slotVisual, 0);
        var background = new TextureRect {
            Name = "ChantSlotBackground", Texture = UI.SlotBackLibrary.Texture(SlotBackStore.Selected((int)factor.Slot!.Value)),
            Position = new(-200, -307), Size = new(400, 614),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore, ShowBehindParent = true
        };
        slotVisual.AddChild(background);
        var header = new Label {
            Name = "ChantSlotHeader",
            Text = new LocString("frieren", $"CHANT_SLOT.{factor.Slot}").GetFormattedText(),
            Position = new Vector2(-190, -298), Size = new Vector2(380, 50),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        header.AddThemeFontSizeOverride("font_size", 34);
        header.AddThemeColorOverride("font_color", new Color("f2e4bc"));
        header.AddThemeColorOverride("font_shadow_color", Colors.Black);
        header.AddThemeConstantOverride("shadow_offset_y", 3);
        slotVisual.AddChild(header);
    }

    private static void Clear(NGridCardHolder holder)
    {
        var visual = holder.GetNodeOrNull<Control>("ChantSlotVisual");
        if (visual == null && !holder.HasMeta("FrierenChantChoice")) return;
        if (visual != null) { holder.RemoveChild(visual); visual.Free(); }
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

// 保留 holder 的位置和入场动画，但抵消其悬停缩放；背景与槽位标题始终使用初始尺寸。
public partial class ChantSlotVisual : Control
{
    public override void _EnterTree() { SetNotifyTransform(true); KeepSlotSize(); }
    public override void _Notification(int what) {
        if (what == NotificationTransformChanged) KeepSlotSize();
    }
    private void KeepSlotSize() {
        if (GetParent() is not NGridCardHolder holder) return;
        var scale = holder.Scale;
        if (Mathf.IsZeroApprox(scale.X) || Mathf.IsZeroApprox(scale.Y)) return;
        var desired = holder.SmallScale / scale;
        if (!Scale.IsEqualApprox(desired)) Scale = desired;
    }
}

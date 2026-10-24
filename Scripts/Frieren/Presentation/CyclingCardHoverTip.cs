using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Saves;

namespace FrierenMod.Scripts.Presentation;

// 保留原生预览卡面和布局，多张衍生牌共用一个预览位置。
public sealed class CyclingCardHoverTip(IReadOnlyList<CardModel> cards) : CardHoverTip(cards[0])
{
    public IReadOnlyList<CardModel> Cards { get; } = cards;
}

[HarmonyPatch(typeof(NHoverTipCardContainer), nameof(NHoverTipCardContainer.Add))]
public static class CyclingCardHoverTipPatch
{
    private static void Postfix(NHoverTipCardContainer __instance, CardHoverTip cardTip)
    {
        if (cardTip is not CyclingCardHoverTip { Cards.Count: > 1 } cycling) return;
        var holder = __instance.GetChildren().OfType<Control>().Last();
        var cardNode = holder.FindChildren("*", recursive: true, owned: false).OfType<NCard>().Single();
        int index = 0;
        // 与一代一致，每两秒轮换；计时器随浮窗销毁，不留下全局订阅。
        var timer = new Godot.Timer { WaitTime = 2.0, Autostart = true };
        timer.Timeout += () => {
            index = (index + 1) % cycling.Cards.Count;
            cardNode.Model = cycling.Cards[index];
            cardNode.UpdateVisuals(PileType.Deck, CardPreviewMode.Normal);
            SaveManager.Instance.MarkCardAsSeen(cycling.Cards[index]);
        };
        holder.AddChild(timer);
    }
}

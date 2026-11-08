using FrierenMod.Scripts.Events;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Events;

namespace FrierenMod.Scripts.Presentation;

// 仅在临时预览中直接展示原生标签，避免事件本身的入场渐显遮住短暂的飞入效果。
[HarmonyPatch(typeof(NEventLayout), nameof(NEventLayout.SetDescription))]
public static class EventArtTextPreview
{
    private static void Prefix(EventModel ____event, MegaRichTextLabel? ____description)
    {
        if (____event is EventArtPreview && ____description != null)
            ____description.SetTextAutoSize(string.Empty); // 相同正文翻页时也重新开始标签动画。
    }

    private static void Postfix(NEventLayout __instance, EventModel ____event,
        Tween? ____descriptionTween, MegaRichTextLabel? ____description)
    {
        if (____event is not EventArtPreview || ____description == null) return;
        ____descriptionTween?.Kill();
        ____description.Modulate = Colors.White;
        ____description.VisibleRatio = 1;
        __instance.GetNode<Control>("%Title").Modulate = Colors.White;
    }
}

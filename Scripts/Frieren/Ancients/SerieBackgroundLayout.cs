using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.Screens;

namespace FrierenMod.Scripts.Ancients;

// 原生背景容器为独立立绘缩放/平移；赛莉耶提供的是完整16:9画面，保持全幅显示。
[HarmonyPatch(typeof(NAncientBgContainer), "OnWindowChange")]
public static class SerieBackgroundLayout
{
    private static void Fit(NAncientBgContainer container)
    {
        if (!container.GetChildren().Any(child => child.Name == "Serie")) return;
        container.Scale = Vector2.One;
        container.Position = Vector2.Zero;
    }
    [HarmonyPostfix]
    private static void Postfix(NAncientBgContainer __instance) => Fit(__instance);

    [HarmonyPatch(typeof(NAncientEventLayout), "InitializeVisuals")]
    private static class OnSceneAdded
    {
        [HarmonyPostfix]
        private static void Postfix(NAncientEventLayout __instance) => Fit(__instance.GetNode<NAncientBgContainer>("%AncientBgContainer"));
    }
}

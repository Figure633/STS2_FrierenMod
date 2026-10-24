using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Presentation;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Pooling;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;

public static class MagicPropAppearanceTests
{
    public static async Task<int> Run(Node root)
    {
        int checks = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception(name); checks++; GD.Print("PASS 魔具修正：" + name); }
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "PURPLE-PROPS");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = 1;
        LocManager.Instance.SetLanguage("zhs");
        var host = new Control { Size = new Vector2(1920, 1080) }; root.AddChild(host);
        host.AddChild(new ColorRect { Size = host.Size, Color = new Color("17202c"), MouseFilter = Control.MouseFilterEnum.Ignore });
        var nodes = new List<NCard>();
        foreach (var rule in MagicPropRules.All) {
            var model = MagicPropCard.Create(player, rule.Id);
            var node = NodePool.Get<NCard>(); AccessTools.Property(typeof(NCard), nameof(NCard.Model)).SetValue(node, model);
            node.Position = new Vector2(290 + (nodes.Count % 4) * 445, 270 + (nodes.Count / 4) * 525);
            node.Scale = Vector2.One * 0.9f; host.AddChild(node); node.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
            nodes.Add(node);
            var banner = (CanvasItem)AccessTools.Field(typeof(NCard), "_banner").GetValue(node)!;
            var border = (CanvasItem)AccessTools.Field(typeof(NCard), "_portraitBorder").GetValue(node)!;
            var plaque = (CanvasItem)AccessTools.Field(typeof(NCard), "_typePlaque").GetValue(node)!;
            Check(banner.Material?.ResourcePath == MagicPropPresentation.BannerMaterialPath &&
                border.Material == banner.Material && plaque.Material == banner.Material,
                rule.Id + "的实际横幅、插画边框与类型牌匾统一使用原版紫色材质");
            Check((Color)AccessTools.Method(typeof(NCard), "GetTitleLabelOutlineColor").Invoke(node, null)! == StsColors.cardTitleOutlineCurse &&
                model.Rarity == CardRarity.Token && !FrierenLibraryFilter.IsCharacterCard(model),
                rule.Id + "标题描边紫色，保持衍生牌归属");
        }
        foreach (var language in new[] { "zhs", "eng" }) {
            LocManager.Instance.SetLanguage(language);
            string expected = language == "zhs" ? "选择一张已装载和一张未装载的因子，使它们交换槽位" :
                "Choose an equipped factor and an unequipped factor. Swap their slots.";
            var tool = MagicPropCard.Create(player, "UnbelievableTool");
            Check(tool.GetDescriptionForPile(PileType.None) == expected &&
                new LocString("frieren", "PROP.UnbelievableTool").GetFormattedText() == expected,
                language + "神奇妙妙工具卡面与魔具说明均采用已保存的新文案");
        }
        LocManager.Instance.SetLanguage("zhs");
        for (int i = 0; i < 5; i++) await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
        if (OS.GetCmdlineUserArgs().Contains("--ui-snapshot")) root.GetViewport().GetTexture().GetImage().SavePng(
            ProjectSettings.GlobalizePath("res://../../artifacts/magic-props-purple.png"));
        foreach (var node in nodes) { host.RemoveChild(node); node.QueueFreeSafely(); }
        host.QueueFree(); LocManager.Instance.SetLanguage("eng"); RunManager.Instance.CleanUp(); return checks;
    }
}

using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using System.Text.Json;
using System.Text.RegularExpressions;

// 使用原生卡牌场景测量真实排版，覆盖普通、升级、初始因子和结算预览。
public static class CardUiAudit
{
    public static async Task<int> Run(Node root, Player player)
    {
        var records = new List<object>();
        var scene = GD.Load<PackedScene>("res://scenes/cards/card.tscn");
        bool snapshots = OS.GetCmdlineUserArgs().Contains("--ui-snapshot");
        foreach (string language in new[] { "eng", "zhs" })
        {
            LocManager.Instance.SetLanguage(language);
            var models = new List<CardModel>();
            foreach (var type in typeof(Mana).Assembly.GetTypes().Where(t => !t.IsAbstract && typeof(CardModel).IsAssignableFrom(t)).OrderBy(t => t.Name)) {
                var card = (CardModel)ModelDb.GetById<CardModel>(ModelDb.GetId(type)).ToMutable();
                card.Owner = player;
                models.Add(card);
                if (card.MaxUpgradeLevel > 0) {
                    var upgraded = (CardModel)ModelDb.GetById<CardModel>(ModelDb.GetId(type)).ToMutable();
                    upgraded.Owner = player;
                    upgraded.UpgradeInternal();
                    models.Add(upgraded);
                }
            }
            foreach (var factor in SpellCatalog.All) {
                models.Add(FactorCard.Create(player, factor.Id, ManaSlot.Draw));
                models.Add(FactorCard.Create(player, factor.Id, ManaSlot.Hand, new ChantOption(ManaSlot.Hand, factor.Id, 2, 3, 4)));
            }
            int page = 0;
            foreach (var batch in models.Chunk(12))
            {
                var canvas = new Control(); root.AddChild(canvas);
                canvas.AddChild(new ColorRect { Size = new Vector2(1920, 1080), Color = new Color("17202c"), MouseFilter = Control.MouseFilterEnum.Ignore });
                var nodes = new List<NCard>();
                for (int i = 0; i < batch.Length; i++) {
                    var model = batch[i];
                    string text = model.GetDescriptionForPile(PileType.None);
                    foreach (var tip in model.HoverTips.OfType<MegaCrit.Sts2.Core.HoverTips.HoverTip>())
                        if (string.IsNullOrWhiteSpace(tip.Description) || tip.Description.Contains("TIP."))
                            throw new Exception($"悬停规则说明缺失：{language}/{model.Id}");
                    if (Regex.IsMatch(text, @"\{[^}]+\}|\b(?:max|min)\s*\(|\b\d+X\b|\bX[+-]\d|FRIERENMOD-"))
                        throw new Exception($"卡面出现未格式化内容：{language}/{model.Id}: {text}");
                    // TestMode 保持开启；直接实例化场景以绕过 NCard.Create 的测试短路。
                    var node = scene.Instantiate<NCard>();
                    AccessTools.Property(typeof(NCard), nameof(NCard.Model)).SetValue(node, model);
                    canvas.AddChild(node);
                    node.Position = new Vector2(160 + i % 6 * 320, 270 + i / 6 * 520);
                    node.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
                    nodes.Add(node);
                }
                for (int i = 0; i < 3; i++) await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
                foreach (var node in nodes) {
                    var description = node.GetNode<RichTextLabel>("%DescriptionLabel");
                    var title = node.GetNode<Label>("%TitleLabel");
                    int font = description.GetThemeFontSize("normal_font_size");
                    int titleFont = title.GetThemeFontSize("font_size");
                    int height = description.GetContentHeight();
                    records.Add(new { language, id = node.Model!.Id.ToString(), upgrade = node.Model.CurrentUpgradeLevel,
                        context = node.Model is FactorCard factor ? factor.Option.HasValue ? "chant" : factor.Slot.HasValue ? "initial" : "inventory" : "card",
                        font, titleFont, height, availableHeight = description.Size.Y, text = node.Model.GetDescriptionForPile(PileType.None) });
                    if (height > description.Size.Y + 2)
                        throw new Exception($"卡面文字溢出：{language}/{node.Model.Id}，{height}/{description.Size.Y}");
                    if (font < 16)
                        throw new Exception($"卡面字号过小：{language}/{node.Model.Id}，字号 {font}");
                    if (titleFont < 16)
                        throw new Exception($"卡牌标题字号过小：{language}/{node.Model.Id}，字号 {titleFont}");
                }
                if (snapshots)
                    root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath($"res://../../artifacts/cards-{language}-{++page:00}.png"));
                canvas.QueueFree();
                await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            GD.Print($"PASS {language} 原生卡面排版：{models.Count} 个普通/升级/因子预览，无溢出与程序公式");
        }
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://../../artifacts/card-ui-audit.json"),
            JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true }));
        return 2;
    }
}

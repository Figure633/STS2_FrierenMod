using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Powers;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using System.Text.RegularExpressions;

// 卡面只显示一代的文字碎片与费用；三个独立能力保留各自的标题和图标。
public static class PuzzlePresentationTests
{
    public static int Run(Player player)
    {
        var clues = new[] {
            (Id: "Factor100", Slot: ManaSlot.Draw, Zh: "芙莉莲", En: "Frieren"),
            (Id: "Factor010", Slot: ManaSlot.Hand, Zh: "消灭", En: "kill"),
            (Id: "Factor001", Slot: ManaSlot.Discard, Zh: "怪物", En: "monster")
        };
        foreach (string language in new[] { "eng", "zhs" })
        {
            LocManager.Instance.SetLanguage(language);
            int states = 0;
            foreach (var clue in clues)
            {
                string fragment = language == "zhs" ? clue.Zh : clue.En;
                void CheckCard(FactorCard card)
                {
                    string formatted = card.GetDescriptionForPile(PileType.None);
                    AssertHidden(formatted);
                    string text = Regex.Replace(formatted, @"\[[^\]]*\]", "");
                    foreach (var tip in card.HoverTips.OfType<HoverTip>()) AssertHidden(tip.Description);
                    if (card.Option is { } option)
                    {
                        string cost = language == "zhs" ? $"消耗{option.Cost}张魔力" : $"Consume {option.Cost} Mana";
                        if (Regex.IsMatch(text, @"[？?!！@]") || !text.Contains(cost) || !text.Contains(fragment))
                            throw new Exception($"谜题咏唱费用、文字碎片或多余符号异常：{language}/{card.FactorId}/{option.Slot}: {text}");
                    }
                    else if (!text.Contains(fragment) || !text.Contains(language == "zhs" ? "咏唱 " +
                        (clue.Id == "Factor100" ? "X+1" : clue.Id == "Factor010" ? "2X+2" : "3X+3") : "Chant X"))
                        throw new Exception($"谜题库存丢失一代线索或费用：{language}/{card.FactorId}: {text}");
                    states++;
                }
                CheckCard(FactorCard.Create(player, clue.Id));
                var inventory = FactorCard.Create(player, clue.Id);
                inventory.InventoryInstanceId = 1;
                CheckCard(inventory);
                foreach (var slot in Enum.GetValues<ManaSlot>())
                {
                    CheckCard(FactorCard.Create(player, clue.Id, slot));
                    var equipped = FactorCard.Create(player, clue.Id, slot);
                    equipped.InventoryInstanceId = 1;
                    CheckCard(equipped);
                    foreach (int cost in new[] { 0, 7 })
                        CheckCard(FactorCard.Create(player, clue.Id, slot, new ChantOption(slot, clue.Id, 2, cost, 4)));
                }
            }
            var types = new[] { typeof(Factor100Power), typeof(Factor010Power), typeof(Factor001Power) };
            var titles = language == "zhs" ? new[] { "！？？", "？！？", "？？！" } : new[] { "!??", "?!?", "??!" };
            for (int i = 0; i < types.Length; i++) {
                var power = (PuzzleCluePower)ModelDb.GetById<PowerModel>(ModelDb.GetId(types[i])).ToMutable();
                AccessTools.Property(typeof(PowerModel), nameof(PowerModel.Owner)).SetValue(power, player.Creature);
                AssertHidden(power.Title.GetFormattedText());
                foreach (var tip in power.HoverTips.OfType<HoverTip>()) AssertHidden(tip.Description);
                string fragment = language == "zhs" ? clues[i].Zh : clues[i].En;
                if (power.Title.GetFormattedText() != titles[i] || !power.IsVisible ||
                    power.StackType != MegaCrit.Sts2.Core.Entities.Powers.PowerStackType.Single ||
                    !power.Description.GetFormattedText().Contains(fragment, StringComparison.OrdinalIgnoreCase) ||
                    GD.Load<Texture2D>(power.CustomPackedIconPath).GetWidth() != 84)
                    throw new Exception($"谜题独立能力或一代图标异常：{language}/{i}");
                states++;
            }
            if (ModelDb.Power<PuzzlePower>().IsVisible) throw new Exception("隐藏顺序状态不能额外显示为第四个谜题能力");
            GD.Print($"PASS {language} 谜题呈现：{states} 个奖励/库存/装载/咏唱/状态场景，三个独立线索与一代图标，无额外槽位符号");
        }
        return 2;
    }

    private static void AssertHidden(string text)
    {
        if (Regex.IsMatch(text, @"\{[^}]+\}|谜题第|解开谜题|完成|顺序|回合结束|所有敌人|\bsteps?\b|solve|turn end|all enemies|wrong order|FRIERENMOD-", RegexOptions.IgnoreCase))
            throw new Exception("谜题出现答案或未格式化占位符：" + text);
    }
}

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

// 谜题只保留一代的碎片与符号，奖励、库存、装载、咏唱和状态浮窗均不能直接给出答案。
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
                        string mark = language == "zhs"
                            ? option.Slot == clue.Slot ? "！" : "？"
                            : option.Slot == clue.Slot ? "@" : "?";
                        string cost = language == "zhs" ? $"消耗 {option.Cost} 张魔力" : $"Consume {option.Cost} Mana";
                        if (text.Split('\n').Last().Trim() != mark || !text.Contains(cost) || text.Contains(fragment))
                            throw new Exception($"谜题咏唱未保留费用与槽位符号：{language}/{card.FactorId}/{option.Slot}: {text}");
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
            var power = (PuzzlePower)ModelDb.GetById<PowerModel>(ModelDb.GetId(typeof(PuzzlePower))).ToMutable();
            AccessTools.Property(typeof(PowerModel), nameof(PowerModel.Owner)).SetValue(power, player.Creature);
            for (int amount = 0; amount <= 3; amount++)
            {
                power.SetAmount(amount);
                AssertHidden(power.Title.GetFormattedText());
                foreach (var tip in power.HoverTips.OfType<HoverTip>()) AssertHidden(tip.Description);
                string fragment = amount == 0 ? "" : language == "zhs" ? clues[amount - 1].Zh : clues[amount - 1].En;
                if (!power.Description.GetFormattedText().Contains(fragment, StringComparison.OrdinalIgnoreCase))
                    throw new Exception($"谜题状态丢失一代碎片：{language}/{amount}");
                states++;
            }
            GD.Print($"PASS {language} 谜题呈现：{states} 个奖励/库存/装载/咏唱/状态场景，仅保留一代线索与费用");
        }
        return 2;
    }

    private static void AssertHidden(string text)
    {
        if (Regex.IsMatch(text, @"\{[^}]+\}|谜题第|解开谜题|完成|顺序|回合结束|所有敌人|\bsteps?\b|solve|turn end|all enemies|wrong order|FRIERENMOD-", RegexOptions.IgnoreCase))
            throw new Exception("谜题出现答案或未格式化占位符：" + text);
    }
}

using System.Text.RegularExpressions;
using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;

public static class CustomSpellDisplayTests
{
    public static async Task<int> Run()
    {
        int count = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception(name); GD.Print("PASS 自定义魔法：" + name); count++; }
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "FRIEREN-CUSTOM-SPELL-DISPLAY");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = 1;
        var combat = new CombatState(runState: run); combat.AddPlayer(player);
        var enemy = combat.CreateCreature(ModelDb.Monster<MegaCrit.Sts2.Core.Models.Monsters.Axebot>().ToMutable(), CombatSide.Enemy, null);
        combat.AddCreature(enemy); enemy.SetMaxHpInternal(9999); enemy.SetCurrentHpInternal(9999);
        CombatManager.Instance.SetUpCombat(combat);
        AccessTools.Property(typeof(CombatManager), nameof(CombatManager.IsInProgress)).SetValue(CombatManager.Instance, true);
        var context = new BlockingPlayerChoiceContext();
        var resources = new ResourceInfo { EnergySpent = 0, EnergyValue = 0, StarsSpent = 0, StarValue = 0 };
        string Text(CardModel card) {
            card.UpdateDynamicVarPreview(CardPreviewMode.Normal, enemy, card.DynamicVars);
            var text = card.GetDescriptionForPile(PileType.Hand, enemy);
            Check(!Regex.IsMatch(text, @"\d+[.,]\d+"), "正文不显示小数：" + text);
            return text;
        }
        foreach (string language in new[] { "zhs", "eng" }) {
            LocManager.Instance.SetLanguage(language);
            foreach (var kind in new[] { "Damage", "DamageAll", "DamageTriple" }) {
                var card = combat.CreateCard<CustomLegendarySpell>(player);
                card.Configure(2, [new LegendaryEffect(kind, 10)]);
                await CardPileCmd.Add(card, PileType.Hand);
                Check(Text(card).Contains("10") && !Text(card).Contains("[green]"), language + kind + "基础整数显示");
                var strength = await PowerCmd.Apply<StrengthPower>(context, player.Creature, 2, player.Creature, null);
                Check(Text(card).Contains("[green]12[/green]"), language + kind + "力量伤害绿色整数");
                await PowerCmd.Remove(strength!);
                var weak = await PowerCmd.Apply<WeakPower>(context, player.Creature, 1, enemy, null);
                Check(Text(card).Contains("[red]7[/red]"), language + kind + "虚弱7.5按原版取整显示7");
                int hp = enemy.CurrentHp;
                await card.OnPlayWrapper(context, kind == "DamageAll" ? null : enemy, false, resources, skipCardPileVisuals: true);
                Check(hp - enemy.CurrentHp == (kind == "DamageTriple" ? 21 : 7), language + kind + "实际伤害与预览一致");
                await PowerCmd.Remove(weak!);
            }
            var block = combat.CreateCard<CustomLegendarySpell>(player);
            block.Configure(2, [new LegendaryEffect("Block", 5), new LegendaryEffect("Vulnerable", 3)]);
            await CardPileCmd.Add(block, PileType.Hand);
            var dex = await PowerCmd.Apply<DexterityPower>(context, player.Creature, 2, player.Creature, null);
            Check(Text(block).Contains("[green]7[/green]"), language + "敏捷格挡绿色整数");
            await PowerCmd.Remove(dex!);
            var frail = await PowerCmd.Apply<FrailPower>(context, player.Creature, 1, enemy, null);
            Check(Text(block).Contains("[red]3[/red]"), language + "脆弱3.75按原版取整显示3");
            int before = player.Creature.Block;
            await block.OnPlayWrapper(context, null, false, resources, skipCardPileVisuals: true);
            Check(player.Creature.Block - before == 3, language + "实际格挡与预览一致");
            await PowerCmd.Remove(frail!);
            foreach (var power in enemy.Powers.ToArray()) await PowerCmd.Remove(power);
            var restored = combat.CreateCard<CustomLegendarySpell>(player);
            restored.Recipe = block.Recipe; restored.CraftedCost = block.CraftedCost;
            Check(Text(restored).Contains("5") && restored.Effects.SequenceEqual(block.Effects) && restored.CraftedCost == 2,
                language + "配方还原保留数值与费用");
            foreach (var kind in new[] { "Damage", "DamageTriple", "DamageAll", "Block", "Chant" }) {
                var choice = combat.CreateCard<LegendaryCraftChoice>(player);
                choice.Effect = new LegendaryEffect(kind, 4);
                Check(Text(choice).Contains("4"), language + kind + "构筑选项文案仍正常");
            }
        }
        return count;
    }
}

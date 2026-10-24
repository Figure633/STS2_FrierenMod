using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Monsters;
using FrierenMod.Scripts.Powers;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Unlocks;

public static class LatestTodoTests
{
    private static readonly List<string> Spoken = [];
    private static bool CaptureTalk(LocString line) { Spoken.Add(line.GetFormattedText()); return false; }
    private static bool SkipEndTurn() => false;

    public static async Task<int> Run()
    {
        int count = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception(name); count++; GD.Print("PASS 0.7.9：" + name); }
        var capture = new Harmony("frieren.tests.todo079");
        capture.Patch(AccessTools.Method(typeof(TalkCmd), nameof(TalkCmd.Play)), prefix: new HarmonyMethod(typeof(LatestTodoTests), nameof(CaptureTalk)));
        capture.Patch(AccessTools.Method(typeof(PlayerCmd), nameof(PlayerCmd.EndTurn)), prefix: new HarmonyMethod(typeof(LatestTodoTests), nameof(SkipEndTurn)));
        try {
            foreach (var language in new[] { "zhs", "eng" }) {
                LocManager.Instance.SetLanguage(language); Spoken.Clear();
                var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
                var run = RunState.CreateForTest([player], seed: "LATEST-TODO-079");
                RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = 1;
                SaveManager.Instance.InitPrefsDataForTest();
                var combat = new CombatState(runState: run); combat.AddPlayer(player);
                var monster = (Mimic)ModelDb.Monster<Mimic>().ToMutable();
                var mimic = combat.CreateCreature(monster, CombatSide.Enemy, null); combat.AddCreature(mimic);
                CombatManager.Instance.SetUpCombat(combat);
                AccessTools.Property(typeof(CombatManager), nameof(CombatManager.IsInProgress)).SetValue(CombatManager.Instance, true);
                var context = new ThrowingPlayerChoiceContext();
                var resources = new ResourceInfo { EnergySpent = 0, EnergyValue = 0, StarsSpent = 0, StarValue = 0 };
                foreach (var slot in Enum.GetValues<ManaSlot>()) {
                    var option = ChantRules.Preview(slot, SpellCatalog.Get("BloodSpell").Rules, 3);
                    var text = FactorCard.Create(player, "BloodSpell", slot, option).GetDescriptionForPile(PileType.None);
                    Check(text.Contains("[red]3[/red]") && !text.Contains(language == "zhs" ? "从" : "from", StringComparison.OrdinalIgnoreCase) &&
                        !text.Contains("注释") && (language != "zhs" || text.StartsWith("消耗")), language + "驭血中文消耗生命、数值标红，不混入魔力支付前缀或编辑注释：" + slot);
                }
                foreach (bool upgraded in new[] { false, true }) {
                    var card = combat.CreateCard<AccessoriesSpell>(player);
                    if (upgraded) { card.UpgradeInternal(); card.FinalizeUpgradeInternal(); }
                    Check(card.EnergyCost.GetWithModifiers(CostModifiers.All) == 3 &&
                        card.Keywords.Contains(CardKeyword.Innate) == !upgraded && card.Keywords.Contains(CardKeyword.Ethereal),
                        language + "饰品魔法始终3费，升级仅移除固有，保留虚无：" + upgraded);
                    Check(card is ILegendaryCard && card.GetDescriptionForPile(PileType.None).Contains(language == "zhs" ? "传说魔法" : "Legendary Spell"),
                        language + "饰品魔法保留传说魔法身份与词条：" + upgraded);
                    await CardPileCmd.Add(card, PileType.Hand);
                    await card.OnPlayWrapper(context, null, false, resources, skipCardPileVisuals: true);
                    var reward = player.Creature.GetPower<GainRelicPower>()!;
                    var room = new CombatRoom(combat); await reward.AfterCombatVictory(room);
                    Check(room.ExtraRewards[player].OfType<RelicReward>().Count() == 1, "饰品魔法胜利奖励仍为一件遗物：" + upgraded);
                    await PowerCmd.Remove(reward);
                }
                monster.RollMove([player.Creature]); await monster.AfterAddedToRoom();
                var disguise = mimic.GetPower<MimicPower>()!;
                var play = new CardPlay { Card = combat.CreateCard<StrikeFrieren>(player), Target = mimic, ResultPile = PileType.Discard,
                    IsAutoPlay = false, PlayIndex = 0, PlayCount = 1, Resources = resources };
                for (int i = 0; i < 4; i++) await disguise.AfterCardPlayedLate(context, play);
                Check(Spoken.Count == 0, "伪装前四次不提前说话");
                await disguise.AfterCardPlayedLate(context, play);
                var chew = language == "zhs" ? "（咀嚼的声音）" : "(Chewing sounds)";
                Check(Spoken.SequenceEqual(new[] { chew }) && !mimic.HasPower<MimicPower>() && player.Creature.HasPower<DarkPower>(),
                    language + "解除伪装只显示咀嚼，原有黑暗结算正常");
                await PowerCmd.Apply<SwallowedPower>(context, player.Creature, 6, mimic, null);
                await monster.PerformMove(); monster.RollMove([player.Creature]);
                await monster.PerformMove(); monster.RollMove([player.Creature]);
                Check(monster.NextMove.Id == "FULL", "达到吞噬阈值后进入吃撑回合");
                await monster.PerformMove(); monster.RollMove([player.Creature]);
                Check(Spoken.SequenceEqual(new[] { chew, language == "zhs" ? "[sine]嗝~~~~~~~[/sine]" : "[sine]Buuurp…[/sine]" }) && monster.NextMove.Id != "FULL",
                    language + "仅吃撑回合说嗝，随后恢复正常行动");
                RunManager.Instance.CleanUp();
            }
        } finally { capture.UnpatchAll(capture.Id); LocManager.Instance.SetLanguage("eng"); }
        return count;
    }
}

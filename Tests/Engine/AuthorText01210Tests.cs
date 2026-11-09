using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using FrierenMod.Scripts.Presentation;
using FrierenMod.Scripts.UI;
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
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Unlocks;

public static class AuthorText01210Tests
{
    public static async Task<int> Run(Node root)
    {
        int checks = 0;
        void Check(bool ok, string text) { if (!ok) throw new Exception(text); checks++; GD.Print("PASS 作者文案：" + text); }
        SaveManager.Instance.InitPrefsDataForTest(); PresentationSettings.Achievements = true;
        var p = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var run = RunState.CreateForTest([p], seed: "AUTHOR-TEXT-01210");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = 1;
        var combat = new CombatState(runState: run); combat.AddPlayer(p);
        var ordinaryBird = combat.CreateCreature(ModelDb.Monster<Byrdpip>().ToMutable(), CombatSide.Enemy, null);
        var hunter = combat.CreateCreature(ModelDb.Monster<HunterKiller>().ToMutable(), CombatSide.Enemy, null);
        foreach (var enemy in new[] { ordinaryBird, hunter }) combat.AddCreature(enemy);
        CombatManager.Instance.SetUpCombat(combat); CombatTestState.SetInProgress(true);
        foreach (var enemy in new[] { ordinaryBird, hunter }) enemy.Monster!.RollMove([p.Creature]);
        var context = new ThrowingPlayerChoiceContext();
        async Task Capture(MegaCrit.Sts2.Core.Entities.Creatures.Creature enemy) {
            var card = combat.CreateCard<BirdCapturingSpell>(p); await CardPileCmd.Add(card, PileType.Hand);
            await card.OnPlayWrapper(context, enemy, false, new ResourceInfo { EnergySpent = 0, EnergyValue = 0, StarsSpent = 0, StarValue = 0 }, true);
        }
        SlotBackStore.Read().Unlocked.Remove("4004"); await Capture(ordinaryBird);
        Check(ordinaryBird.HasPower<BirdCapturedPower>() && !SlotBackStore.Read().Unlocked.Contains("4004"), "普通鸟类仍可眩晕，但不解锁猎人杀手成就");
        await Capture(hunter);
        Check(hunter.HasPower<BirdCapturedPower>() && SlotBackStore.Read().Unlocked.Contains("4004"), "实际眩晕猎人杀手后解锁成就");
        foreach (int block in new[] { 99, 100, 101, 999 }) {
            SlotBackStore.Read().Unlocked.Remove("5002"); p.Creature.LoseBlockInternal(p.Creature.Block); p.Creature.GainBlockInternal(block);
            FrierenAchievements.TurnStarted(p);
            Check(SlotBackStore.Read().Unlocked.Contains("5002") == (block >= 100), "格挡成就边界：" + block);
        }
        foreach (int total in new[] { 19, 20, 21 }) {
            SlotBackStore.Read().Unlocked.Remove("5009"); var deck = new MagicDeckState();
            for (int i = 0; i < total - 1; i++) deck.Add("BuildUpBody"); deck.AddProp("UnbelievableTool");
            MagicDeckStore.Write(p, deck); FrierenAchievements.RunEnded(run, p, true);
            Check(SlotBackStore.Read().Unlocked.Contains("5009") == (total >= 20), "魔法牌组成就按因子加魔具计数：" + total);
        }
        p.Deck.Clear(silent: true);
        var kinds = ModelDb.CardPool<FrierenCardPool>().AllCards.Where(FrierenCardAvailability.IsRandomCandidate).Take(15).ToArray();
        foreach (var model in kinds.Take(14)) await CardPileCmd.Add(run.CreateCard(model, p), PileType.Deck);
        await CardPileCmd.Add(run.CreateCard(kinds[0], p), PileType.Deck);
        Check(!CommissionCmd.OwnsSpell(p, "distinct-twelve"), "百变秀14种卡加同名重复牌仍未达标");
        await CardPileCmd.Add(run.CreateCard(kinds[14], p), PileType.Deck);
        Check(CommissionCmd.OwnsSpell(p, "distinct-twelve"), "百变秀达到15种卡，可兼容旧存档条件键");
        Check(CommissionState.Villagers.Length == 16 && CommissionState.Villagers.All(q => q.Id != "SLEEP"), "睡眠委托移出生成池");
        CombatTestState.SetInProgress(false);
        AccessTools.Property(typeof(CombatManager), nameof(CombatManager.IsStarting)).SetValue(CombatManager.Instance, false);
        CommissionCmd.Write(p, new CommissionState { CurrentAct = 0, GeneratedActs = [0], Quests = [
            new() { Id = 10001, Act = 0, Template = "WEDDING", Goal = CommissionGoal.Deck, Spell = "FlowerFieldSpell" },
            new() { Id = 10002, Act = 0, Template = "LESSON", Goal = CommissionGoal.Deck, Spell = "upgraded-five", Stars = 2 },
            new() { Id = 10003, Act = 0, Template = "VARIETY", Goal = CommissionGoal.Deck, Spell = "distinct-twelve", Stars = 2 }
        ] });
        foreach (var lang in new[] { "zhs", "eng" }) {
            LocManager.Instance.SetLanguage(lang);
            Check(new LocString("frieren", "BACK.5002.description").GetFormattedText().Contains("100") &&
                new LocString("frieren", "BACK.5009.description").GetFormattedText().Contains("20") &&
                new LocString("frieren", "COMMISSION.GOAL.VARIETY").GetRawText().Contains("15"), lang + "显示门槛与实际判定一致");
            var host = new Control { Size = new(1920, 1080) }; root.AddChild(host);
            var board = new CommissionScreen(host, p); board.Open();
            for (int i = 0; i < 5; i++) await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
            if (OS.GetCmdlineUserArgs().Contains("--ui-snapshot")) {
                var dir = ProjectSettings.GlobalizePath("res://../../artifacts/author-text-01210"); Directory.CreateDirectory(dir);
                root.GetViewport().GetTexture().GetImage().SavePng(Path.Combine(dir, lang + ".png"));
            }
            board.Close(); host.QueueFree();
            for (int i = 0; i < 3; i++) await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        RunManager.Instance.CleanUp(); return checks;
    }
}

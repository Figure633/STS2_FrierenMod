using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Powers;
using FrierenMod.Scripts.Presentation;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;

public static class CurrentTodoFixTests
{
    public static async Task<int> Run()
    {
        int count = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception(name); count++; GD.Print("PASS " + name); }
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "TODO-083");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = 1;
        var combat = new CombatState(runState: run); combat.AddPlayer(player);
        var enemy = combat.CreateCreature(ModelDb.Monster<MegaCrit.Sts2.Core.Models.Monsters.Axebot>().ToMutable(), CombatSide.Enemy, null);
        combat.AddCreature(enemy);
        CombatManager.Instance.SetUpCombat(combat);
        typeof(CombatManager).GetProperty(nameof(CombatManager.IsInProgress))!.SetValue(CombatManager.Instance, true);
        var context = new ThrowingPlayerChoiceContext();
        var hellfire = combat.CreateCard<HellFireSummoning>(player);
        await CardPileCmd.Add(hellfire, PileType.Draw); await hellfire.BeforeCombatStart();
        var seal = player.Creature.GetPower<SealPower>()!;
        int updates = 0; seal.DisplayAmountChanged += () => updates++;
        Check(seal.Cards.Count == 1 && seal.DisplayAmount == 0, "封印初始显示零次咏唱，不显示封印卡数");
        CardPlay MakePlay(CardModel card) => new() { Card = card, Target = null, IsAutoPlay = false, ResultPile = PileType.Discard,
            Resources = new ResourceInfo { EnergySpent = 0, EnergyValue = 0, StarsSpent = 0, StarValue = 0 }, PlayCount = 1, PlayIndex = 0 };
        var ordinary = MakePlay(combat.CreateCard<StrikeFrieren>(player));
        CombatManager.Instance.History.CardPlayStarted(combat, ordinary);
        Check(seal.DisplayAmount == 0, "普通牌不增加咏唱计数");
        for (int i = 1; i <= 2; i++) {
            var play = MakePlay(combat.CreateCard<RapidChant>(player));
            CombatManager.Instance.History.CardPlayStarted(combat, play); await seal.AfterChant(context, play);
            Check(seal.DisplayAmount == i && updates == i, "第" + i + "张咏唱即时更新图标");
        }
        player.PlayerCombatState!.IncrementTurnNumber();
        await seal.AfterPlayerTurnStart(context, player);
        Check(seal.DisplayAmount == 0 && updates == 3, "换回合归零并通知图标刷新");
        for (int i = 1; i <= 5; i++) {
            var play = MakePlay(combat.CreateCard<RapidChant>(player));
            CombatManager.Instance.History.CardPlayStarted(combat, play); await seal.AfterChant(context, play);
        }
        Check(!player.Creature.HasPower<SealPower>() && hellfire.Pile?.Type == PileType.Hand, "单回合五次后原有解封入手正常");
        bool original = PresentationSettings.OriginalMusic;
        try {
            PresentationSettings.OriginalMusic = true;
            Check(FrierenMusic.SuppressOpeningTheme(run, "event:/sfx/ambience/act1_neow"), "一代音乐开启时抑制涅奥独立主题循环");
            Check(!FrierenMusic.SuppressOpeningTheme(run, "event:/sfx/npcs/neow/neow_welcome"), "涅奥台词和其他音效保留");
            PresentationSettings.OriginalMusic = false;
            Check(!FrierenMusic.SuppressOpeningTheme(run, "event:/sfx/ambience/act1_neow"), "关闭音乐替换保留原生主题");
            PresentationSettings.OriginalMusic = true;
            var other = RunState.CreateForTest([Player.CreateForNewRun<Ironclad>(UnlockState.all, 1)], seed: "OTHER");
            Check(!FrierenMusic.SuppressOpeningTheme(other, "event:/sfx/ambience/act1_neow"), "其他角色不改变开局音乐");
        } finally { PresentationSettings.OriginalMusic = original; }
        return count;
    }
}


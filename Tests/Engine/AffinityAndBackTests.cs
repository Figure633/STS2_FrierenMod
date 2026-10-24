using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Powers;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.TestSupport;
using FrierenMod.Scripts.Mechanics;

public static class AffinityAndBackTests
{
    public static async Task<int> Run()
    {
        int count = 0;
        void Check(bool value, string name) {
            if (!value) throw new Exception(name);
            count++; GD.Print("PASS 魔力亲和与卡背：" + name);
        }
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var other = Player.CreateForNewRun<Frieren>(UnlockState.all, 2);
        var run = RunState.CreateForTest([player, other], seed: "AFFINITY-BACKS");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = 1;
        var combat = new CombatState(runState: run); combat.AddPlayer(player); combat.AddPlayer(other);
        var enemy = combat.CreateCreature(ModelDb.Monster<Axebot>().ToMutable(), CombatSide.Enemy, null); combat.AddCreature(enemy);
        CombatManager.Instance.SetUpCombat(combat);
        typeof(CombatManager).GetProperty(nameof(CombatManager.IsInProgress))!.SetValue(CombatManager.Instance, true);
        var context = new ThrowingPlayerChoiceContext();
        var mana = combat.CreateCard<Mana>(player); var dense = combat.CreateCard<HighDensityMana>(player);
        var foreign = combat.CreateCard<Mana>(other); var strike = combat.CreateCard<StrikeFrieren>(player);
        foreach (var card in new CardModel[] { mana, dense, foreign, strike }) await CardPileCmd.Add(card, PileType.Hand);
        var affinity = combat.CreateCard<ManaAffinity>(player);
        Check(affinity.Type == CardType.Power && affinity.Rarity == CardRarity.Uncommon && affinity.EnergyCost.Canonical == 1, "一费罕见能力");
        affinity.UpgradeInternal(); affinity.FinalizeUpgradeInternal();
        Check(affinity.EnergyCost.GetWithModifiers(CostModifiers.Local) == 0, "升级后零费");
        Check(!mana.ShouldRetainThisTurn, "施放前魔力没有保留");
        var power = await PowerCmd.Apply<ManaAffinityPower>(context, player.Creature, 1, player.Creature, affinity);
        Check(mana.Keywords.Contains(CardKeyword.Retain) && dense.Keywords.Contains(CardKeyword.Retain), "普通和高密度魔力都获得保留关键字");
        var later = combat.CreateCard<Mana>(player); await CardPileCmd.Add(later, PileType.Hand);
        Check(later.ShouldRetainThisTurn, "后来生成的魔力也获得保留");
        Check(!foreign.ShouldRetainThisTurn && !strike.ShouldRetainThisTurn, "不影响其他玩家或普通牌");
        dense.AddKeyword(CardKeyword.Retain);
        await PowerCmd.Remove(power);
        Check(!mana.ShouldRetainThisTurn && dense.ShouldRetainThisTurn, "移除能力自动恢复且保留其他来源的保留");
        Check(player.Character.CardPool.GetUnlockedCards(UnlockState.all, CardMultiplayerConstraint.None).OfType<ManaAffinity>().Any(), "进入芙莉莲普通卡池");
        Check(player.Character.CardPool.GetUnlockedCards(UnlockState.all, CardMultiplayerConstraint.None).OfType<Snuggle>().Any(), "贴贴进入芙莉莲普通卡池");
        var selector = new TestCardSelector(); using var selections = CardSelectCmd.UseSelector(selector);
        Task<Snuggle> PrepareSnuggle(bool upgraded = false) {
            foreach (var pile in player.PlayerCombatState!.AllPiles)
                foreach (var card in pile.Cards.ToArray()) pile.RemoveInternal(card);
            player.PlayerCombatState.Energy = 3;
            var cardModel = combat.CreateCard<Snuggle>(player);
            if (upgraded) { cardModel.UpgradeInternal(); cardModel.FinalizeUpgradeInternal(); }
            return Task.FromResult(cardModel);
        }
        async Task PlaySnuggle(Snuggle card) {
            await CardPileCmd.Add(card, PileType.Hand);
            await card.OnPlayWrapper(context, null, false, new ResourceInfo { EnergySpent = 0, EnergyValue = 0, StarsSpent = 0, StarValue = 0 }, skipCardPileVisuals: true);
        }
        var snuggle = await PrepareSnuggle();
        var discardMana = combat.CreateCard<Mana>(player); await CardPileCmd.Add(discardMana, PileType.Hand);
        selector.PrepareToSelect(new[] { 0 }); selector.PrepareToSelect(new[] { 0 });
        await PlaySnuggle(snuggle);
        Check(snuggle.EnergyCost.Canonical == 0 && snuggle.DynamicVars["Amount"].IntValue == 1, "贴贴零费，普通版最多弃一张");
        Check(discardMana.Pile?.Type == PileType.Discard && player.PlayerCombatState!.Energy == 4, "第一分支弃魔力并获得一能量");
        snuggle = await PrepareSnuggle(true);
        var firstMana = combat.CreateCard<Mana>(player); var secondMana = combat.CreateCard<HighDensityMana>(player);
        await CardPileCmd.Add(firstMana, PileType.Hand); await CardPileCmd.Add(secondMana, PileType.Hand);
        selector.PrepareToSelect(new[] { 0 }); selector.PrepareToSelect(new[] { 0, 1 });
        await PlaySnuggle(snuggle);
        Check(snuggle.DynamicVars["Amount"].IntValue == 2 && player.PlayerCombatState!.Energy == 5 && secondMana.Pile?.Type == PileType.Discard, "升级只提高第一分支至两张，高密度魔力弃置时不额外触发消耗收益");
        snuggle = await PrepareSnuggle();
        var ordinary = combat.CreateCard<StrikeFrieren>(player); await CardPileCmd.Add(ordinary, PileType.Hand);
        selector.PrepareToSelect(new[] { 0 }); selector.PrepareToSelect(new[] { 0 });
        await PlaySnuggle(snuggle);
        Check(ordinary.Pile?.Type == PileType.Discard && player.PlayerCombatState!.Energy == 3, "沿用一代实际效果：可弃普通牌，但不获得能量");
        snuggle = await PrepareSnuggle(); await CardPileCmd.Add(combat.CreateCard<Mana>(player), PileType.Hand);
        selector.PrepareToSelect(new[] { 0 }); selector.PrepareToSelect(Array.Empty<int>());
        await PlaySnuggle(snuggle);
        Check(ManaCmd.InPile(player, PileType.Hand).Count() == 1 && player.PlayerCombatState!.Energy == 3, "允许选择弃零张");
        foreach (bool upgraded in new[] { false, true }) {
            snuggle = await PrepareSnuggle(upgraded);
            selector.PrepareToSelect(new[] { 1 }); await PlaySnuggle(snuggle);
            Check(ManaCmd.InPile(player, PileType.Hand).Count() == 1 && player.PlayerCombatState!.Energy == 3, "第二分支普通和升级都只生成一张魔力");
        }
        Check(!player.Character.CardPool.GetUnlockedCards(UnlockState.all, CardMultiplayerConstraint.None).Any(c => c is Snuggle1 or Snuggle2), "二选一选项不进入普通卡池");
        var state = new SlotBackProgress();
        Check(SlotBackCatalog.Ids.Count == 104 && SlotBackCatalog.Ids.Distinct().Count() == 104, "104张原版卡背ID完整无重复");
        Check(state.Unlocked.Count == 3 && state.Selected.SequenceEqual(new[] { "0001", "0002", "0003" }), "三个原版初始背景");
        Check(!state.Select(0, "0004") && !state.Select(3, "0001"), "锁定背景及非法槽位无法装载");
        Check(state.Unlock("0004") && !state.Unlock("0004") && !state.Unlock("bogus"), "解锁去重并拒绝非法ID");
        Check(state.Select(1, "0004") && SlotBackProgress.FromJson(state.ToJson()).Selected[1] == "0004", "选择和解锁进度序列化往返");
        var rolled = state.Roll('1', _ => 0)!; state.Unlock(rolled);
        Check(rolled != state.Roll('1', _ => 0), "随机奖励排除已收藏背景");
        Check(state.CollectionRewards().Count() == 3 && !state.CollectionRewards().Any(), "纪念背景只赠送一次");
        state.Unlocked.UnionWith(["2007", "4013", "1008"]);
        Check(state.CollectionRewards().Contains("9003"), "勇者小队成就遵循一代成员集合");
        state.Unlocked.UnionWith(SlotBackCatalog.Ids);
        Check(state.Roll('3', _ => 0) == null, "收集完同组背景后不再重复随机奖励");
        var restored = SlotBackProgress.FromJson("{\"Unlocked\":[\"bogus\"],\"Selected\":[\"bogus\",\"bogus\",\"bogus\"]}");
        Check(restored.Selected.SequenceEqual(new[] { "0001", "0002", "0003" }), "旧或无效选项回退到初始背景");
        typeof(CombatManager).GetProperty(nameof(CombatManager.IsInProgress))!.SetValue(CombatManager.Instance, false);
        RunManager.Instance.CleanUp();
        return count;
    }
}

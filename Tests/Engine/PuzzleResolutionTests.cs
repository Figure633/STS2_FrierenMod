using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
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
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;

public static class PuzzleResolutionTests
{
    public static async Task<int> Run()
    {
        int count = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception(name); GD.Print("PASS 谜题结算：" + name); count++; }
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "FRIEREN-PUZZLE-INSTANT");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = 1;
        var combat = new CombatState(runState: run); combat.AddPlayer(player);
        Creature[] SpawnEnemies()
        {
            var spawned = Enumerable.Range(0, 2).Select(_ => combat.CreateCreature(
                ModelDb.Monster<MegaCrit.Sts2.Core.Models.Monsters.Axebot>().ToMutable(), CombatSide.Enemy, null)).ToArray();
            foreach (var enemy in spawned) combat.AddCreature(enemy);
            return spawned;
        }
        var enemies = SpawnEnemies();
        CombatManager.Instance.SetUpCombat(combat);
        AccessTools.Property(typeof(CombatManager), nameof(CombatManager.IsInProgress)).SetValue(CombatManager.Instance, true);
        var context = new BlockingPlayerChoiceContext();
        CardModel card = combat.CreateCard<RapidChant>(player);
        var play = new CardPlay { Card = card, Target = null, IsAutoPlay = false, PlayIndex = 0, ResultPile = PileType.Discard, PlayCount = 1,
            Resources = new ResourceInfo { EnergySpent = 0, EnergyValue = 0, StarsSpent = 0, StarValue = 0 } };
        var ids = new[] { "Factor100", "Factor010", "Factor001" };
        var deck = new MagicDeckState();
        for (int i = 0; i < 3; i++) { deck.PrepareDraft(_ => 0); deck.SelectInitial(deck.Draft[0]); }
        for (int i = 0; i < ids.Length; i++) deck.Equip(deck.Add(ids[i]).InstanceId, (ManaSlot)i);
        MagicDeckStore.Write(player, deck);
        int Progress() => player.Creature.GetPowerAmount<PuzzlePower>();
        bool Alive() => enemies.All(enemy => enemy.IsAlive);
        async Task Reset()
        {
            foreach (var power in player.Creature.Powers.ToArray()) await PowerCmd.Remove(power);
            foreach (var pile in player.PlayerCombatState!.AllPiles)
                foreach (var mana in pile.Cards.ToArray()) pile.RemoveInternal(mana);
            foreach (var slot in Enum.GetValues<ManaSlot>()) await ManaCmd.Generate(player, ManaCmd.Pile(slot), 8);
        }
        Task Chant(int stage, ManaSlot? slot = null) => ChantCmd.Resolve(context, play,
            new(slot ?? (ManaSlot)stage, ids[stage], 1, 2 * (stage + 1), 1));

        await Reset(); await Chant(0, ManaSlot.Hand);
        Check(Progress() == 0 && Alive(), "错误槽位不推进谜题或击杀");
        await Chant(1);
        Check(Progress() == 0 && Alive(), "跳过第一段不触发");
        await Reset(); await Chant(0); await Chant(2);
        Check(Progress() == 0 && Alive(), "顺序错误清除进度且不击杀");
        await Reset(); await Chant(0); await Chant(0);
        Check(Progress() == 0 && Alive(), "重复第一段仍按原规则清除进度");
        await Reset(); await Chant(0); await Chant(1);
        await PowerCmd.Apply<StrengthPower>(context, player.Creature, 1, player.Creature, card);
        Check(Progress() == 2 && Alive(), "前两段与其他能力变化均不提前触发");
        var incomplete = player.Creature.GetPower<PuzzlePower>()!;
        await incomplete.AfterSideTurnEnd(context, CombatSide.Player, [player.Creature]);
        Check(Progress() == 0 && Alive(), "未完成的进度仍在回合结束清除");
        await Reset(); await Chant(0); await Chant(1);
        int manaBefore = ManaCmd.InPile(player, PileType.Discard).Count();
        int hp = player.Creature.CurrentHp, round = combat.RoundNumber;
        await Chant(2);
        Check(enemies.All(enemy => enemy.IsDead) && Progress() == 3 && combat.RoundNumber == round,
            "第三段返回前两个敌人已通过原生命令死亡，无需结束回合");
        Check(player.Creature.CurrentHp == hp && ManaCmd.InPile(player, PileType.Discard).Count() == manaBefore - 6,
            "先支付正确牌堆魔力，击杀不影响玩家");
        // 击杀已走过原版胜利清理；终极咏唱使用独立的新战斗。
        CombatManager.Instance.Reset(graceful: true);
        combat = new CombatState(runState: run); combat.AddPlayer(player); enemies = SpawnEnemies();
        CombatManager.Instance.SetUpCombat(combat);
        AccessTools.Property(typeof(CombatManager), nameof(CombatManager.IsInProgress)).SetValue(CombatManager.Instance, true);
        MagicDeckStore.Write(player, deck);
        card = combat.CreateCard<FinalChant>(player);
        play = new CardPlay { Card = card, Target = null, IsAutoPlay = false, PlayIndex = 0, ResultPile = PileType.Discard, PlayCount = 1,
            Resources = new ResourceInfo { EnergySpent = 0, EnergyValue = 0, StarsSpent = 0, StarValue = 0 } };
        await Reset(); await ChantCmd.Final(context, play);
        Check(enemies.All(enemy => enemy.IsDead) && Progress() == 3,
            "终极咏唱按槽位顺序集齐时也立即击杀全部敌人");
        return count;
    }
}

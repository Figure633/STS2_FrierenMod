using FrierenMod.Scripts.Characters;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;

public static class CardPoolIsolationTests
{
    private static List<Reward> Offered = [];
    private static bool Capture(List<Reward> rewards, ref Task __result)
    { Offered = rewards; __result = Task.CompletedTask; return false; }

    public static async Task<int> Run()
    {
        int count = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception(name); GD.Print("PASS 卡池隔离：" + name); count++; }
        // 两个玩家故意共用 UnlockState，验证限制依据实际接收者，而非本地角色或解锁对象。
        var foreign = Player.CreateForNewRun<Ironclad>(UnlockState.all, 1);
        var frieren = Player.CreateForNewRun<Frieren>(UnlockState.all, 2);
        var run = RunState.CreateForTest([foreign, frieren], seed: "FRIEREN-POOL-ISOLATION");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = 1;
        var pool = ModelDb.CardPool<FrierenCardPool>();
        bool Allowed(CardModel card) => card.Pool is not FrierenCardPool;
        var gem = ModelDb.Relic<PrismaticGem>().ToMutable(); gem.Owner = foreign;
        var options = gem.ModifyCardRewardCreationOptions(foreign,
            CardCreationOptions.ForNonCombatWithDefaultOdds([foreign.Character.CardPool]).WithFlags(CardCreationFlags.IsCardReward));
        Check(options.CardPools.All(p => p is not FrierenCardPool) && options.CardPools.Count > 1,
            "棱彩宝石保留原版跨角色奖励，排除芙莉莲池");
        var own = ModelDb.Relic<PrismaticGem>().ToMutable(); own.Owner = frieren;
        Check(own.ModifyCardRewardCreationOptions(frieren,
            CardCreationOptions.ForNonCombatWithDefaultOdds([pool]).WithFlags(CardCreationFlags.IsCardReward)).CardPools.Contains(pool),
            "同局芙莉莲仍可获得自己的卡牌");
        var explicitPool = CardCreationOptions.ForNonCombatWithDefaultOdds([pool, foreign.Character.CardPool]);
        Check(explicitPool.GetPossibleCards(foreign).Any() && explicitPool.GetPossibleCards(foreign).All(Allowed),
            "自定义事件的混合奖励候选也执行隔离");
        Check(explicitPool.GetPossibleCards(frieren).Any(c => c.Pool == pool) && pool.AllCards.Any(),
            "不删除注册模型或修改全局卡池");

        var capture = new Harmony("frieren.tests.pool_isolation");
        capture.Patch(AccessTools.Method(typeof(RewardsCmd), nameof(RewardsCmd.OfferCustom)),
            prefix: new HarmonyMethod(typeof(CardPoolIsolationTests), nameof(Capture)));
        try {
            var kaleidoscope = ModelDb.Relic<Kaleidoscope>().ToMutable(); kaleidoscope.Owner = foreign;
            for (int i = 0; i < 20; i++) {
                await kaleidoscope.AfterObtained();
                if (Offered.Count != 2 || Offered.Cast<CardReward>().Any(r => r.Cards.Count() != 3 || r.Cards.Any(c => !Allowed(c))))
                    throw new Exception("万花筒抽到芙莉莲或空池");
            }
            Check(true, "实际原版万花筒连续20次生成完整两组三选一，无芙莉莲与空池");
            var original = run.CreateCard(foreign.Character.StartingDeck.First(), foreign);
            var mixed = UnlockState.all.CharacterCardPools.SelectMany(p => p.AllCards).ToArray();
            for (int i = 0; i < 50; i++)
                if (!Allowed(CardFactory.CreateRandomCardForTransform(original, mixed, false, run.Rng.Niche)))
                    throw new Exception("变化获得芙莉莲卡牌");
            Check(true, "显式混合卡池的原生变化入口同样隔离");
            var combat = new CombatState(runState: run); combat.AddPlayer(foreign); combat.AddPlayer(frieren);
            CombatManager.Instance.SetUpCombat(combat);
            var distinct = CardFactory.GetDistinctForCombat(foreign, mixed, 20, run.Rng.CombatCardGeneration).ToArray();
            var repeated = CardFactory.GetForCombat(foreign, mixed, 50, run.Rng.CombatCardGeneration).ToArray();
            Check(distinct.Length == 20 && repeated.Length == 50 && distinct.Concat(repeated).All(Allowed),
                "飞溅等原生战斗生成入口按接收者过滤");
        }
        finally { capture.UnpatchAll(capture.Id); }
        return count;
    }
}


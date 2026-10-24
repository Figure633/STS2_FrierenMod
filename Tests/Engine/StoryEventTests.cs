using BaseLib.Patches.Hooks;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Encounters;
using FrierenMod.Scripts.Events;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Monsters;
using FrierenMod.Scripts.Powers;
using FrierenMod.Scripts.Relics;
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
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;

public static class StoryEventTests
{
    private static bool ForcedEndRequested;
    private static bool CaptureEnd(bool canBackOut) { ForcedEndRequested = !canBackOut; return false; }
    public static async Task<int> Run(Node root)
    {
        int count = 0;
        void Check(bool ok, string text) { if (!ok) throw new Exception(text); count++; GD.Print("PASS 一代事件：" + text); }
        var manager = RunManager.Instance;
        SaveManager.Instance.InitPrefsDataForTest();
        LocalContext.NetId = 1;
        bool started = false;
        RunState Start(Player player) {
            if (started) manager.CleanUp(); started = true;
            var run = RunState.CreateForTest([player], seed: "FRIEREN-STORY-EVENTS");
            manager.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = 1; return run;
        }
        async Task<T> Story<T>(int gold = 0, int hp = 20, int maxHp = 75) where T : EventModel {
            var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1); Start(player);
            player.Creature.SetMaxHpInternal(maxHp); player.Creature.SetCurrentHpInternal(hp);
            if (player.Gold > 0) await PlayerCmd.LoseGold(player.Gold, player);
            if (gold > 0) await PlayerCmd.GainGold(gold, player);
            var story = (T)ModelDb.Event<T>().ToMutable(); await story.BeginEvent(player, false); return story;
        }
        var originals = new EventModel[] { ModelDb.Event<FoodEvent>(), ModelDb.Event<KraftGift>(), ModelDb.Event<AnimalWell>(), ModelDb.Event<MimicFight>() };
        var iron = Player.CreateForNewRun<Ironclad>(UnlockState.all, 1); var otherRun = Start(iron);
        Check(originals.All(e => !e.IsAllowed(otherRun)), "原版角色不抽到芙莉莲专属事件");
        var own = Player.CreateForNewRun<Frieren>(UnlockState.all, 1); var ownRun = Start(own);
        Check(ModelDb.Event<FoodEvent>().IsAllowed(ownRun) && ModelDb.Event<AnimalWell>().IsAllowed(ownRun) &&
            !ModelDb.Event<KraftGift>().IsAllowed(ownRun) && !ModelDb.Event<MimicFight>().IsAllowed(ownRun), "第一幕事件范围正确");
        Check(originals.All(e => ModelDb.AllSharedEvents.Contains(e)), "四个事件均进入原生跨幕随机池");
        foreach (var language in new[] { "zhs", "eng" }) {
            LocManager.Instance.SetLanguage(language);
            foreach (var canonical in originals) {
                var story = canonical.ToMutable(); await story.BeginEvent(own, false);
                foreach (var option in story.CurrentOptions) story.DynamicVars.AddTo(option.Description);
                Check(story.CreateInitialPortrait().GetWidth() > 0 && !story.Title.GetFormattedText().Contains("FRIERENMOD-") &&
                    story.CurrentOptions.All(o => !o.Title.GetFormattedText().Contains("LOC_ERR") && !o.Description.GetFormattedText().Contains("LOC_ERR")),
                    language + "原图、标题、选项完整：" + canonical.Id.Entry);
            }
        }
        var food = await Story<FoodEvent>(); Check(food.CurrentOptions.Skip(1).All(o => o.IsLocked), "零金币仅能免费试吃");
        await food.CurrentOptions[0].Chosen(); Check(food.IsFinished && food.Owner!.Creature.CurrentHp == 35, "免费试吃治疗15并完成事件");
        food = await Story<FoodEvent>(75); await food.CurrentOptions[1].Chosen();
        Check(food.Owner!.Gold == 0 && food.Owner.Deck.Cards.OfType<EmergencyFood>().Count() == 1 && food.IsFinished, "打包扣75并将应急食物加入主牌组");
        food = await Story<FoodEvent>(150, 1, 80); await food.CurrentOptions[2].Chosen();
        Check(food.Owner!.Gold == 0 && food.Owner.GetRelic<Strawberry>() != null && food.Owner.Creature.CurrentHp == 68,
            "正餐按获得遗物前最大生命75%治疗");
        food = await Story<FoodEvent>(250); await food.CurrentOptions[3].Chosen();
        Check(food.Owner!.Gold == 0 && food.Owner.GetRelic<Mango>() != null && food.Owner.Creature.CurrentHp == food.Owner.Creature.MaxHp,
            "大餐原生芒果奖励并恢复全部生命");
        var kraft = await Story<KraftGift>(299, 20, 79); Check(kraft.CurrentOptions.Take(3).All(o => o.IsLocked), "克拉伏特三个条件不足时锁定");
        kraft = await Story<KraftGift>(300, 20, 80); await kraft.CurrentOptions[1].Chosen();
        Check(kraft.Owner!.Gold == 300 && kraft.Owner.GetRelic<MembershipCard>() != null, "会员卡检查持有金币且不扣钱");
        kraft = await Story<KraftGift>(0, 20, 80); await kraft.CurrentOptions[0].Chosen();
        Check(kraft.Owner!.GetRelic<MeatOnTheBone>() != null, "80最大生命可领取带骨肉");
        kraft = await Story<KraftGift>(); while (kraft.Owner!.Deck.Cards.Count < 30) await CardPileCmd.Add(kraft.Owner.RunState.CreateCard<StrikeFrieren>(kraft.Owner), PileType.Deck);
        // 原生选项在进事件时冻结，重新开始以计算新的可选条件。
        var fullDeck = (KraftGift)ModelDb.Event<KraftGift>().ToMutable(); await fullDeck.BeginEvent(kraft.Owner, false); await fullDeck.CurrentOptions[2].Chosen();
        var ring = kraft.Owner.GetRelic<KraftSerpentRing>()!;
        Check(ring != null && ring.ModifyHandDraw(kraft.Owner, 5) == 6 && ring.Icon.GetWidth() > 0 && ring.IconOutline.GetWidth() > 0,
            "30张牌获得每回合额外抽一张的长蛇戒指，原生图标可用");
        var well = await Story<AnimalWell>(); await well.CurrentOptions[0].Chosen();
        Check(well.IsFinished && well.Owner!.Potions.OfType<FirePotion>().Count() == 1 && well.Owner.Potions.OfType<ExplosiveAmpoule>().Count() == 1,
            "动物井原生奖励领取两种药水");
        well = await Story<AnimalWell>(); var selector = new TestCardSelector(); using (CardSelectCmd.UseSelector(selector)) {
            var chosen = well.Owner!.Deck.Cards[0]; selector.PrepareToSelect([0]); await well.CurrentOptions[1].Chosen();
            Check(chosen.IsUpgraded && well.IsFinished, "动物井经原生牌组选牌升级一张");
        }
        well = await Story<AnimalWell>(); await well.CurrentOptions[2].Chosen();
        Check(well.Owner!.Deck.Cards.OfType<BirdCapturingSpell>().Count() == 1 && well.IsFinished, "动物井获得捕鸟魔法");
        var mimic = await Story<MimicFight>(); await mimic.CurrentOptions[0].Chosen();
        Check(mimic.CurrentOptions.Count == 3 && mimic.Description!.LocEntryKey.Contains("CAUGHT"), "宝箱两种打开选项进入被吞页面");
        await mimic.CurrentOptions[1].Chosen(); Check(mimic.Owner!.Creature.CurrentHp == 5 && mimic.Owner.Deck.Cards.OfType<RingletForm>().Count() == 1 && mimic.IsFinished,
            "爆破失去15生命并获得卷发形态");
        mimic = await Story<MimicFight>(); await mimic.CurrentOptions[1].Chosen(); await mimic.CurrentOptions[2].Chosen();
        Check(mimic.Owner!.Creature.CurrentHp == 20 && mimic.IsFinished, "菲伦救援免费结束事件");
        var player = mimic.Owner; var run = (RunState)player.RunState;
        var encounter = ModelDb.Encounter<MimicEncounter>().ToMutable(); encounter.GenerateMonstersWithSlots(run);
        var combat = new CombatState(encounter, run, [], [], run.MultiplayerScalingModel); combat.AddPlayer(player);
        var monster = (Mimic)ModelDb.Monster<Mimic>().ToMutable(); var enemy = combat.CreateCreature(monster, CombatSide.Enemy, null); combat.AddCreature(enemy);
        CombatManager.Instance.SetUpCombat(combat); AccessTools.Property(typeof(CombatManager), nameof(CombatManager.IsInProgress)).SetValue(CombatManager.Instance, true);
        monster.RollMove([player.Creature]);
        Check(monster.NextMove.Id == "DISGUISE" && monster.MinInitialHp == 160 && monster.MaxInitialHp == 164, "宝箱怪初始血量和伪装意图");
        monster.RegisterSceneConversions();
        var visuals = monster.CreateVisuals(); root.AddChild(visuals);
        Check(visuals.Bounds.Size.X > 0 && visuals.GetCurrentBody().GetChild<Sprite2D>(0).Texture.GetWidth() > 0, "一代宝箱怪原图经BaseLib创建原生战斗节点"); visuals.QueueFree();
        await monster.AfterAddedToRoom(); var countdown = enemy.GetPower<MimicPower>()!;
        var context = new BlockingPlayerChoiceContext();
        await ManaCmd.Generate(player, PileType.Draw, 1); await ManaCmd.Generate(player, PileType.Hand, 1); await ManaCmd.Generate(player, PileType.Discard, 1);
        var play = new CardPlay { Card = combat.CreateCard<StrikeFrieren>(player), Target = enemy, IsAutoPlay = false, ResultPile = PileType.Discard,
            PlayCount = 1, PlayIndex = 0, Resources = new ResourceInfo { EnergySpent = 0, EnergyValue = 0, StarsSpent = 0, StarValue = 0 } };
        for (int i = 0; i < 4; i++) await countdown.AfterCardPlayedLate(context, play);
        Check(countdown.Amount == 1 && ManaCmd.Total(player) == 3, "前四张牌只减少伪装倒计时");
        var endTurn = new Harmony("frieren.tests.mimic.endturn");
        var endMethod = AccessTools.Method(typeof(PlayerCmd), nameof(PlayerCmd.EndTurn));
        endTurn.Patch(endMethod, prefix: new HarmonyMethod(typeof(StoryEventTests), nameof(CaptureEnd)));
        try { await countdown.AfterCardPlayedLate(context, play); }
        finally { endTurn.Unpatch(endMethod, HarmonyPatchType.Prefix, endTurn.Id); }
        Check(ForcedEndRequested && !enemy.HasPower<MimicPower>() && ManaCmd.Total(player) == 0 && player.Creature.HasPower<DarkPower>(), "第五张牌清除三个牌堆魔力、施加黑暗并请求原生强制结束回合");
        var darkness = player.Creature.GetPower<DarkPower>()!;
        for (int i = 0; i < 4; i++) await darkness.AfterPlayerTurnStart(context, player);
        Check(darkness.Adaptation == 4, "黑暗四回合恢复适应阶段");
        await PowerCmd.Apply<FrierenSuitcasePower>(context, player.Creature, 1, player.Creature, null);
        await PowerCmd.Apply<SwallowedPower>(context, player.Creature, 4, enemy, null);
        Check(MaxHandSizePatch.GetMaxHandSize(player, CardPile.MaxCardsInHand) == 9, "吞噬在行李箱13张手牌上限基础上扣减");
        var room = new CombatRoom(combat); var rewards = await RewardsCmd.GenerateForRoomEnd(player, room);
        Check(rewards.Rewards.OfType<RelicReward>().Count() == 2 && !rewards.Rewards.OfType<CardReward>().Any() &&
            rewards.Rewards.OfType<GoldReward>().Count() == 1, "战斗奖励经原生入口仅有100金币及两件遗物，无额外普通卡奖励");
        await RelicCmd.Obtain<FakeFlammeGrimoire>(player); var special = MimicEncounter.Rewards(player);
        Check(special.OfType<RelicReward>().Any(r => r.Relic is FlammeGrimoire) && special.OfType<RelicReward>().Any(r => r.Relic is MimicHead),
            "持有伪作时宝箱怪奖励换成真本著作并保留宝箱怪的头");
        LocManager.Instance.SetLanguage("eng"); manager.CleanUp(); return count;
    }
}

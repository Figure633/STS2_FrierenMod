using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.ValueProps;

public static class MissingCardTests
{
    public static async Task<int> Run()
    {
        int passed = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception("补牌测试：" + name); }
        void Pass(string name) { passed++; Console.WriteLine("PASS 补牌：" + name); }
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "FRIEREN-MISSING-CARDS");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService());
        MegaCrit.Sts2.Core.Context.LocalContext.NetId = 1;
        var combat = new CombatState(runState: run);
        combat.AddPlayer(player);
        var enemy = combat.CreateCreature(ModelDb.Monster<MegaCrit.Sts2.Core.Models.Monsters.Axebot>().ToMutable(), CombatSide.Enemy, null);
        combat.AddCreature(enemy);
        enemy.SetMaxHpInternal(9999); enemy.SetCurrentHpInternal(9999);
        CombatManager.Instance.SetUpCombat(combat);
        typeof(CombatManager).GetProperty(nameof(CombatManager.IsInProgress))!.SetValue(CombatManager.Instance, true);
        var context = new ThrowingPlayerChoiceContext();
        var selector = new TestCardSelector();
        using var selection = CardSelectCmd.UseSelector(selector);
        async Task Reset()
        {
            foreach (var creature in combat.Creatures)
            {
                foreach (var power in creature.Powers.ToArray()) await PowerCmd.Remove(power);
                creature.LoseBlockInternal(creature.Block); creature.SetCurrentHpInternal(creature.MaxHp);
            }
            foreach (var pile in player.PlayerCombatState!.AllPiles)
                foreach (var c in pile.Cards.ToArray()) pile.RemoveInternal(c);
            player.PlayerCombatState.Energy = 3;
            CombatManager.Instance.History.Clear();
        }
        T New<T>(bool upgraded = false) where T : CardModel
        {
            var c = combat.CreateCard<T>(player);
            if (upgraded) c.UpgradeInternal();
            return c;
        }
        CardPlay MakePlay(CardModel c, Creature? target = null) => new() { Card = c, Target = target, ResultPile = PileType.Discard,
            Resources = new ResourceInfo { EnergySpent = 0, EnergyValue = 0, StarsSpent = 0, StarValue = 0 }, PlayCount = 1, PlayIndex = 0, IsAutoPlay = false };
        async Task Play(CardModel c, Creature? target = null)
        {
            await CardPileCmd.Add(c, PileType.Hand);
            await c.OnPlayWrapper(context, target, false, MakePlay(c).Resources, skipCardPileVisuals: true);
        }
        int Mana(PileType pile) => ManaCmd.InPile(player, pile).Count();
        int Pow<T>() where T : PowerModel => player.Creature.GetPowerAmount<T>();

        await Reset();
        var dense = New<HighDensityMana>(true); await CardPileCmd.Add(dense, PileType.Draw);
        Check(Mana(PileType.Draw) == 1, "高密度魔力计数");
        Check(await ManaCmd.Spend(context, player, ManaSlot.Draw, 1), "高密度魔力支付");
        Check(player.PlayerCombatState!.Energy == 5 && dense.Pile?.Type == PileType.Exhaust, "高密度消耗返能");
        Pass("高密度魔力可支付咏唱并触发升级返能");

        await Reset(); await Play(New<Disperse>());
        Check(Mana(PileType.Draw) == 1 && Mana(PileType.Hand) == 2 && Mana(PileType.Discard) == 3, "分散生成");
        await Play(New<Recycle>()); Check(Mana(PileType.Draw) == 4 && Mana(PileType.Discard) == 3, "回收复制而非搬移");
        await Play(New<BackFlow>()); Check(Mana(PileType.Hand) == 5 && Mana(PileType.Discard) == 0, "逆流搬移");
        Pass("分散、回收、逆流的牌堆资源流转");

        await Reset(); await Play(New<ShavedIceSpell>(true)); await Play(New<SwampSpell>(true));
        Check(Mana(PileType.Draw) == 1 && Mana(PileType.Hand) == 4, "刨冰和泥沼升级");
        Pass("刨冰和泥沼升级数值");

        await Reset(); var parcel = New<ManaParcel>(true); await CardPileCmd.Add(parcel, PileType.Draw);
        await CardPileCmd.Draw(context, 1, player);
        Check(parcel.Pile?.Type == PileType.Exhaust && Mana(PileType.Hand) == 4, "魔力包抽到即消耗");
        Pass("魔力包真实抽牌触发生成与消耗");

        await Reset(); await ManaCmd.Generate(player, PileType.Draw, 2); await CardPileCmd.Add(New<StrikeFrieren>(), PileType.Draw);
        await Play(New<Apprehend>(), enemy);
        Check(Mana(PileType.Hand) == 0 && Mana(PileType.Discard) == 2 && enemy.CurrentHp == 9991, "领悟只丢抽出的魔力");
        Pass("领悟抽牌、伤害与条件丢弃");

        await Reset(); await ManaCmd.Generate(player, PileType.Draw, 2);
        await Play(New<TongueTwisterSpell>()); Check(player.Creature.Block == 10, "绕口令按实际魔力逐次格挡");
        Pass("绕口令逐张抽取结果计算格挡");

        await Reset(); await Play(New<ClearMind>(true));
        Check(Mana(PileType.Hand) == 3 && Pow<AccelerateFlowPower>() == 1, "澄澈生成与加速");
        await CardPileCmd.Add(New<StrikeFrieren>(), PileType.Draw); await CardPileCmd.Add(New<DefendFrieren>(), PileType.Draw);
        await SynchroCmd.Play(context, New<Mana>());
        Check(player.PlayerCombatState!.DrawPile.IsEmpty, "加速抽二");
        await Play(New<AirSlash>(), enemy); await ManaCmd.Generate(player, PileType.Draw, 4);
        Check(Mana(PileType.Draw) == 0 && Pow<BanManaGainPower>() == 1, "风刃禁魔");
        Pass("加速同调抽二与风刃禁魔");

        await Reset(); player.Creature.GainBlockInternal(29); await Play(New<ButtSlam>(), enemy);
        Check(enemy.CurrentHp == 9979 && Mana(PileType.Draw) == 4, "臀击按整十格挡命中");
        Pass("臀击固定次数与每次生成魔力");

        await Reset(); await CardPileCmd.Add(New<StrikeFrieren>(), PileType.Hand); await CardPileCmd.Add(New<DefendFrieren>(), PileType.Hand);
        await ManaCmd.Generate(player, PileType.Hand, 1); await Play(New<Free>(), enemy);
        Check(enemy.CurrentHp == 9995 && player.Creature.Block == 4 && Mana(PileType.Hand) == 1, "自由排除魔力");
        Pass("自由仅丢非魔力并逐张结算");

        await Reset(); await Play(New<LockTarget>(), enemy);
        Check(enemy.CurrentHp == 9983, "锁定无魔力平局触发两次");
        Pass("锁定目标的相等边界");

        await Reset(); var shooting = New<ContinuousShooting>(); await Play(shooting, enemy);
        Check(enemy.CurrentHp == 9990 && !shooting.Keywords.Contains(CardKeyword.Exhaust), "无魔力不消耗");
        await ManaCmd.Generate(player, PileType.Draw, 1); await Play(shooting, enemy);
        Check(shooting.Pile?.Type == PileType.Exhaust, "有魔力消耗进入消耗牌堆");
        Pass("连续射击动态消耗条件");

        await Reset(); await CardPileCmd.Add(New<StrikeFrieren>(), PileType.Hand); await CardPileCmd.Add(New<DefendFrieren>(), PileType.Hand);
        await Play(New<ElementsBombing>(true));
        var elements = player.PlayerCombatState!.Hand.Cards.Where(c => c is Ice or Fire or Thunder or Dark or Light).ToArray();
        Check(elements.Length == 2 && elements.All(c => c.IsUpgraded) && player.PlayerCombatState.ExhaustPile.Cards.Count(c => c is StrikeFrieren or DefendFrieren) == 2, $"元素轰炸交换整手 elements={elements.Length}, upgrades={string.Join(",", elements.Select(c => c.IsUpgraded))}");
        Pass("元素轰炸按手牌数消耗并生成升级元素");

        await Reset(); await Play(New<FireSpell>(true));
        Check(player.PlayerCombatState!.Hand.Cards.OfType<LittleFire>().Single().IsUpgraded, "火焰魔法按敌人数");
        await Play(New<FiveMinutesMore>(true)); Check(player.PlayerCombatState.Energy == 5 && player.PlayerCombatState.Hand.Cards.OfType<Laziness>().Count() == 1, "再睡五分钟");
        Pass("火焰魔法与再睡五分钟的升级临时牌");

        await Reset(); await Play(New<RingletForm>()); await Play(New<Outpouring>()); await Play(New<RitesPreparation>());
        var mana = New<Mana>(); await CardCmd.Exhaust(context, mana);
        Check(player.Creature.Block == 2, "卷发消耗魔力格挡");
        await player.Creature.GetPower<OutpouringPower>()!.AfterCardPlayed(context, MakePlay(mana));
        await player.Creature.GetPower<RitesPreparationPower>()!.AfterCardPlayed(context, MakePlay(mana));
        Check(Pow<StrengthPower>() == 1 && Pow<RitesPreparationPower>() == 2 && enemy.CurrentHp == 9994, "同调力量与仪式次数");
        Pass("卷发、涌流和仪式准备的资源触发");

        await Reset(); await Play(New<PajamasForm>());
        foreach (var pile in new[] { PileType.Draw, PileType.Hand, PileType.Discard }) await ManaCmd.Generate(player, pile, 1);
        await player.Creature.GetPower<PajamasFormPower>()!.BeforeSideTurnEnd(context, CombatSide.Player, [player.Creature]);
        Check(ManaCmd.Total(player) == 0 && Mana(PileType.Exhaust) == 3 && enemy.CurrentHp == 9987, "睡衣跨牌堆");
        Pass("睡衣形态消耗三个牌堆并按数量造成伤害");

        await Reset(); await Play(New<Perpetual>()); await PowerCmd.Apply<BanManaGainPower>(context, player.Creature, 1, player.Creature, null);
        await player.Creature.GetPower<PerpetualPower>()!.AfterPlayerTurnStart(context, player);
        Check(Mana(PileType.Discard) == 2, "永续忽略禁魔");
        await Play(New<Trick>()); await player.Creature.GetPower<TrickPower>()!.AfterPlayerTurnStart(context, player);
        Check(player.PlayerCombatState!.Hand.Cards.OfType<ManaConcealment>().Any(), "伎俩生成");
        Pass("永续的禁魔例外与伎俩回合生成");

        await Reset(); await Play(New<FlyingMagic>(true)); await ManaCmd.Generate(player, PileType.Discard, 2);
        await player.Creature.GetPower<FlyingMagicPower>()!.BeforeSideTurnEnd(context, CombatSide.Player, [player.Creature]);
        Check(Pow<IntangiblePower>() == 3 && Mana(PileType.Discard) == 1 && Mana(PileType.Exhaust) == 1, "飞行魔法消耗一");
        Pass("飞行魔法无实体与升级后的魔力消耗");

        await Reset(); await PowerCmd.Apply<StrengthPower>(context, player.Creature, 2, player.Creature, null);
        await PowerCmd.Apply<DexterityPower>(context, player.Creature, 5, player.Creature, null);
        await Play(New<RedAppleSpell>(true)); Check(Pow<StrengthPower>() == 5 && Pow<DexterityPower>() == 2, "红苹果交换");
        await PowerCmd.Apply<StrengthPower>(context, player.Creature, 1, player.Creature, null);
        await player.Creature.GetPower<SwitchStrengthAndDexterityPower>()!.BeforeSideTurnEnd(context, CombatSide.Player, [player.Creature]);
        Check(Pow<StrengthPower>() == 2 && Pow<DexterityPower>() == 6, "苹果交换当前值而非旧快照");
        await Reset();
        await PowerCmd.Apply<StrengthPower>(context, player.Creature, 2, player.Creature, null);
        await PowerCmd.Apply<DexterityPower>(context, player.Creature, 5, player.Creature, null);
        await PowerCmd.Apply<ArtifactPower>(context, player.Creature, 2, player.Creature, null);
        await CardFlowCmd.SwapStrengthAndDexterity(context, player, null);
        Check(Pow<ArtifactPower>() == 0 && Pow<StrengthPower>() == 7 && Pow<DexterityPower>() == 7, "苹果按一代顺序触发人工制品");
        Pass("苹果交换期间增益不丢失，保留人工制品触发顺序");

        await Reset(); await Play(New<FrierenSuitcase>());
        Check(BaseLib.Patches.Hooks.MaxHandSizePatch.GetMaxHandSize(player, CardPile.MaxCardsInHand) == 13, "手提箱手牌上限");
        for (int i = 0; i < 13; i++) await CardPileCmd.Add(New<StrikeFrieren>(), PileType.Hand);
        await player.Creature.GetPower<FrierenSuitcasePower>()!.AfterCardPlayedLate(context, MakePlay(New<Mana>()));
        Check(player.Creature.Block == 3, "手提箱满手格挡");
        Pass("手提箱使用BaseLib所属玩家手牌上限钩子");

        await Reset(); await Play(New<OpenTheWaygate>(true)); var gate = player.Creature.GetPower<OpenTheWaygatePower>()!;
        for (int i = 0; i < 3; i++) await gate.AfterCardPlayed(context, MakePlay(New<Mana>()));
        Check(!gate.ShouldTakeExtraTurn(player), "时空门提前触发");
        await gate.AfterCardPlayed(context, MakePlay(New<HighDensityMana>()));
        Check(gate.ShouldTakeExtraTurn(player), "时空门四连"); await gate.AfterTakingExtraTurn(player);
        Check(!player.Creature.HasPower<OpenTheWaygatePower>(), "时空门一次性");
        Pass("时空之门连续同调阈值与一次性额外回合");

        await Reset(); var custom = New<CustomLegendarySpell>();
        custom.Configure(2, [new("ManaHand",4), new("DamageTriple",5), new("Dexterity",4)]);
        Check(custom.Type == CardType.Attack && custom.TargetType == TargetType.AnyEnemy && custom.Keywords.Contains(CardKeyword.Exhaust), "自定义模型行为");
        await Play(custom, enemy);
        Check(enemy.CurrentHp == 9984 && Mana(PileType.Hand) == 4 && Pow<DexterityPower>() == 4, "三层传奇结算");
        var saved = custom.ToSerializable();
        Pass("自定义传奇三层组合、攻击类型与消耗");

        await Reset(); player.Creature.SetCurrentHpInternal(40); player.Creature.GainBlockInternal(7);
        await PowerCmd.Apply<StrengthPower>(context, player.Creature, 3, player.Creature, null);
        var oldCard = New<StrikeFrieren>(); await CardPileCmd.Add(oldCard, PileType.Draw);
        await Play(New<TimeConcept>()); var time = player.Creature.GetPower<TimeTravelPower>()!;
        player.Creature.SetCurrentHpInternal(20); player.Creature.LoseBlockInternal(7); player.PlayerCombatState!.Energy = 0;
        await CardPileCmd.Add(oldCard, PileType.Exhaust); await ManaCmd.Generate(player, PileType.Hand, 3);
        await PowerCmd.Apply<StrengthPower>(context, player.Creature, 2, player.Creature, null);
        await time.AfterPlayerTurnStartEarly(context, player); Check(player.Creature.CurrentHp == 20, "回溯不能提前");
        await time.AfterPlayerTurnStartEarly(context, player);
        Check(player.Creature.CurrentHp == 40 && player.Creature.Block == 7 && player.PlayerCombatState.Energy == 3 && Pow<StrengthPower>() == 3, "回溯自身状态");
        Check(oldCard.Pile?.Type == PileType.Draw && ManaCmd.Total(player) == 0 && player.PlayerCombatState.Hand.Cards.Any(c => c is MegaCrit.Sts2.Core.Models.Cards.Regret), "回溯牌堆与悔恨");
        Pass("时间概念两回合快照、状态隔离与未升级悔恨");
        await Reset(); var recallTarget = New<StrikeFrieren>(); await CardPileCmd.Add(recallTarget, PileType.Exhaust);
        await Play(New<Recall>()); Check(recallTarget.Pile?.Type == PileType.Hand && recallTarget.ShouldRetainThisTurn, "回忆取回并保留");
        await CardPileCmd.Add(New<DefendFrieren>(), PileType.Hand);
        await Play(New<GrapeSpell>()); Check(Mana(PileType.Hand) == 2, "葡萄消耗再生");
        Pass("回忆与葡萄的选牌和消耗流程");

        await Reset(); var selectedCard = New<StrikeFrieren>(); await CardPileCmd.Add(selectedCard, PileType.Draw);
        await Play(New<PancakeSpell>()); Check(selectedCard.Pile?.Type == PileType.Draw, "松饼魔力不足");
        await ManaCmd.Generate(player, PileType.Hand, 2);
        selector.PrepareToSelect(new[] { 0 });
        await Play(New<PancakeSpell>()); Check(Mana(PileType.Hand) == 0 && player.PlayerCombatState!.Hand.Cards.Count >= 1, "松饼消耗资源并选牌");
        Pass("松饼魔力不足不选牌、支付后检索");

        await Reset(); await Play(New<BabySleeping>());
        Check(player.Creature.HasPower<RetainHandPower>(), "宝宝熟睡整手保留");
        await Play(New<Kiss>(), enemy);
        Check(enemy.GetPowerAmount<WeakPower>() == 99 && enemy.GetPowerAmount<VulnerablePower>() == 99, "飞吻减益");
        await Play(New<RestraintMagic>(), enemy);
        Check(enemy.GetPowerAmount<StrengthPower>() == -3 && enemy.HasPower<SlowPower>(), "拘束魔法迟缓");
        Pass("宝宝熟睡、飞吻和拘束魔法的原生能力");

        await Reset(); await CardPileCmd.Add(New<StrikeFrieren>(), PileType.Draw); await ManaCmd.Generate(player, PileType.Draw, 2);
        await Play(New<BreaksBarriersSpell>()); Check(Mana(PileType.Hand) == 2 && player.PlayerCombatState!.DrawPile.Cards.Single() is StrikeFrieren, "障壁魔法排序");
        Pass("破除障壁把魔力放在抽牌顶端");

        await Reset(); var plain = New<Mana>(); await CardPileCmd.Add(plain, PileType.Exhaust); await CardPileCmd.Add(New<HighDensityMana>(), PileType.Exhaust);
        await Play(New<OrdinaryOffensiveMagic>(), enemy); Check(enemy.CurrentHp == 9995, "普通攻击包括高密度");
        await Play(New<ManaDetection>(), enemy);
        await Play(New<StrikeFrieren>(), enemy); Check(Mana(PileType.Discard) == 1, "魔力探测返还");
        Pass("普通攻击统计消耗魔力、魔力探测攻击返还");

        await Reset(); var exhausted = New<StrikeFrieren>(); await CardPileCmd.Add(exhausted, PileType.Draw);
        await ManaCmd.Generate(player, PileType.Hand, 1);
        await Play(New<SayThinkingSpell>());
        Check(enemy.CurrentHp == 9993 && Mana(PileType.Exhaust) == 1, "说出心声自动打牌");
        Pass("说出心声消耗魔力并免费打出牌堆顶牌");

        await Reset(); var xCard = New<MultipleOffensiveMagic>(true); xCard.EnergyCost.CapturedXValue = 2;
        await Play(xCard, enemy);
        Check(enemy.CurrentHp == 9981 && Mana(PileType.Hand) == 2, "多重攻击X费升级");
        Pass("多重攻击使用原生X值计算伤害次数及魔力");

        await Reset(); var denseInDraw = New<HighDensityMana>(); await CardPileCmd.Add(denseInDraw, PileType.Draw);
        // 用雷电同调避免抽牌，让连续同调检索路径独立受测。
        await PowerCmd.Apply<FrierenMod.Scripts.Powers.ThunderPower>(context, player.Creature, 1, player.Creature, null);
        for (int i = 0; i < 3; i++) CombatManager.Instance.History.CardPlayStarted(combat, MakePlay(New<Mana>()));
        await SynchroCmd.Play(context, New<Mana>());
        Check(denseInDraw.Pile?.Type == PileType.Hand, "三连同调高密度检索");
        await Play(New<MageStyleFinisher>(), enemy);
        Check(enemy.CurrentHp == 9969, "法师终结技次数");
        Pass("三连同调检索高密度及终结技统计");

        await Reset(); var grand = New<GrandCross>(); await CardPileCmd.Add(grand, PileType.Hand);
        Check(grand.EnergyCost.GetWithModifiers(CostModifiers.All) == 2, "十字斩初始费用");
        CombatManager.Instance.History.CardPlayStarted(combat, MakePlay(New<RapidChant>()));
        Check(grand.EnergyCost.GetWithModifiers(CostModifiers.All) == 0, "十字斩咏唱后费用");
        await Play(New<AccessoriesSpell>());
        var room = new MegaCrit.Sts2.Core.Rooms.CombatRoom(combat);
        await player.Creature.GetPower<GainRelicPower>()!.AfterCombatVictory(room);
        Check(room.ExtraRewards[player].OfType<MegaCrit.Sts2.Core.Rewards.RelicReward>().Count() == 1, "饰品胜利奖励");
        Pass("十字斩咏唱费用与饰品魔法胜利奖励");

        await Reset(); for (int i = 0; i < 4; i++) selector.PrepareToSelect(new[] { 0 });
        await Play(New<SerieGrimoire>(true));
        var crafted = player.PlayerCombatState!.Hand.Cards.OfType<CustomLegendarySpell>().Single();
        Check(crafted.Effects.Length == 3 && crafted.CraftedCost == 0, "魔导书四阶段选择");
        foreach (var effect in crafted.Effects.Select((e, i) => (e, i))) Check(LegendaryCraft.Options(0, effect.i).Contains(effect.e), "魔导书候选层次");
        var serialized = crafted.ToSerializable();
        var roundTrip = CardModel.FromSerializable(serialized);
        Check(roundTrip is CustomLegendarySpell restored && restored.Recipe == crafted.Recipe && restored.CraftedCost == crafted.CraftedCost, "自定义存档");
        Pass("赛丽艾魔导书四阶段原生选择及自定义牌存档");

        await Reset();
        for (int i = 0; i < 9; i++) CombatManager.Instance.History.CardPlayStarted(combat, MakePlay(New<StrikeFrieren>()));
        var collapse = New<ManaCollapse>(); await Play(collapse, enemy);
        Check(Mana(PileType.Hand) == 2 && collapse.Pile?.Type == PileType.Draw, $"魔力崩坏前十张回抽牌堆 mana={Mana(PileType.Hand)}, pile={collapse.Pile?.Type}");
        CombatManager.Instance.History.CardPlayStarted(combat, MakePlay(New<StrikeFrieren>()));
        CombatManager.Instance.History.CardPlayStarted(combat, MakePlay(New<StrikeFrieren>()));
        await Play(collapse, enemy);
        Check(Mana(PileType.Hand) == 2 && collapse.Pile?.Type == PileType.Discard, "魔力崩坏超限");
        Pass("魔力崩坏的前十张边界和回牌堆结算");

        await Reset();
        foreach (var pile in new[] { PileType.Draw, PileType.Hand, PileType.Discard }) await ManaCmd.Generate(player, pile, 4);
        await Play(New<ApexMagic>());
        Check(enemy.IsDead, "顶点魔法四四四消灭敌人");
        Pass("顶点魔法三牌堆恰好四张魔力时消灭敌人");
        return passed;
    }
}

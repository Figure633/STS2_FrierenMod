using System.Reflection;
using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;
using FrierenMod.Scripts.Powers;
using FrierenMod.Scripts.Rewards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.ValueProps;
using System.Text.Json;

static class CombatTests
{
    public static async Task<int> Run()
    {
        int passed = 0;
        void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        void Pass(string name) { passed++; Console.WriteLine("PASS " + name); }
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "FRIERENCHANT");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService());
        MegaCrit.Sts2.Core.Context.LocalContext.NetId = 1;
        var combat = new CombatState(runState: run);
        combat.AddPlayer(player);
        var enemy = combat.CreateCreature(ModelDb.Monster<MegaCrit.Sts2.Core.Models.Monsters.Axebot>().ToMutable(), CombatSide.Enemy, null);
        combat.AddCreature(enemy);
        CombatManager.Instance.SetUpCombat(combat);
        typeof(CombatManager).GetProperty(nameof(CombatManager.IsInProgress))!.SetValue(CombatManager.Instance, true);
        var deck = MagicDeckState.CreatePrototypeLoadout();
        MagicDeckStore.Write(player, deck);
        await ManaCmd.Generate(player, PileType.Draw, 3);
        var card = combat.CreateCard<RapidChant>(player);
        var play = new CardPlay { Card = card, Target = null, ResultPile = PileType.Discard,
            Resources = new ResourceInfo { EnergySpent = 0, EnergyValue = 0, StarsSpent = 0, StarValue = 0 }, IsAutoPlay = false, PlayIndex = 0, PlayCount = 1 };
        await ChantCmd.Resolve(new ThrowingPlayerChoiceContext(), play, ChantCmd.Options(player, 2).Single(x => x.Slot == ManaSlot.Draw));
        if (player.Creature.Block != 4 || ManaCmd.InPile(player, PileType.Draw).Count() != 1)
            throw new Exception("实际咏唱结算或指定牌堆扣费错误。");
        Pass("原生战斗命令：指定槽支付与双次格挡");
        var context = new ThrowingPlayerChoiceContext();
        async Task Reset()
        {
            foreach (var creature in combat.Creatures) {
                foreach (var power in creature.Powers.ToArray()) await PowerCmd.Remove(power);
                creature.LoseBlockInternal(creature.Block); creature.SetCurrentHpInternal(creature.MaxHp);
            }
            foreach (var pile in player.PlayerCombatState!.AllPiles)
                foreach (var c in pile.Cards.ToArray()) pile.RemoveInternal(c);
            player.PlayerCombatState.Energy = 3;
            CombatManager.Instance.History.Clear();
        }
        MagicDeckState CompleteDeck(params string[] ids)
        {
            var value = new MagicDeckState();
            for (int i = 0; i < 3; i++) { value.PrepareDraft(_ => 0); value.SelectInitial(value.Draft[0]); }
            for (int i = 0; i < ids.Length; i++) value.Equip(value.Add(ids[i]).InstanceId, (ManaSlot)i);
            MagicDeckStore.Write(player, value); return value;
        }
        int Power<T>(MegaCrit.Sts2.Core.Entities.Creatures.Creature? creature = null) where T : PowerModel => (creature ?? player.Creature).GetPower<T>()?.Amount ?? 0;
        async Task Play(CardModel c, MegaCrit.Sts2.Core.Entities.Creatures.Creature? target = null)
        {
            await CardPileCmd.Add(c, PileType.Hand);
            await c.OnPlayWrapper(context, target, false, play.Resources, skipCardPileVisuals: true);
        }
        await Reset();
        MagicDeckStore.Write(player, new MagicDeckState());
        var selector = new TestCardSelector();
        using var selection = CardSelectCmd.UseSelector(selector);
        for (int i = 0; i < 3; i++) selector.PrepareToSelect(new[] { i });
        await player.GetRelic<FrierenMod.Scripts.Relics.HolyEmblem>()!.AfterActEntered();
        var initialized = MagicDeckStore.Read(player);
        Check(initialized.SetupCompleted && initialized.Items.Count == 3 && initialized.Items.All(i => i.Slot >= 0), "初始三选一未完成装载");
        await MagicDeckCmd.ChooseStartingFactors(context, player);
        Check(initialized.ToJson() == MagicDeckStore.Read(player).ToJson(), "重复补发初始因子");
        Pass("原生选择：开局连续三次三选一，完成后不重复");

        initialized.Add("BluePrint"); MagicDeckStore.Write(player, initialized);
        var json = JsonSerializer.Serialize(player.ToSerializable(), JsonSerializationUtility.Options);
        var save = JsonSerializer.Deserialize<MegaCrit.Sts2.Core.Saves.Runs.SerializablePlayer>(json, JsonSerializationUtility.Options)!;
        var restored = Player.FromSerializable(save);
        Check(MagicDeckStore.Read(restored).ToJson() == initialized.ToJson(), "真实玩家存档丢失因子");
        Pass("BaseLib 真实玩家 JSON 存档恢复因子库存、槽位和初始进度");

        var reward = new FactorReward(player) { Seed = 92376 }; reward.Populate();
        var rewardSave = reward.ToSerializable();
        var recovered = FactorReward.Restore(rewardSave, player);
        Check(reward.Choices.SequenceEqual(recovered.Choices) && recovered.Choices.Distinct().Count() == 3, "因子奖励读档重掷或重复");
        selector.PrepareToSelect(new[] { 2 });
        Check(await MagicDeckCmd.ChooseReward(context, player, recovered.Choices), "奖励领取失败");
        Check(MagicDeckStore.Read(player).Items.Last().SpellId == recovered.Choices[2], "奖励未加入库存");
        Pass("奖励选项固定、领取加入库存");

        var expectedPowers = new Dictionary<string, (Type, int, bool)> {
            ["BuildUpBody"] = (typeof(StrengthPower), 1, false), ["HighSpeedMovement"] = (typeof(DexterityPower), 1, false),
            ["BePrepared"] = (typeof(VigorPower), 4, false), ["BarrierMagic"] = (typeof(BlurPower), 1, false),
            ["TransferToAlchemist"] = (typeof(PoisonPower), 2, true), ["MeleeMage"] = (typeof(StrikeUpPower), 2, false),
            ["MetallicizFlower"] = (typeof(MetallicizePower), 1, false), ["Sleeeep"] = (typeof(DrawCardsNextTurnPower), 1, false),
            ["Hypnosis"] = (typeof(WeakPower), 1, true), ["NatureBeauty"] = (typeof(EnergyNextTurnPower), 1, false),
            ["PinchAndScrape"] = (typeof(SynchroWithoutManaPower), 2, false), ["SpellBoost"] = (typeof(PhantasmalPower), 1, false),
            ["GreatMageComposure"] = (typeof(ChantWithoutManaTimesPower), 1, false), ["ImpeccableIntangible"] = (typeof(IntangiblePower), 1, false),
            ["Transcendence"] = (typeof(MantraPower), 1, false), ["GrossedOutLook"] = (typeof(StrengthPower), -3, true)
        };
        foreach (var factor in SpellCatalog.All.Where(f => f.Id != "BluePrint" && !f.Puzzle))
        {
            await Reset(); CompleteDeck(factor.Id);
            await ManaCmd.Generate(player, PileType.Draw, 10);
            var option = ChantCmd.Options(player, 1).Single(x => x.Slot == ManaSlot.Draw);
            await ChantCmd.Resolve(context, play, option);
            int remaining = ManaCmd.InPile(player, PileType.Draw).Count();
            Check(remaining == 10 - option.Cost - (factor.Id == "PeacefulDays" ? 1 : 0), "扣费错误: " + factor.Id);
            if (expectedPowers.TryGetValue(factor.Id, out var expected))
                Check((expected.Item3 ? enemy : player.Creature).Powers.Single(p => p.GetType() == expected.Item1).Amount == expected.Item2, "能力数值错误: " + factor.Id);
            if (factor.Id == "AdvancedDefensiveMagic") Check(player.Creature.Block == 2, "双次格挡错误");
            if (factor.Id == "BloodSpell") Check(player.Creature.CurrentHp == player.Creature.MaxHp - 3, "血魔法错误");
            if (factor.Id == "IceArrows") Check(enemy.CurrentHp == enemy.MaxHp - 4, "冰箭错误");
            if (factor.Id == "BigFire") Check(player.PlayerCombatState!.DrawPile.Cards.OfType<LittleFire>().Count() == 1, "小小火未生成");
            if (factor.Id == "TheLivingLegend") Check(player.PlayerCombatState!.DrawPile.Cards.OfType<ILegendaryCard>().Count() == 1, "传奇魔法未生成");
            if (factor.Id == "PassOnTheTorch") Check(ManaCmd.InPile(player, PileType.Hand).Count() == 1, "薪火槽位错误");
            if (factor.Id == "Strategizing") Check(player.PlayerCombatState!.Energy == 4, "能量错误");
            Pass("原生因子结算 " + factor.Id);
        }
        await Reset(); CompleteDeck("BluePrint", "BuildUpBody", "HighSpeedMovement");
        await ManaCmd.Generate(player, PileType.Draw, 2);
        await ChantCmd.Resolve(context, play, ChantCmd.Options(player, 2).Single(x => x.Slot == ManaSlot.Draw));
        Check(Power<StrengthPower>() == 2 && ManaCmd.Total(player) == 0, "蓝图未复制下一槽或错扣牌堆");
        Pass("蓝图复制因子但仍从自己的牌堆支付");

        await Reset(); CompleteDeck("Factor100", "Factor010", "Factor001");
        foreach (var slot in Enum.GetValues<ManaSlot>()) await ManaCmd.Generate(player, ManaCmd.Pile(slot), 8);
        foreach (var option in ChantCmd.Options(player, 1)) await ChantCmd.Resolve(context, play, option);
        Check(Power<PuzzlePower>() == 3, "因子序列无法完成");
        Pass("三个谜题因子在对应槽位按顺序累积进度");

        await Reset(); CompleteDeck("BuildUpBody", "HighSpeedMovement", "AdvancedDefensiveMagic");
        await PowerCmd.Apply<ConcentrationPower>(context, player.Creature, 2, player.Creature, null);
        Check(ChantCmd.Options(player, 2).Count == 3 && ChantCmd.Options(player, 2).All(o => o.Cost == 0), "专注减费不生效");
        await PowerCmd.Apply<WeakenedChantPower>(context, player.Creature, 1, enemy, null);
        Check(ChantCmd.Options(player, 2).Count == 0, "咏唱削弱未增加费用");
        await PowerCmd.Apply<ChantWithoutManaTimesPower>(context, player.Creature, 2, player.Creature, null);
        selector.PrepareToSelect(new[] { 0 }); await ChantCmd.Choose(context, play, 2);
        Check(Power<StrengthPower>() == 2 && Power<ChantWithoutManaTimesPower>() == 1, "免耗次数错误");
        foreach (var slot in Enum.GetValues<ManaSlot>()) await ManaCmd.Generate(player, ManaCmd.Pile(slot), 2);
        await ChantCmd.Final(context, play);
        Check(Power<ChantWithoutManaTimesPower>() == 0 && ManaCmd.Total(player) == 6 && Power<DexterityPower>() == 2, "终极咏唱重复扣免耗或资源");
        Pass("专注、削弱、普通免耗与终极咏唱一次性扣次数");

        await Reset(); CompleteDeck("BuildUpBody", "HighSpeedMovement", "AdvancedDefensiveMagic");
        await Play(combat.CreateCard<Imagination>(player));
        await ManaCmd.Generate(player, PileType.Draw, 3); Check(ManaCmd.Total(player) == 0, "禁产魔失效");
        // 使用原生 Add 放入现有魔力，模拟打出想象力前的资源。
        await CardPileCmd.Add(combat.CreateCard<Mana>(player), PileType.Draw);
        await CardPileCmd.Add(combat.CreateCard<Mana>(player), PileType.Hand);
        var paid = combat.CreateCard<FinalChant>(player);
        await CardPileCmd.Add(paid, PileType.Hand);
        Check(paid.EnergyCost.GetAmountToSpend() == 0, "想象力未替代能量费用");
        int energy = player.PlayerCombatState!.Energy;
        await paid.SpendResources();
        Check(ManaCmd.Total(player) == 0 && player.PlayerCombatState.Energy == energy, $"魔力代付扣费错误: 剩余魔力={ManaCmd.Total(player)}, 能量={player.PlayerCombatState.Energy}, 原能量={energy}, 代付费用={player.Creature.GetPower<ManaInsteadOfEnergyPower>()?.Cost(paid)}");
        Check(!combat.CreateCard<StrikeFrieren>(player).CanPlay(), "魔力不足仍能出牌");
        await Hook.AfterTurnEnd(combat, CombatSide.Player, [player.Creature]);
        Check(Power<ManaInsteadOfEnergyPower>() == 0 && Power<BanManaGainPower>() == 0 && Power<ChantWithoutManaPower>() == 0, "想象力回合结束未清除: " + string.Join(",", player.Creature.Powers.Select(p => p.GetType().Name)));
        Pass("想象力：禁生成、跨牌堆代付、资源不足拦截、回合清理");

        await Reset(); CompleteDeck("BuildUpBody");
        await ManaCmd.Generate(player, PileType.Draw, 3);
        var returnCard = combat.CreateCard<HalfTheSky>(player); await CardPileCmd.Add(returnCard, PileType.Discard);
        var reduced = combat.CreateCard<Famehameha>(player); await CardPileCmd.Add(reduced, PileType.Hand);
        await PowerCmd.Apply<FullAheadPower>(context, player.Creature, 1, player.Creature, null);
        selector.PrepareToSelect(new[] { 0 }); await Play(combat.CreateCard<RapidChant>(player));
        Check(returnCard.Pile?.Type == PileType.Hand && reduced.EnergyCost.GetAmountToSpend() == 3 && Power<DexterityPower>() == 1, "咏唱回调链错误");
        selector.PrepareToSelect(new[] { 0 }); await Play(combat.CreateCard<RapidChant>(player));
        Check(reduced.EnergyCost.GetAmountToSpend() == 2 && Power<DexterityPower>() == 1, "咏唱减费或每回合首次逻辑错误");
        Pass("原生出牌链：咏唱、弃牌回手、减费、每回合首次加成");

        await Reset(); CompleteDeck("BuildUpBody", "HighSpeedMovement", "AdvancedDefensiveMagic");
        Check(!combat.CreateCard<PreciseChant>(player).CanPlay() && !combat.CreateCard<FinalChant>(player).CanPlay(), "无有效因子时仍消耗能量空打");
        await ManaCmd.Generate(player, PileType.Hand, 3);
        selector.PrepareToSelect(new[] { 0 }); await Play(combat.CreateCard<LureTheEnemyInDeep>(player));
        Check(player.PlayerCombatState!.Hand.Cards.Any(c => c is LureTheEnemyInDeep), "手牌槽咏唱未回手");
        Pass("无可用槽位阻止空打；诱敌深入使用手牌槽后回手");

        await Reset(); CompleteDeck("BuildUpBody");
        var seal = combat.CreateCard<HellFireSummoning>(player); await CardPileCmd.Add(seal, PileType.Draw);
        await seal.BeforeCombatStart();
        Check(seal.Pile == null && Power<SealPower>() == 1, "封印未移出牌堆");
        await ManaCmd.Generate(player, PileType.Draw, 6);
        for (int i = 0; i < 5; i++) { selector.PrepareToSelect(new[] { 0 }); await Play(combat.CreateCard<RapidChant>(player)); }
        Check(seal.Pile?.Type == PileType.Hand && Power<SealPower>() == 0, "第五次咏唱未解除封印");
        Pass("封印战斗开始移出牌堆，第五张咏唱牌解封回手");

        await Reset(); CompleteDeck("BuildUpBody");
        await PowerCmd.Apply<SynchroWithoutManaPower>(context, player.Creature, 1, player.Creature, null);
        var manaCard = combat.CreateCard<Mana>(player); await Play(manaCard);
        Check(manaCard.Pile?.Type == PileType.Discard && Power<SynchroWithoutManaPower>() == 0, "节俭没有保留魔力");
        await Play(manaCard); Check(manaCard.Pile?.Type == PileType.Exhaust, "节俭次数用完后未恢复消耗");
        Pass("节俭在真实同调出牌中改为弃置，并正确扣次数");

        await Reset(); CompleteDeck("BuildUpBody");
        await Play(combat.CreateCard<FolkGrimoire>(player));
        Check(player.PlayerCombatState!.Hand.Cards.Count == 2 && player.PlayerCombatState.Hand.Cards.Any(c => c is IChantCard) && player.PlayerCombatState.Hand.Cards.Any(c => c is ILegendaryCard), "魔导书随机池不完整");
        await PowerCmd.Apply<MantraPower>(context, player.Creature, 10, player.Creature, null);
        Check(Power<DivinityPower>() == 1 && player.PlayerCombatState.Energy == 6, "真言未进入神格");
        Pass("魔导书生成咏唱与传奇魔法；十层真言进入神格");

        await Reset();
        var savedDeck = player.Deck.Cards.ToArray();
        try
        {
            CardModel AddLegend<T>() where T : CardModel, new()
            {
                var legend = ModelDb.Card<T>().ToMutable();
                legend.Owner = player; player.Deck.AddInternal(legend);
                return legend;
            }
            void Preview(FairyTaleEnd tale, decimal expected, bool targeting = false)
            {
                tale.UpdateDynamicVarPreview(CardPreviewMode.Normal, targeting ? enemy : null, tale.DynamicVars);
                // 原生预览保留小数，卡面与伤害结算在最终显示/扣血时取整。
                Check((int)tale.DynamicVars.Damage.PreviewValue == expected, $"童话伤害预览错误：{tale.DynamicVars.Damage.PreviewValue}/{expected}");
                foreach (string language in new[] { "eng", "zhs" })
                {
                    LocManager.Instance.SetLanguage(language);
                    string text = System.Text.RegularExpressions.Regex.Replace(tale.GetDescriptionForPile(PileType.Hand), @"\[[^\]]+\]", "");
                    Check(text.Contains(language == "zhs" ? $"造成 {expected} 点伤害" : $"Deal {expected} damage"), "童话卡面未显示实际伤害：" + text);
                }
            }

            var tale = combat.CreateCard<FairyTaleEnd>(player);
            await CardPileCmd.Add(tale, PileType.Hand);
            Preview(tale, 6);
            AddLegend<FairyTaleEnd>();
            Preview(tale, 12);
            var mold = AddLegend<MoldSpell>();
            var duplicate = AddLegend<MoldSpell>(); duplicate.UpgradeInternal();
            Preview(tale, 24); Preview(tale, 24);
            Check(tale.DynamicVars.Damage.BaseValue == 6, "预览改写了童话的原始伤害");
            player.Deck.RemoveInternal(mold); Preview(tale, 24);
            player.Deck.RemoveInternal(duplicate); Preview(tale, 12);
            AddLegend<MoldSpell>(); Preview(tale, 24);
            Pass("童话卡面按永久牌组种类计算，重复/升级同名牌及临时生成不重复翻倍");

            var upgradedTale = combat.CreateCard<FairyTaleEnd>(player);
            upgradedTale.UpgradeInternal();
            await CardPileCmd.Add(upgradedTale, PileType.Hand);
            Preview(upgradedTale, 32);
            Check(tale.DynamicVars.Damage.BaseValue == 6 && upgradedTale.DynamicVars.Damage.BaseValue == 8, "升级预览污染了原卡");
            Pass("童话升级正确显示倍率后的伤害，原卡与副本互不污染");

            await PowerCmd.Apply<StrengthPower>(context, player.Creature, 3, player.Creature, null);
            await PowerCmd.Apply<WeakPower>(context, player.Creature, 1, enemy, null);
            await PowerCmd.Apply<VulnerablePower>(context, enemy, 1, player.Creature, null);
            Preview(tale, 20); Preview(tale, 30, targeting: true);
            int previousHp = enemy.CurrentHp;
            await tale.OnPlayWrapper(context, enemy, false, play.Resources, skipCardPileVisuals: true);
            Check(previousHp - enemy.CurrentHp == 30, "童话实际命中与力量/虚弱/易伤预览不一致");
            Pass("童话手牌和指向敌人预览反映力量/虚弱/易伤，并与实际命中一致");

            var deckPreview = (FairyTaleEnd)ModelDb.Card<FairyTaleEnd>().ToMutable();
            deckPreview.Owner = player;
            Preview(deckPreview, 24);
            foreach (var creature in combat.Creatures)
                foreach (var power in creature.Powers.ToArray()) await PowerCmd.Remove(power);
            typeof(CombatManager).GetProperty(nameof(CombatManager.IsInProgress))!.SetValue(CombatManager.Instance, false);
            Preview(deckPreview, 24);
            typeof(CombatManager).GetProperty(nameof(CombatManager.IsInProgress))!.SetValue(CombatManager.Instance, true);
            Pass("童话牌组/奖励预览在战斗外仍显示牌组倍率，不混入战斗临时增减益");

            deckPreview.DynamicVars.Damage.BaseValue = 50000;
            Preview(deckPreview, 99999);
            Check(deckPreview.DynamicVars.Damage.BaseValue == 50000, "童话伤害封顶反写了基础值");
            Pass("童话伤害预览保留现有封顶且不改写原始数值");
        }
        finally
        {
            typeof(CombatManager).GetProperty(nameof(CombatManager.IsInProgress))!.SetValue(CombatManager.Instance, true);
            player.Deck.Clear(silent: true);
            foreach (var savedCard in savedDeck) player.Deck.AddInternal(savedCard, silent: true);
            await Reset();
        }

        foreach (var language in new[] { "eng", "zhs" }) {
            LocManager.Instance.SetLanguage(language);
            foreach (var type in typeof(Mana).Assembly.GetTypes().Where(t => !t.IsAbstract && typeof(CardModel).IsAssignableFrom(t))) {
                var model = combat.CreateCard(ModelDb.GetById<CardModel>(ModelDb.GetId(type)), player);
                _ = model.GetDescriptionForPile(PileType.None);
                if (model.MaxUpgradeLevel > 0) { model.UpgradeInternal(); _ = model.GetDescriptionForPile(PileType.None); }
                Check(Godot.ResourceLoader.Exists(model.PortraitPath), "缺少卡面 " + type.Name);
            }
            foreach (var factor in SpellCatalog.All) {
                var model = FactorCard.Create(player, factor.Id, ManaSlot.Hand, new ChantOption(ManaSlot.Hand, factor.Id, 1, 2, 3));
                _ = model.GetDescriptionForPile(PileType.None);
            }
        }
        Pass("全部卡牌及因子中英说明在 Godot 中格式化，升级文本和卡面完整");
        return passed;
    }
}

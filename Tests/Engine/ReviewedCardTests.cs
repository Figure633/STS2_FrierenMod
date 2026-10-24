using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;

public static class ReviewedCardTests
{
    public static async Task<int> Run(Node root)
    {
        Console.WriteLine("REVIEWED: 开始核定卡牌测试");
        int passed = 0;
        void Check(bool condition, string name) { if (!condition) throw new Exception("核定卡牌：" + name); }
        void Pass(string name) { passed++; Console.WriteLine("PASS 核定卡牌：" + name); }
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "FRIEREN-REVIEWED");
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
        T New<T>(bool upgrade = false) where T : CardModel
        {
            var card = combat.CreateCard<T>(player);
            if (upgrade) { card.UpgradeInternal(); card.FinalizeUpgradeInternal(); }
            return card;
        }
        CardPlay MakePlay(CardModel c) => new() { Card = c, Target = null, IsAutoPlay = false, ResultPile = PileType.Discard,
            Resources = new ResourceInfo { EnergySpent = 0, EnergyValue = 0, StarsSpent = 0, StarValue = 0 }, PlayCount = 1, PlayIndex = 0 };
        async Task Play(CardModel c, Creature? target = null)
        {
            await CardPileCmd.Add(c, PileType.Hand);
            await c.OnPlayWrapper(context, target, false, MakePlay(c).Resources, skipCardPileVisuals: true);
        }
        void Equip(params string[] factors)
        {
            var deck = new MagicDeckState();
            for (int i = 0; i < 3; i++) { deck.PrepareDraft(_ => 0); deck.SelectInitial(deck.Draft[0]); }
            for (int i = 0; i < factors.Length; i++) deck.Equip(deck.Add(factors[i]).InstanceId, (ManaSlot)i);
            MagicDeckStore.Write(player, deck);
        }
        async Task Chant(string factor, int x)
        {
            Equip(factor);
            await ManaCmd.Generate(player, PileType.Draw, 12);
            await ChantCmd.Resolve(context, MakePlay(New<RapidChant>()), ChantCmd.Options(player, x).Single(o => o.Slot == ManaSlot.Draw));
        }

        await Reset();
        await PowerCmd.Apply<ConcentrationPower>(context, player.Creature, 10, player.Creature, null);
        await Chant("Hypnosis", 3);
        Check(enemy.GetPowerAmount<WeakPower>() == 3 && enemy.GetPowerAmount<VulnerablePower>() == 3, "催眠应按 X 而非折扣后的消耗量施加层数");
        Check(ManaCmd.InPile(player, PileType.Draw).Count() == 12, "专注免耗");
        Pass("催眠层数独立于费用与专注折扣");

        await Reset();
        await PowerCmd.Apply<DexterityPower>(context, player.Creature, 3, player.Creature, null);
        await Chant("BarrierMagic", 2);
        Check(player.Creature.Block == 5 && player.Creature.GetPowerAmount<BlurPower>() == 2, "结界魔法正常接受敏捷并给予残影");
        Pass("结界魔法的格挡与残影");

        await Reset(); Equip("BuildUpBody", "HighSpeedMovement", "AdvancedDefensiveMagic");
        foreach (var slot in Enum.GetValues<ManaSlot>()) await ManaCmd.Generate(player, ManaCmd.Pile(slot), 2);
        var held = New<Confrontation>(); var discarded = New<Confrontation>();
        await CardPileCmd.Add(held, PileType.Hand); await CardPileCmd.Add(discarded, PileType.Discard);
        await ChantCmd.Final(context, MakePlay(New<FinalChant>()));
        Check(held.DynamicVars.Block.BaseValue == 15 && discarded.DynamicVars.Block.BaseValue == 6, "一次终极咏唱的三个成功槽位应分别触发手牌对峙");
        var failed = new ChantOption(ManaSlot.Draw, "BuildUpBody", 1, 99, 1);
        await ChantCmd.Resolve(context, MakePlay(New<RapidChant>()), failed);
        Check(held.DynamicVars.Block.BaseValue == 15, "支付失败不能触发对峙");
        Pass("对峙按成功咏唱触发，支持终极咏唱，排除失败与弃牌堆");

        await Reset(); var mana = New<Mana>();
        await Play(mana);
        Check(mana.Pile?.Type == PileType.Exhaust, "普通魔力同调仍然消耗");
        var dense = New<HighDensityMana>();
        await Play(dense);
        Check(dense.Pile?.Type == PileType.Exhaust && player.PlayerCombatState!.Energy == 4, "高密度魔力消耗仍给能量");
        await PowerCmd.Apply<SynchroWithoutManaPower>(context, player.Creature, 1, player.Creature, null);
        var frugal = New<HighDensityMana>(); await Play(frugal);
        Check(frugal.Pile?.Type == PileType.Hand && player.PlayerCombatState!.Energy == 5 && !player.Creature.HasPower<SynchroWithoutManaPower>(), "节俭回手时同样获得能量");
        foreach (string lang in new[] { "zhs", "eng" })
        {
            LocManager.Instance.SetLanguage(lang);
            foreach (var c in new CardModel[] { mana, dense }) {
                string exhaust = lang == "zhs" ? "[gold]消耗[/gold]。" : "[gold]Exhaust[/gold].";
                Check(!c.GetDescriptionForPile(PileType.Hand).Split('\n').Contains(exhaust), "同调卡面不重复列消耗关键词");
            }
        }
        Pass("同调简写保留原生消耗、能量与节俭流程");

        await Reset(); var fire = New<LittleFire>(); var spentFire = New<LittleFire>();
        await CardPileCmd.Add(spentFire, PileType.Exhaust); await Play(fire, enemy);
        Check(fire.DynamicVars.Damage.BaseValue == 5 && spentFire.DynamicVars.Damage.BaseValue == 5 && New<LittleFire>().DynamicVars.Damage.BaseValue == 3,
            "只强化战斗中现有火花，包括消耗堆，不强化后来生成的牌");
        Check(fire.EnergyCost.GetAmountToSpend() == 1, "回火加费");
        Pass("小小火花强化范围与回火");

        await Reset(); await ManaCmd.Generate(player, PileType.Hand, 2); await Play(New<PancakeSpell>());
        Check(ManaCmd.InPile(player, PileType.Hand).Count() == 2, "空抽牌堆不应收取交换魔力");
        var chosen = New<StrikeFrieren>(); await CardPileCmd.Add(chosen, PileType.Draw); await Play(New<PancakeSpell>());
        Check(chosen.Pile?.Type == PileType.Hand && ManaCmd.InPile(player, PileType.Draw).Count() == 2, "选原有抽牌后交换魔力");
        Pass("松饼交换原有抽牌，空牌堆不收取魔力");

        await Reset(); await Play(New<FrierenSuitcase>());
        var suitcase = player.Creature.GetPower<FrierenSuitcasePower>()!;
        Check(suitcase.ModifyMaxHandSize(player, 10) == 13 && suitcase.ModifyMaxHandSize(player, 20) == 13, "手牌上限固定为13");
        Pass("手提箱将手牌上限改为13");

        await Reset();
        var savedDeck = player.Deck.Cards.ToArray();
        foreach (var c in savedDeck) player.Deck.RemoveInternal(c);
        var legend = ModelDb.Card<FairyTaleEnd>().ToMutable(); legend.Owner = player; player.Deck.AddInternal(legend);
        var tale = New<FairyTaleEnd>(); await CardPileCmd.Add(tale, PileType.Hand);
        var hellfire = New<HellFireSummoning>(); await CardPileCmd.Add(hellfire, PileType.Hand);
        for (int i = 0; i < 2; i++) CombatManager.Instance.History.CardPlayStarted(combat, MakePlay(New<RapidChant>()));
        foreach (string lang in new[] { "zhs", "eng" })
        {
            LocManager.Instance.SetLanguage(lang);
            tale.UpdateDynamicVarPreview(CardPreviewMode.Normal, null, tale.DynamicVars);
            Check(tale.DynamicVars.Damage.PreviewValue == 12 && tale.DynamicVars["BaseDamage"].BaseValue == 6, "童话保留基础6、实际12");
            string text = tale.GetDescriptionForPile(PileType.Hand);
            Check(text.Contains("12") && text.Contains("6") && !text.Contains("BaseDamage"), "童话括号实际伤害");
            hellfire.UpdateDynamicVarPreview(CardPreviewMode.Normal, null, hellfire.DynamicVars);
            text = hellfire.GetDescriptionForPile(PileType.Hand);
            Check(text.Contains(lang == "zhs" ? "伤害 4 次" : "damage 4 times") && !text.Contains("{Hits}"), "业火实际攻击次数");
        }
        await PowerCmd.Apply<StrengthPower>(context, player.Creature, 2, player.Creature, null);
        tale.UpdateDynamicVarPreview(CardPreviewMode.Normal, null, tale.DynamicVars);
        hellfire.UpdateDynamicVarPreview(CardPreviewMode.Normal, null, hellfire.DynamicVars);
        Check(tale.GetDescriptionForPile(PileType.Hand).Contains("[green]14[/green]") && hellfire.GetDescriptionForPile(PileType.Hand).Contains("[green]10[/green]"), "力量仅影响实际伤害预览并变绿");
        await PowerCmd.Remove(player.Creature.GetPower<StrengthPower>()!);
        await PowerCmd.Apply<WeakPower>(context, player.Creature, 1, enemy, null);
        tale.UpdateDynamicVarPreview(CardPreviewMode.Normal, null, tale.DynamicVars);
        hellfire.UpdateDynamicVarPreview(CardPreviewMode.Normal, null, hellfire.DynamicVars);
        Check(tale.GetDescriptionForPile(PileType.Hand).Contains("[red]9[/red]") && hellfire.GetDescriptionForPile(PileType.Hand).Contains("[red]6[/red]"), "削弱伤害预览变红");
        await PowerCmd.Remove(player.Creature.GetPower<WeakPower>()!);
        tale.UpgradeInternal(); hellfire.UpgradeInternal();
        tale.UpdateDynamicVarPreview(CardPreviewMode.Normal, null, tale.DynamicVars);
        Check(tale.DynamicVars.Damage.PreviewValue == 16 && tale.DynamicVars["BaseDamage"].BaseValue == 8 && hellfire.DynamicVars["BaseDamage"].BaseValue == 10, "升级数值与实际预览同步");
        foreach (var c in player.Deck.Cards.ToArray()) player.Deck.RemoveInternal(c);
        foreach (var c in savedDeck) player.Deck.AddInternal(c, silent: true);
        Pass("童话与业火的中英文实际预览、力量、虚弱、升级");

        LocManager.Instance.SetLanguage("zhs");
        foreach (var card in new CardModel[] { New<AirSlash>(), New<ClearMind>(), New<PancakeSpell>(), New<BabySleeping>(), New<Imagination>(), New<LightningMagic>(), New<LittleFire>(), New<Confrontation>() })
        {
            string text = card.GetDescriptionForPile(PileType.Hand);
            Check(!text.Contains('{') && !text.Contains("核实") && !text.Contains("未实装"), "卡面模板与编辑备注 " + card.Id);
        }
        Pass("已改卡面正常格式化，无编辑备注或裸模板");
        return passed;
    }
}

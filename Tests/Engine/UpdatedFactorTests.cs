using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.TestSupport;

public static class UpdatedFactorTests
{
    public static async Task<int> Run()
    {
        int checks = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception(name); checks++; GD.Print("PASS 因子更新：" + name); }
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "FACTORS-074");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = 1;
        var combat = new CombatState(runState: run); combat.AddPlayer(player);
        var enemy = combat.CreateCreature(ModelDb.Monster<Axebot>().ToMutable(), CombatSide.Enemy, null); combat.AddCreature(enemy);
        CombatManager.Instance.SetUpCombat(combat);
        typeof(CombatManager).GetProperty(nameof(CombatManager.IsInProgress))!.SetValue(CombatManager.Instance, true);
        var context = new ThrowingPlayerChoiceContext(); var selector = new TestCardSelector(); using var selection = CardSelectCmd.UseSelector(selector);
        void Equip(params string[] ids) {
            var deck = new MagicDeckState();
            for (int i = 0; i < 3; i++) { deck.PrepareDraft(_ => 0); deck.SelectInitial(deck.Draft[0]); }
            for (int i = 0; i < ids.Length; i++) deck.Equip(deck.Add(ids[i]).InstanceId, (ManaSlot)i);
            MagicDeckStore.Write(player, deck);
        }
        CardPlay PlayOf<T>() where T : CardModel => new() { Card = combat.CreateCard<T>(player), Target = null, ResultPile = PileType.Discard,
            Resources = new ResourceInfo { EnergySpent = 0, EnergyValue = 0, StarsSpent = 0, StarValue = 0 }, IsAutoPlay = false, PlayIndex = 0, PlayCount = 1 };
        async Task Reset() {
            foreach (var creature in combat.Creatures) { foreach (var power in creature.Powers.ToArray()) await PowerCmd.Remove(power); creature.SetCurrentHpInternal(creature.MaxHp); }
            foreach (var pile in player.PlayerCombatState!.AllPiles) foreach (var card in pile.Cards.ToArray()) pile.RemoveInternal(card);
            CombatManager.Instance.History.Clear(); player.PlayerCombatState.Energy = 3;
        }
        foreach (var slot in Enum.GetValues<ManaSlot>()) {
            await Reset(); Equip("BloodSpell", "BloodSpell", "BloodSpell");
            await ManaCmd.Generate(player, ManaCmd.Pile(slot), 1);
            selector.PrepareToSelect([(int)slot]);
            await ChantCmd.Choose(context, PlayOf<RapidChant>(), 2);
            Check(player.Creature.CurrentHp == player.Creature.MaxHp - 2 && ManaCmd.InPile(player, ManaCmd.Pile(slot)).Count() == 3,
                "普通驭血不消耗原有魔力，失去指定生命并在对应牌堆生成：" + slot);
        }
        await Reset(); Equip("BloodSpell", "BloodSpell", "BloodSpell");
        foreach (var slot in Enum.GetValues<ManaSlot>()) await ManaCmd.Generate(player, ManaCmd.Pile(slot), 5);
        await PowerCmd.Apply<ChantWithoutManaTimesPower>(context, player.Creature, 2, player.Creature, null);
        await PowerCmd.Apply<ConcentrationPower>(context, player.Creature, 10, player.Creature, null);
        await PowerCmd.Apply<WeakenedChantPower>(context, player.Creature, 10, player.Creature, null);
        Check(ChantCmd.Choices(player, null).Count == 3 && ChantCmd.Choices(player, null).All(c => !c.CanSelect), "自动咏唱仍显示驭血槽位但禁止选择");
        var precise = await ChantCmd.Choose(context, PlayOf<PreciseChant>(), null);
        await ChantCmd.Final(context, PlayOf<FinalChant>());
        Check(precise == null && player.Creature.CurrentHp == player.Creature.MaxHp && ManaCmd.Total(player) == 15 && player.Creature.GetPowerAmount<ChantWithoutManaTimesPower>() == 2,
            "精准和终极跳过全部驭血，不扣血、不增减魔力、不扣免耗次数");
        selector.PrepareToSelect([0]); await ChantCmd.Choose(context, PlayOf<RapidChant>(), 2);
        Check(player.Creature.CurrentHp == player.Creature.MaxHp - 2 && ManaCmd.InPile(player, PileType.Draw).Count() == 7,
            "专注、削弱与免耗咏唱均不修改驭血生命代价");
        await Reset(); Equip("BloodSpell", "BuildUpBody", "HighSpeedMovement");
        foreach (var slot in Enum.GetValues<ManaSlot>()) await ManaCmd.Generate(player, ManaCmd.Pile(slot), 2);
        await ChantCmd.Final(context, PlayOf<FinalChant>());
        Check(player.Creature.CurrentHp == player.Creature.MaxHp && ManaCmd.InPile(player, PileType.Draw).Count() == 2 &&
            player.Creature.GetPowerAmount<StrengthPower>() == 2 && player.Creature.GetPowerAmount<DexterityPower>() == 2,
            "混合终极咏唱跳过驭血，其他因子正常结算");
        foreach (string lang in new[] { "zhs", "eng" }) {
            LocManager.Instance.SetLanguage(lang);
            var preview = FactorCard.Create(player, "BloodSpell", ManaSlot.Draw, ChantRules.Preview(ManaSlot.Draw, SpellCatalog.Get("BloodSpell").Rules, 3));
            string text = preview.GetDescriptionForPile(PileType.None);
            Check(text.Contains("[red]3[/red]") && !text.Contains(lang == "zhs" ? "消耗" : "Consume", StringComparison.OrdinalIgnoreCase), lang + "驭血预览生命标红且没有魔力支付前缀");
        }
        await Reset(); Equip("GreatMageComposure", "BuildUpBody", "HighSpeedMovement");
        await ManaCmd.Generate(player, PileType.Draw, 8);
        await ChantCmd.Resolve(context, PlayOf<RapidChant>(), ChantCmd.Options(player, 2).Single(c => c.Slot == ManaSlot.Draw));
        var free = player.Creature.GetPower<ChantWithoutManaTimesPower>()!;
        await free.AfterSideTurnEnd(context, CombatSide.Player, [player.Creature]);
        await free.AfterSideTurnStart(CombatSide.Player, [player.Creature], combat);
        Check(player.Creature.GetPowerAmount<ChantWithoutManaTimesPower>() == 2, "大魔法师的余裕由实际咏唱获得，未用次数跨回合保留");
        selector.PrepareToSelect([1]); await ChantCmd.Choose(context, PlayOf<RapidChant>(), 2);
        Check(player.Creature.GetPowerAmount<ChantWithoutManaTimesPower>() == 1 && player.Creature.GetPowerAmount<StrengthPower>() == 2, "跨回合免耗允许零魔力并扣除一次");
        Equip("BuildUpBody", "HighSpeedMovement", "AdvancedDefensiveMagic");
        foreach (var slot in Enum.GetValues<ManaSlot>()) await ManaCmd.Generate(player, ManaCmd.Pile(slot), 2);
        await ChantCmd.Final(context, PlayOf<FinalChant>());
        Check(!player.Creature.HasPower<ChantWithoutManaTimesPower>() && ManaCmd.Total(player) == 6, "终极咏唱三个槽位只扣最后一次免耗，保留魔力");
        await Reset(); Equip("PinchAndScrape", "BuildUpBody", "HighSpeedMovement"); await ManaCmd.Generate(player, PileType.Draw, 1);
        await ChantCmd.Resolve(context, PlayOf<RapidChant>(), ChantCmd.Options(player, 1).Single(c => c.Slot == ManaSlot.Draw));
        Check(player.Creature.GetPowerAmount<SynchroWithoutManaPower>() == 2, "省吃俭用按2X获得层数");
        async Task PlayMana(CardModel card) {
            await CardPileCmd.Add(card, PileType.Hand);
            await card.OnPlayWrapper(context, null, false, new ResourceInfo { EnergySpent = 0, EnergyValue = 0, StarsSpent = 0, StarValue = 0 }, skipCardPileVisuals: true);
        }
        var mana = combat.CreateCard<Mana>(player); await PlayMana(mana);
        Check(mana.Pile?.Type == PileType.Hand && player.Creature.GetPowerAmount<SynchroWithoutManaPower>() == 1, "普通魔力同调后回手并扣一层");
        var dense = combat.CreateCard<HighDensityMana>(player); await PlayMana(dense);
        Check(dense.Pile?.Type == PileType.Hand && !player.Creature.HasPower<SynchroWithoutManaPower>() && player.PlayerCombatState!.Energy == 3,
            "最后一层高密度魔力仍回手，不触发消耗能量");
        await PlayMana(dense);
        Check(dense.Pile?.Type == PileType.Exhaust && player.PlayerCombatState!.Energy == 4, "层数用完恢复原生消耗并触发能量");
        LocManager.Instance.SetLanguage("eng"); RunManager.Instance.CleanUp(); return checks;
    }
}

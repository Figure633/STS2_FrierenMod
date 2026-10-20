using System.Text.Json;
using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Potions;
using FrierenMod.Scripts.Relics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.ValueProps;
using ConcentrationPower = FrierenMod.Scripts.Powers.ConcentrationPower;

public static class RelicsPotionsTests
{
    public static async Task<int> Run()
    {
        int count = 0;
        void Check(bool value, string name) { if (!value) throw new Exception(name); count++; GD.Print("PASS 遗物药水：" + name); }
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var other = Player.CreateForNewRun<Frieren>(UnlockState.all, 2);
        var run = RunState.CreateForTest([player, other], seed: "FRIEREN-RELICS-POTIONS");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = 1;
        var encounter = ModelDb.Encounter<MegaCrit.Sts2.Core.Models.Encounters.TurretOperatorWeak>().ToMutable();
        var combat = new CombatState(encounter, run); combat.AddPlayer(player); combat.AddPlayer(other);
        var enemy = combat.CreateCreature(ModelDb.Monster<MegaCrit.Sts2.Core.Models.Monsters.Axebot>().ToMutable(), CombatSide.Enemy, null);
        combat.AddCreature(enemy); enemy.SetMaxHpInternal(9999); enemy.SetCurrentHpInternal(9999);
        CombatManager.Instance.SetUpCombat(combat);
        AccessTools.Property(typeof(CombatManager), nameof(CombatManager.IsInProgress)).SetValue(CombatManager.Instance, true);
        var context = new BlockingPlayerChoiceContext();
        var selector = new TestCardSelector(); using var selection = CardSelectCmd.UseSelector(selector);
        T Card<T>() where T : CardModel => combat.CreateCard<T>(player);
        CardPlay PlayInfo(CardModel card) => new() { Card = card, Target = null, ResultPile = PileType.Discard,
            Resources = new ResourceInfo { EnergySpent = 0, EnergyValue = 0, StarsSpent = 0, StarValue = 0 },
            PlayCount = 1, PlayIndex = 0, IsAutoPlay = false };
        async Task Play(CardModel card) {
            await CardPileCmd.Add(card, PileType.Hand);
            await card.OnPlayWrapper(context, null, false, PlayInfo(card).Resources, skipCardPileVisuals: true);
        }
        async Task Reset() {
            selector.Cleanup();
            foreach (var relic in player.Relics.Where(r => r is not HolyEmblem).ToArray()) await RelicCmd.Remove(relic);
            foreach (var creature in combat.Creatures) {
                foreach (var power in creature.Powers.ToArray()) await PowerCmd.Remove(power);
                creature.LoseBlockInternal(creature.Block); creature.SetCurrentHpInternal(creature.MaxHp);
            }
            foreach (var pile in player.PlayerCombatState!.AllPiles)
                foreach (var card in pile.Cards.ToArray()) pile.RemoveInternal(card);
            foreach (var potion in player.Potions.ToArray()) await PotionCmd.Discard(potion);
            player.PlayerCombatState.Energy = 5; combat.RoundNumber = 1;
            AccessTools.Property(typeof(PlayerCombatState), nameof(PlayerCombatState.TurnNumber)).SetValue(player.PlayerCombatState, 1);
            CombatManager.Instance.History.Clear();
            MagicDeckStore.Write(player, MagicDeckState.CreatePrototypeLoadout());
        }
        var relics = ModelDb.RelicPool<FrierenRelicPool>().AllRelics.ToArray();
        var potions = ModelDb.PotionPool<FrierenPotionPool>().AllPotions.ToArray();
        Check(relics.Length == 19 && potions.Length == 3, "19件有效遗物和3种药水注册完整，无空壳StarkRelic");
        foreach (var relic in relics) {
            Check(relic.Icon.GetWidth() > 0 && relic.IconOutline.GetWidth() > 0 && ResourceLoader.Exists(relic is HolyEmblem ? relic.PackedIconPath :
                $"res://FrierenMod/images/relics/{relic.GetType().Name}.png"), "遗物图标及轮廓可加载：" + relic.GetType().Name);
        }
        foreach (var potion in potions) Check(potion.Image.GetWidth() > 0 && potion.Outline?.GetWidth() > 0,
            "药水图片和轮廓可加载：" + potion.GetType().Name);
        foreach (string language in new[] { "zhs", "eng" }) {
            LocManager.Instance.SetLanguage(language);
            Check(relics.All(r => !r.Title.GetFormattedText().Contains("FRIERENMOD-") && !string.IsNullOrWhiteSpace(r.DynamicDescription.GetFormattedText())), "遗物双语文本完整：" + language);
            Check(potions.All(p => !p.Title.GetFormattedText().Contains("FRIERENMOD-") && !string.IsNullOrWhiteSpace(p.DynamicDescription.GetFormattedText())), "药水双语文本完整：" + language);
            foreach (var type in new[] { typeof(Himmel), typeof(Repent), typeof(LongevityPain), typeof(Fatalism) }) {
                var card = ModelDb.GetById<CardModel>(ModelDb.GetId(type));
                Check(card.ShouldShowInCardLibrary && !string.IsNullOrWhiteSpace(card.GetDescriptionForPile(PileType.None)), "衍生牌文本及百科可见：" + type.Name + "/" + language);
            }
        }
        Check(new[] { typeof(ComfortableBed), typeof(IcicleCherryBlossom), typeof(HimmelGravestone), typeof(ScaleOfObedience) }
            .All(t => ModelDb.GetById<RelicModel>(ModelDb.GetId(t)).Rarity == RelicRarity.Rare), "四件原Boss遗物登记稀有");
        Check(ModelDb.Relic<MimicHead>().Rarity == RelicRarity.Event && ModelDb.Relic<FlammeGrimoire>().Rarity == RelicRarity.Event &&
            ModelDb.Relic<HairAccessory>().Rarity == RelicRarity.Starter && ModelDb.Relic<HaitaaWand>().Rarity == RelicRarity.Starter,
            "事件遗物和未来角色初始遗物保留获取限制");
        var grabBag = new RelicGrabBag(); grabBag.Populate(player, new MegaCrit.Sts2.Core.Random.Rng(60));
        Check(relics.All(r => grabBag.Contains(r) == (r.Rarity is RelicRarity.Common or RelicRarity.Uncommon or RelicRarity.Rare or RelicRarity.Shop)),
            "原生遗物奖励袋收录普通稀有及商店遗物，排除事件与初始遗物");

        await Reset(); var ear = await RelicCmd.Obtain<EarAccessory>(player);
        await ear.BeforeHandDraw(player, context, combat);
        Check(ManaCmd.InPile(player, PileType.Hand).Count() == 2 && !ManaCmd.InPile(other, PileType.Hand).Any(), "耳饰开局生成两张魔力，玩家隔离");
        await ear.BeforeHandDraw(other, context, combat);
        Check(ManaCmd.InPile(player, PileType.Hand).Count() == 2, "耳饰不响应其他玩家回合");

        await Reset(); var fake = await RelicCmd.Obtain<FakeFlammeGrimoire>(player);
        Check(Card<HotTeaSpell>().CanPlay() && Card<ContinualChant>().CanPlay(), "伪著作第一回合解除传说和连环咏唱限制");
        player.PlayerCombatState!.IncrementTurnNumber();
        Check(!Card<HotTeaSpell>().CanPlay(), "伪著作第二回合恢复咏唱前置条件");
        await RelicCmd.Obtain<FlammeGrimoire>(player);
        Check(player.GetRelic<FakeFlammeGrimoire>() == null && Card<HotTeaSpell>().CanPlay(), "真著作替换伪著作且解除后续回合限制");

        await Reset(); var hair = await RelicCmd.Obtain<HairAccessory>(player);
        var mana = Card<Mana>(); await CardPileCmd.Add(mana, PileType.Hand); await CardCmd.Exhaust(context, mana);
        Check(player.Creature.GetPowerAmount<ConcentrationPower>() == 1, "菲伦发饰实际响应魔力消耗");
        var nonMana = Card<DefendFrieren>(); await CardPileCmd.Add(nonMana, PileType.Hand); await CardCmd.Exhaust(context, nonMana);
        Check(player.Creature.GetPowerAmount<ConcentrationPower>() == 1, "非魔力消耗不增加专注");

        await Reset(); var staff = await RelicCmd.Obtain<UbelStaff>(player);
        for (int i = 0; i < 5; i++) await staff.BeforeCardPlayed(PlayInfo(Card<RapidChant>()));
        var savedStaff = JsonSerializer.Serialize(staff.ToSerializable(), JsonSerializationUtility.Options);
        var loadedStaff = (UbelStaff)RelicModel.FromSerializable(JsonSerializer.Deserialize<SerializableRelic>(savedStaff, JsonSerializationUtility.Options)!);
        Check(staff.Counter == 5 && loadedStaff.Counter == 5 && staff.Status == RelicStatus.Active && loadedStaff.Status == RelicStatus.Active,
            "法杖计数与待激活高亮正确存读档");
        int energy = player.PlayerCombatState!.Energy;
        await staff.BeforeCardPlayed(PlayInfo(Card<RapidChant>()));
        Check(staff.Counter == 0 && player.PlayerCombatState.Energy == energy + 1, "第六张咏唱牌回能并归零");

        await Reset(); var oldHair = await RelicCmd.Obtain<OldHairAccessory>(player); await oldHair.BeforeCombatStart();
        await CardPileCmd.Add(Card<DefendFrieren>(), PileType.Draw);
        for (int i = 0; i < 4; i++) await oldHair.AfterCardPlayed(context, PlayInfo(Card<Mana>()));
        Check(oldHair.Counter == 0 && player.PlayerCombatState!.Hand.Cards.Count == 1, "旧发饰四次同调额外抽牌");

        await Reset(); var statue = await RelicCmd.Obtain<StatueOfHimmel>(player); await statue.BeforeCombatStart();
        await statue.AfterPlayerTurnStart(context, player); int artifact = player.Creature.GetPowerAmount<ArtifactPower>();
        await statue.AfterPlayerTurnStart(context, player);
        Check(artifact >= 1 && player.Creature.GetPowerAmount<ArtifactPower>() == artifact, "雕像首回合授予人工制品，已有时不叠加");
        await PowerCmd.Remove(player.Creature.GetPower<ArtifactPower>()!); await statue.AfterPlayerTurnStart(context, player);
        Check(player.Creature.GetPowerAmount<ArtifactPower>() >= 1, "第三回合缺失人工制品时补充");
        await PowerCmd.Remove(player.Creature.GetPower<ArtifactPower>()!); await statue.AfterPlayerTurnStart(context, player);
        Check(!player.Creature.HasPower<ArtifactPower>(), "第四回合不再补充人工制品");

        await Reset(); var stille = await RelicCmd.Obtain<Stille>(player); await stille.BeforeCombatStart();
        for (int i = 0; i < 3; i++) await stille.AfterPlayerTurnStart(context, player);
        Check(player.Creature.GetPowerAmount<DexterityPower>() == 2, "陨铁鸟敏捷保持到第三回合结束前");
        await stille.BeforeSideTurnEnd(context, CombatSide.Player, [player.Creature]);
        Check(player.Creature.GetPowerAmount<DexterityPower>() == 0, "第三回合结束陨铁鸟敏捷消失");

        await Reset(); var cloak = await RelicCmd.Obtain<ImmovableCloak>(player); await cloak.BeforeCombatStart();
        Check(cloak.ModifyHandDraw(player, 5) == 6 && cloak.ModifyHandDraw(other, 5) == 5, "未失去生命时额外抽牌且隔离玩家");
        await CreatureCmd.Damage(context, player.Creature, 1, ValueProp.Unblockable | ValueProp.Unpowered, enemy);
        Check(cloak.ModifyHandDraw(player, 5) == 5, "实际生命损失停用外套");
        await cloak.BeforeCombatStart(); Check(cloak.ModifyHandDraw(player, 5) == 6, "新战斗恢复外套");

        await Reset(); var pudding = await RelicCmd.Obtain<MercuryPudding>(player); player.Creature.SetCurrentHpInternal(10);
        await pudding.AfterCombatVictory(null!); int hp = player.Creature.CurrentHp;
        Check(pudding.IsUsedUp && hp == Math.Min(player.Creature.MaxHp, 10 + player.Creature.MaxHp / 2), "布丁低血量胜利治疗一次");
        player.Creature.SetCurrentHpInternal(10); await pudding.AfterCombatVictory(null!);
        Check(player.Creature.CurrentHp == 10 && ((MercuryPudding)RelicModel.FromSerializable(pudding.ToSerializable())).Used, "布丁耗尽状态持久化且不重复治疗");

        await Reset(); var ring = await RelicCmd.Obtain<MirroredLotusRing>(player); await ring.BeforeCombatStart();
        for (int i = 0; i < 4; i++) await ring.AfterPlayerTurnStart(context, player);
        var himmel = player.PlayerCombatState!.Hand.Cards.OfType<Himmel>().Single();
        await himmel.BeforeSideTurnEnd(context, CombatSide.Player, [player.Creature]);
        Check(player.PlayerCombatState.Hand.Cards.OfType<Repent>().Count() == 1 && himmel.Keywords.Contains(CardKeyword.Ethereal), "戒指第四回合生成辛美尔，回合结束生成后悔莫及");
        var returnA = Card<DefendFrieren>(); var returnB = Card<OrdinaryOffensiveMagic>();
        await CardPileCmd.Add(returnA, PileType.Discard); await CardPileCmd.Add(returnB, PileType.Discard);
        await Play(player.PlayerCombatState.Hand.Cards.OfType<Repent>().Single());
        Check(returnA.Pile?.Type == PileType.Hand && returnB.Pile?.Type == PileType.Hand &&
            returnA.EnergyCost.GetWithModifiers(CostModifiers.Local) == 0 && returnB.EnergyCost.GetWithModifiers(CostModifiers.Local) == 0,
            "后悔莫及检索弃牌堆并设为本回合免费");

        await Reset(); int maxEnergy = player.PlayerCombatState!.MaxEnergy;
        var bed = await RelicCmd.Obtain<ComfortableBed>(player); await bed.BeforeCombatStart();
        Check(player.PlayerCombatState.MaxEnergy == maxEnergy + 1 && player.PlayerCombatState.DrawPile.Cards.OfType<Laziness>().Count() == 1 &&
            player.PlayerCombatState.DiscardPile.Cards.OfType<Laziness>().Count() == 1, "舒适的床增加能量上限并生成两张懒懒散散");
        await RelicCmd.Remove(bed); Check(player.PlayerCombatState.MaxEnergy == maxEnergy, "移除床恢复能量上限");

        await Reset(); var scale = await RelicCmd.Obtain<ScaleOfObedience>(player);
        await CardPileCmd.Add(Card<DefendFrieren>(), PileType.Draw); energy = player.PlayerCombatState!.Energy;
        await scale.AfterPlayerTurnStart(context, player);
        Check(player.PlayerCombatState.Energy == energy + 1 && player.PlayerCombatState.Hand.Cards.Count == 1, "服从天平普通战斗提供能量和抽牌");

        await Reset(); var ice = await RelicCmd.Obtain<IcicleCherryBlossom>(player);
        var results = Enumerable.Range(0, 100).Select(_ => new CardCreationResult(run.CreateCard<DefendFrieren>(player))).ToList();
        Check(ice.TryModifyCardRewardOptionsLate(player, results, CardCreationOptions.ForRoom(player, RoomType.Monster)) &&
            results.Count(x => x.Card is Mana) is > 30 and < 85 && results.Where(x => x.Card is Mana).All(x => x.ModifyingRelics.Contains(ice)), "冰柱樱按各选项替换奖励并记录来源");

        await Reset(); var head = await RelicCmd.Obtain<MimicHead>(player); var rewards = new List<Reward>();
        Check(head.TryModifyRewards(player, rewards, new TreasureRoom(0)) && rewards.OfType<GoldReward>().Single().Amount == 100 &&
            !head.TryModifyRewards(other, [], new TreasureRoom(0)), "宝箱怪的头只为持有者添加100金币宝箱奖励");
        var house = await RelicCmd.Obtain<MickeyHouse>(player); int tools = MagicDeckStore.Read(player).Props.Count;
        await house.AfterRoomEntered(new EventRoom(ModelDb.AllEvents.First()));
        Check(MagicDeckStore.Read(player).Props.Count == tools + 1, "进入事件房间获得妙妙工具");

        await Reset(); var wand = await RelicCmd.Obtain<HaitaaWand>(player); await wand.AfterPlayerTurnStart(context, player);
        foreach (var pile in new[] { PileType.Hand, PileType.Draw, PileType.Discard }) await CardPileCmd.Add(Card<Mana>(), pile);
        await wand.BeforeSideTurnEnd(context, CombatSide.Player, [player.Creature]);
        Check(ManaCmd.Total(player) == 0 && player.Creature.GetPowerAmount<ConcentrationPower>() == 4, "海塔法杖消耗三个牌堆的魔力转为专注");

        await Reset(); int deckCount = player.Deck.Cards.Count; await RelicCmd.Obtain<HimmelGravestone>(player);
        var curse = player.Deck.Cards.OfType<LongevityPain>().Single();
        Check(player.Deck.Cards.Count == deckCount + 1 && !curse.IsRemovable && !curse.IsTransformable, "辛美尔之墓拾取实际加入不可移除诅咒");
        var combatCurse = Card<LongevityPain>(); await CardPileCmd.Add(combatCurse, PileType.Hand);
        for (int i = 0; i < 10; i++) await combatCurse.AfterCardPlayed(context, PlayInfo(Card<Mana>()));
        Check(combatCurse.Remaining == 10 && player.PlayerCombatState!.Hand.Cards.OfType<Fatalism>().Count() == 1, "连续十次同调生成寿命论");
        await combatCurse.AfterCardPlayed(context, PlayInfo(Card<Mana>())); await combatCurse.AfterCardPlayed(context, PlayInfo(Card<DefendFrieren>()));
        Check(combatCurse.Remaining == 10, "非同调牌中断连续计数");
        await CardCmd.Exhaust(context, combatCurse);
        Check(player.PlayerCombatState.Hand.Cards.OfType<LongevityPain>().Any(), "诅咒消耗后返回手牌");

        await Reset(); var bottled = (await PotionCmd.TryToProcure<BottledMana>(player)).potion;
        await bottled.OnUseWrapper(context, null);
        Check(ManaCmd.InPile(player, PileType.Hand).Count() == 3 && !player.Potions.Contains(bottled), "瓶装魔力通过原生药水使用流程生成3张并消耗");
        var dissolve = (await PotionCmd.TryToProcure<DissolveClothPotion>(player)).potion;
        await CreatureCmd.GainBlock(enemy, 20, ValueProp.Unpowered, null);
        await dissolve.OnUseWrapper(context, enemy);
        Check(enemy.Block == 0 && enemy.GetPowerAmount<WeakPower>() >= 3 && enemy.GetPowerAmount<VulnerablePower>() >= 3,
            "溶衣药水移除格挡并附加易伤虚弱");
        var wine = (await PotionCmd.TryToProcure<EmperorWine>(player)).potion;
        selector.PrepareToSelect(new[] { 0 }); await wine.OnUseWrapper(context, null);
        Check(player.PlayerCombatState.Hand.Cards.Any(c => c.Rarity == CardRarity.Rare && c.EnergyCost.GetWithModifiers(CostModifiers.Local) == 0),
            "皇帝酒从三个不同稀有选项选择并生成免费牌");

        await Reset(); var savedPudding = await RelicCmd.Obtain<MercuryPudding>(player); savedPudding.Used = true;
        var savedUbel = await RelicCmd.Obtain<UbelStaff>(player); savedUbel.Counter = 5;
        await PotionCmd.TryToProcure<BottledMana>(player); await PotionCmd.TryToProcure<DissolveClothPotion>(player);
        var savedPlayer = JsonSerializer.Serialize(player.ToSerializable(), JsonSerializationUtility.Options);
        var restoredPlayer = Player.FromSerializable(JsonSerializer.Deserialize<SerializablePlayer>(savedPlayer, JsonSerializationUtility.Options)!);
        Check(restoredPlayer.GetRelic<MercuryPudding>() is { Used: true, IsUsedUp: true } &&
            restoredPlayer.GetRelic<UbelStaff>() is { Counter: 5, Status: RelicStatus.Active }, "玩家完整存档恢复两件遗物状态");
        Check(restoredPlayer.Potions.OfType<BottledMana>().Single().Owner == restoredPlayer &&
            restoredPlayer.Potions.OfType<DissolveClothPotion>().Single().Owner == restoredPlayer &&
            restoredPlayer.Deck.Cards.OfType<LongevityPain>().Any(), "玩家完整存档恢复药水归属及永恒诅咒");

        await Reset(); player.Creature.SetMaxHpInternal(2000); player.Creature.SetCurrentHpInternal(2000);
        other.Creature.SetMaxHpInternal(2000); other.Creature.SetCurrentHpInternal(2000);
        enemy.SetCurrentHpInternal(2000); await CreatureCmd.GainBlock(player.Creature, 100, ValueProp.Unpowered, null);
        await Play(Card<Fatalism>());
        Check(player.Creature.CurrentHp == 1001 && other.Creature.CurrentHp == 1001 && enemy.CurrentHp == 1001,
            "寿命论对全部角色造成999点无视格挡的生命损失");
        LocManager.Instance.SetLanguage("eng");
        return count;
    }
}

using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;

public static class FactorBlockPreviewTests
{
    private sealed class InspectSelector(Action<FactorCard> inspect) : ICardSelector
    {
        public Task<IEnumerable<CardModel>> GetSelectedCards(IEnumerable<CardModel> options, int minSelect, int maxSelect) {
            var selected = options.OfType<FactorCard>().Single(c => c.Slot == ManaSlot.Draw);
            inspect(selected); return Task.FromResult<IEnumerable<CardModel>>([selected]);
        }
        public CardRewardSelection GetSelectedCardReward(IReadOnlyList<CardCreationResult> options, IReadOnlyList<CardRewardAlternative> alternatives)
            => throw new NotSupportedException();
    }

    public static async Task<int> Run()
    {
        int count = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception(name); GD.Print("PASS 因子格挡预览：" + name); count++; }
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "FRIEREN-FACTOR-BLOCK-PREVIEW");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = 1;
        var combat = new CombatState(runState: run); combat.AddPlayer(player);
        var enemy = combat.CreateCreature(ModelDb.Monster<MegaCrit.Sts2.Core.Models.Monsters.Axebot>().ToMutable(), CombatSide.Enemy, null);
        combat.AddCreature(enemy); CombatManager.Instance.SetUpCombat(combat);
        AccessTools.Property(typeof(CombatManager), nameof(CombatManager.IsInProgress)).SetValue(CombatManager.Instance, true);
        var context = new BlockingPlayerChoiceContext();
        var source = combat.CreateCard<DefensiveMagic>(player);
        var play = new CardPlay { Card = source, Target = null, IsAutoPlay = false, PlayIndex = 0, PlayCount = 1, ResultPile = PileType.Discard,
            Resources = new ResourceInfo { EnergySpent = 0, EnergyValue = 0, StarsSpent = 0, StarValue = 0 } };
        async Task Clear() {
            foreach (var power in player.Creature.Powers.ToArray()) await PowerCmd.Remove(power);
            await CreatureCmd.LoseBlock(player.Creature, decimal.MaxValue);
        }
        foreach (string language in new[] { "zhs", "eng" }) {
            LocManager.Instance.SetLanguage(language);
            foreach (string id in new[] { "BarrierMagic", "AdvancedDefensiveMagic" }) {
                foreach (var scenario in new (int Dex, bool Frail, int Expected)[] { (0, false, 4), (3, false, 7), (0, true, 3), (3, true, 5), (-8, false, 0) }) {
                    await Clear();
                    if (scenario.Dex != 0) await PowerCmd.Apply<DexterityPower>(context, player.Creature, scenario.Dex, player.Creature, null);
                    if (scenario.Frail) await PowerCmd.Apply<FrailPower>(context, player.Creature, 1, enemy, null);
                    var option = ChantRules.Preview(ManaSlot.Hand, MagicDeckState.Definition(id), 4);
                    var factor = FactorCard.Create(player, id, ManaSlot.Hand, option, chantSource: source);
                    string text = factor.GetDescriptionForPile(PileType.None);
                    string number = scenario.Expected > 4 ? $"[green]{scenario.Expected}[/green]" : scenario.Expected < 4 ? $"[red]{scenario.Expected}[/red]" : "4";
                    Check(text.Contains(language == "zhs" ? $"获得 {number} 点格挡" : $"Gain {number} Block"), $"{language} {id} 敏捷{scenario.Dex} 脆弱{scenario.Frail} 的数值与颜色");
                    Check(factor.GetDescriptionForPile(PileType.None) == text && factor.Option == option && factor.DynamicVars["Reward"].BaseValue == 4 &&
                        player.Creature.Block == 0 && player.Creature.GetPowerAmount<BlurPower>() == 0,
                        "重复预览不改写奖励、费用或提前给格挡和残影");
                    if (id == "BarrierMagic") Check(text.Contains(language == "zhs" ? "与 4 层残影" : "and 4 Blur"), "格挡增减不改变或染色残影层数");
                    await FactorEffects.Apply(context, play, option);
                    Check(player.Creature.Block == scenario.Expected * (id == "AdvancedDefensiveMagic" ? 2 : 1) &&
                        player.Creature.GetPowerAmount<BlurPower>() == (id == "BarrierMagic" ? 4 : 0), "实际结算与格挡预览及残影层数一致");
                }
                Check(FactorCard.Create(player, id).GetDescriptionForPile(PileType.None).Contains("X"), "百科和库存仍保留 X 规则文本");
            }
        }

        await Clear();
        CardCmd.Enchant<Nimble>(source, 2);
        var deck = new MagicDeckState();
        for (int i = 0; i < 3; i++) { deck.PrepareDraft(_ => 0); deck.SelectInitial(deck.Draft[0]); }
        deck.GrantStartingProps();
        foreach (string id in new[] { "BarrierMagic", "AdvancedDefensiveMagic" }) {
            await Clear(); deck.Equip(deck.Add(id).InstanceId, ManaSlot.Draw); MagicDeckStore.Write(player, deck);
            int cost = id == "BarrierMagic" ? 5 : 4;
            for (int i = 0; i < cost; i++) await CardPileCmd.Add(combat.CreateCard<Mana>(player), PileType.Draw);
            using var selector = CardSelectCmd.UseSelector(new InspectSelector(factor => {
                string text = factor.GetDescriptionForPile(PileType.None);
                Check(text.Contains("[green]6[/green]") && factor.Option!.Value.Cost == cost,
                    "真实咏唱选择使用来源牌附魔，费用不受预览影响：" + id);
            }));
            await ChantCmd.Choose(context, play, 4);
            Check(player.Creature.Block == (id == "BarrierMagic" ? 6 : 12) && ManaCmd.InPile(player, PileType.Draw).Count() == 0 &&
                player.Creature.GetPowerAmount<BlurPower>() == (id == "BarrierMagic" ? 4 : 0), "带附魔的实际咏唱与选择卡面一致：" + id);
        }
        await Clear(); LocManager.Instance.SetLanguage("eng");
        return count;
    }
}

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
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;

public static class GeneratedCardTests
{
    private static readonly List<CardPileAddResult> Previews = [];
    private static void Record(CardPileAddResult result) => Previews.Add(result);

    public static async Task<int> Run()
    {
        int count = 0;
        void Check(bool value, string name) { if (!value) throw new Exception(name); GD.Print("PASS 生成动画：" + name); count++; }
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "FRIEREN-GENERATED-CARDS");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = 1;
        var combat = new CombatState(runState: run); combat.AddPlayer(player);
        var enemy = combat.CreateCreature(ModelDb.Monster<MegaCrit.Sts2.Core.Models.Monsters.Axebot>().ToMutable(), CombatSide.Enemy, null);
        combat.AddCreature(enemy); CombatManager.Instance.SetUpCombat(combat);
        AccessTools.Property(typeof(CombatManager), nameof(CombatManager.IsInProgress)).SetValue(CombatManager.Instance, true);
        var context = new BlockingPlayerChoiceContext();
        // 记录调用真实原生动画入口的结果，TestMode 内部跳过画面，不改动存档或原生结算。
        var harmony = new Harmony("frieren.tests.generated_cards");
        var previewMethod = AccessTools.Method(typeof(CardCmd), nameof(CardCmd.PreviewCardPileAdd),
            [typeof(CardPileAddResult), typeof(float), typeof(CardPreviewStyle)]);
        harmony.Patch(previewMethod, prefix: new HarmonyMethod(typeof(GeneratedCardTests), nameof(Record)));
        T Card<T>(bool upgrade = false) where T : CardModel {
            var card = combat.CreateCard<T>(player);
            if (upgrade) { card.UpgradeInternal(); card.FinalizeUpgradeInternal(); }
            return card;
        }
        CardPlay Play(CardModel card) => new() { Card = card, Target = null, IsAutoPlay = false,
            ResultPile = PileType.Discard, PlayCount = 1, PlayIndex = 0,
            Resources = new ResourceInfo { EnergySpent = 0, EnergyValue = 0, StarsSpent = 0, StarValue = 0 } };
        async Task Effect(CardModel card) => await (Task)AccessTools.Method(card.GetType(), "OnPlay").Invoke(card, [context, Play(card)])!;
        void Reset() {
            foreach (var pile in player.PlayerCombatState!.AllPiles)
                foreach (var card in pile.Cards.ToArray()) pile.RemoveInternal(card);
            Previews.Clear();
        }
        bool ValidPreviews(int expected) => Previews.Count == expected && Previews.All(r => r.success &&
            r.cardAdded.Owner == player && r.oldPile == null && r.cardAdded.Pile?.Type is PileType.Draw or PileType.Discard) &&
            Previews.Select(r => r.cardAdded).Distinct().Count() == expected;
        try {
            foreach (bool upgrade in new[] { false, true }) {
                Reset(); await Effect(Card<ShavedIceSpell>(upgrade));
                int expected = upgrade ? 5 : 3;
                Check(ManaCmd.InPile(player, PileType.Draw).Count() == expected && ValidPreviews(expected),
                    "刨冰魔法" + (upgrade ? "升级" : "基础") + "生成的每张魔力调用原生飞入动画");
            }
            Reset(); await Effect(Card<Disperse>());
            Check(ManaCmd.InPile(player, PileType.Draw).Count() == 1 && ManaCmd.InPile(player, PileType.Hand).Count() == 2 &&
                ManaCmd.InPile(player, PileType.Discard).Count() == 3 && ValidPreviews(4),
                "分散的抽弃牌堆四张魔力展示飞入，手牌两张沿用原生入手动画且不重复");
            Reset();
            for (int i = 0; i < CardPile.MaxCardsInHand; i++) await CardPileCmd.Add(Card<Mana>(), PileType.Hand);
            await ManaCmd.Generate(player, PileType.Hand, 2);
            Check(ManaCmd.InPile(player, PileType.Hand).Count() == CardPile.MaxCardsInHand &&
                ManaCmd.InPile(player, PileType.Discard).Count() == 2 && Previews.Count == 0,
                "满手牌溢出弃牌沿用原生动画，不另加重复展示");
            Reset(); await PowerCmd.Apply<BanManaGainPower>(context, player.Creature, 1, player.Creature, null);
            await ManaCmd.Generate(player, PileType.Draw, 3);
            Check(ManaCmd.Total(player) == 0 && Previews.Count == 0, "禁止获得魔力时不产生卡牌或假动画");
            await ManaCmd.Generate(player, PileType.Draw, 3, ignoreBan: true);
            Check(ManaCmd.InPile(player, PileType.Draw).Count() == 3 && ValidPreviews(3), "明确忽略禁令的生成仍有原生动画");
            await PowerCmd.Remove(player.Creature.GetPower<BanManaGainPower>()!);
            Reset(); await ManaCmd.Generate(player, PileType.Draw, 0);
            Check(Previews.Count == 0 && ManaCmd.Total(player) == 0, "生成零张不播放动画");
            Reset(); await CardFlowCmd.Generate<GreenApple>(player, PileType.Draw, upgraded: true);
            Check(ValidPreviews(1) && Previews[0].cardAdded is GreenApple { IsUpgraded: true }, "通用生成入口保留升级状态和实际卡牌的动画");
            Reset(); await FactorEffects.Apply(context, Play(Card<RapidChant>()), new(ManaSlot.Discard, "BigFire", 2, 4, 2));
            Check(ValidPreviews(2) && Previews.All(r => r.cardAdded is LittleFire), "因子生成至弃牌堆的小小火花同样播放动画");
            Reset(); await FactorEffects.Apply(context, Play(Card<RapidChant>()), new(ManaSlot.Draw, "TheLivingLegend", 2, 2, 2));
            Check(ValidPreviews(2) && Previews.All(r => r.cardAdded is ILegendaryCard), "因子生成至抽牌堆的传说魔法同样播放动画");
        } finally { harmony.Unpatch(previewMethod, HarmonyPatchType.All, harmony.Id); }
        return count;
    }
}

using System.Text.Json;
using System.Text.RegularExpressions;
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
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;

public static class PlayFeedbackTests
{
    public static async Task<int> Run()
    {
        int count = 0;
        void Check(bool value, string name) { if (!value) throw new Exception(name); GD.Print("PASS 出牌提示：" + name); count++; }
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "FRIEREN-PLAY-FEEDBACK");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = 1;
        var combat = new CombatState(runState: run); combat.AddPlayer(player);
        var enemy = combat.CreateCreature(ModelDb.Monster<MegaCrit.Sts2.Core.Models.Monsters.Axebot>().ToMutable(), CombatSide.Enemy, null);
        combat.AddCreature(enemy); CombatManager.Instance.SetUpCombat(combat);
        AccessTools.Property(typeof(CombatManager), nameof(CombatManager.IsInProgress)).SetValue(CombatManager.Instance, true);
        MagicDeckStore.Write(player, MagicDeckState.CreatePrototypeLoadout());
        player.PlayerCombatState!.Energy = 10;
        var context = new BlockingPlayerChoiceContext();
        T Card<T>(bool upgrade = false) where T : CardModel {
            var card = combat.CreateCard<T>(player);
            if (upgrade) { card.UpgradeInternal(); card.FinalizeUpgradeInternal(); }
            return card;
        }
        var nativeMessage = AccessTools.Method(typeof(CardModel).Assembly.GetType(
            "MegaCrit.Sts2.Core.Entities.Cards.UnplayableReasonExtensions", true), "GetPlayerDialogueLine");
        string? Message(UnplayableReason reason, AbstractModel? preventer) =>
            ((LocString?)nativeMessage.Invoke(null, [reason, preventer]))?.GetFormattedText();
        string Text(string key) => new LocString("frieren", key).GetFormattedText();
        void Blocked(CardModel card, string key) {
            bool playable = card.CanPlay(out var reason, out var preventer);
            Check(!playable && reason == UnplayableReason.BlockedByCardLogic && ReferenceEquals(preventer, card) &&
                Message(reason, preventer) == Text(key), card.GetType().Name + " 使用具体失败原因：" + key);
        }
        CardPlay Play(CardModel card) => new() { Card = card, Target = null, IsAutoPlay = false,
            ResultPile = PileType.Discard, PlayCount = 1, PlayIndex = 0,
            Resources = new ResourceInfo { EnergySpent = 0, EnergyValue = 0, StarsSpent = 0, StarValue = 0 } };

        foreach (string language in new[] { "zhs", "eng" }) {
            LocManager.Instance.SetLanguage(language); CombatManager.Instance.History.Clear();
            player.PlayerCombatState.Energy = 10;
            foreach (var card in new CardModel[] { Card<RapidChant>(), Card<DefensiveMagic>(), Card<SecondDefensiveMagic>(),
                Card<FlowerFieldSpell>(), Card<PreciseChant>(), Card<FinalChant>(), Card<PerfectDefensiveMagic>() })
                Blocked(card, "PLAY.NO_MANA");
            Blocked(Card<OilSpell>(), "PLAY.NO_CHANT");
            Blocked(Card<ContinualChant>(), "PLAY.NO_CHANT");
            Check(Card<StrikeFrieren>().CanPlay(out var ordinaryReason, out var ordinaryPreventer) &&
                ordinaryReason == UnplayableReason.None && ordinaryPreventer == null, "普通卡不受咏唱条件影响：" + language);
            var inactive = Card<TimeConcept>(); inactive.CanPlay(out var inactiveReason, out var inactivePreventer);
            Check(inactivePreventer == null && Message(inactiveReason, inactivePreventer) == new LocString("combat_messages", "UNPLAYABLE").GetFormattedText(),
                "停用牌保留原版不可打出提示：" + language);
            var status = Card<Laziness>(); status.CanPlay(out var statusReason, out var statusPreventer);
            Check(statusPreventer == null && Message(statusReason, statusPreventer) == new LocString("combat_messages", "UNPLAYABLE").GetFormattedText(),
                "不可打出关键词保留原生提示：" + language);
            player.PlayerCombatState.Energy = 0;
            var defensive = Card<DefensiveMagic>(); defensive.CanPlay(out var energyReason, out var energyPreventer);
            Check(energyReason.HasFlag(UnplayableReason.EnergyCostTooHigh) && energyPreventer == null &&
                Message(energyReason, energyPreventer) == new LocString("combat_messages", "NOT_ENOUGH_ENERGY").GetFormattedText(),
                "能量与魔力同时不足时优先提示原生能量不足：" + language);
            player.PlayerCombatState.Energy = 10;
            CombatManager.Instance.History.CardPlayStarted(combat, Play(Card<RapidChant>()));
            Check(Card<OilSpell>().CanPlay(out var unlockedReason, out var unlockedPreventer) &&
                unlockedReason == UnplayableReason.None && unlockedPreventer == null, "本回合咏唱后传说魔法立即解锁：" + language);
            Check(Card<ContinualChant>().CanPlay(), "连环咏唱可先生成魔力时不被错误阻止：" + language);
            await PowerCmd.Apply<BanManaGainPower>(context, player.Creature, 1, player.Creature, null);
            Blocked(Card<ContinualChant>(), "PLAY.NO_MANA");
            await PowerCmd.Remove(player.Creature.GetPower<BanManaGainPower>()!);
            combat.RoundNumber++;
            Blocked(Card<OilSpell>(), "PLAY.NO_CHANT");
            var mana = Card<Mana>(); await CardPileCmd.Add(mana, PileType.Draw);
            Check(Card<RapidChant>().CanPlay(out var manaReason, out var manaPreventer) && manaReason == UnplayableReason.None && manaPreventer == null &&
                mana.Pile?.Type == PileType.Draw && player.PlayerCombatState.Energy == 10,
                "魔力足够恢复可打出，检查提示不会支付魔力或能量：" + language);
            mana.Pile!.RemoveInternal(mana);
            var model = Card<OilSpell>();
            foreach (var higherPriority in new[] { UnplayableReason.EnergyCostTooHigh, UnplayableReason.StarCostTooHigh,
                UnplayableReason.NoLivingAllies, UnplayableReason.HasUnplayableKeyword, UnplayableReason.BlockedByHook }) {
                string expectedKey = higherPriority switch {
                    UnplayableReason.EnergyCostTooHigh => "NOT_ENOUGH_ENERGY", UnplayableReason.StarCostTooHigh => "NOT_ENOUGH_STARS",
                    UnplayableReason.NoLivingAllies => "NO_LIVING_ALLIES", UnplayableReason.BlockedByHook => "BLOCKED_BY_HOOK", _ => "UNPLAYABLE"
                };
                var expected = new LocString("combat_messages", expectedKey);
                if (higherPriority == UnplayableReason.BlockedByHook) expected.Add("BlockingHook", model.Title);
                Check(Message(UnplayableReason.BlockedByCardLogic | higherPriority, model) == expected.GetFormattedText(),
                    "复合失败原因仍遵循原生优先级：" + higherPriority + "/" + language);
            }
        }
        var canonical = ModelDb.Card<RapidChant>(); canonical.CanPlay(out var noOwnerReason, out var noOwnerPreventer);
        Check(noOwnerReason == UnplayableReason.None && noOwnerPreventer == null, "百科无主卡不携带战斗失败原因");

        LocManager.Instance.SetLanguage("zhs");
        string Plain(string text) => Regex.Replace(Regex.Replace(text, @"\[[^\]]*\]", ""), @"\s+", "");
        var index = File.ReadLines(ProjectSettings.GlobalizePath("res://../../docs/inventory/card_names.jsonl"))
            .Select(line => JsonDocument.Parse(line).RootElement.Clone()).ToDictionary(row => row.GetProperty("model").GetString()!);
        foreach (var type in new[] { typeof(ElementsBombing), typeof(Envy), typeof(SpeakFromHeartSpell), typeof(ShavedIceSpell) }) {
            var row = index[type.Name]; string source = row.GetProperty("descriptionChinese").GetString()!;
            string[] versions = source.Replace("普通：\n", "").Split("\n\n升级：\n");
            for (int i = 0; i < 2; i++) {
                var card = combat.CreateCard(ModelDb.GetById<CardModel>(ModelDb.GetId(type)), player);
                if (i == 1) { card.UpgradeInternal(); card.FinalizeUpgradeInternal(); }
                Check(Plain(card.GetDescriptionForPile(PileType.None)) == Plain(versions[i]),
                    "已核定效果文案与实际卡面一致：" + type.Name + (i == 1 ? "+" : ""));
            }
        }
        LocManager.Instance.SetLanguage("eng");
        return count;
    }
}

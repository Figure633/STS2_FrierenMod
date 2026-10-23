using System.Text.Json;
using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
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
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Unlocks;

public static class UpgradedHolyEmblemTests
{
    public static async Task<int> Run()
    {
        int count = 0;
        void Check(bool ok, string text) { if (!ok) throw new Exception(text); count++; GD.Print("PASS 升级圣徽：" + text); }
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "UPGRADED-HOLY-EMBLEM");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = 1;
        var deck = new MagicDeckState();
        for (int i = 0; i < 3; i++) { deck.PrepareDraft(_ => 0); deck.SelectInitial(deck.Draft[0]); }
        deck.GrantStartingProps(); MagicDeckStore.Write(player, deck);
        var touch = (TouchOfOrobas)ModelDb.Relic<TouchOfOrobas>().ToMutable();
        Check(touch.SetupForPlayer(player) && touch.UpgradedRelic == ModelDb.Relic<UpgradedHolyEmblem>().Id,
            "原版先古升级选项经BaseLib识别升级圣徽");
        await RelicCmd.Obtain(touch, player);
        Check(player.GetRelic<HolyEmblem>() == null && player.GetRelic<UpgradedHolyEmblem>() != null,
            "原生替换移除旧圣徽，只保留升级版");
        Check(MagicDeckStore.Read(player).ToJson() == deck.ToJson(), "升级保留因子、槽位、魔具及赠品标记");
        var restored = Player.FromSerializable(JsonSerializer.Deserialize<SerializablePlayer>(
            JsonSerializer.Serialize(player.ToSerializable(), JsonSerializationUtility.Options), JsonSerializationUtility.Options)!);
        Check(restored.GetRelic<UpgradedHolyEmblem>() != null && MagicDeckStore.Read(restored).ToJson() == deck.ToJson(),
            "真实玩家序列化保留升级遗物及魔法书");
        var relic = player.GetRelic<UpgradedHolyEmblem>()!;
        foreach (var lang in new[] { "zhs", "eng" }) {
            LocManager.Instance.SetLanguage(lang);
            Check(!relic.Title.GetFormattedText().Contains("FRIERENMOD-") && !relic.DynamicDescription.GetFormattedText().Contains("LOC_ERR") &&
                relic.Icon.GetWidth() > 0 && relic.IconOutline.GetWidth() > 0, lang + "名称、描述及原图可用");
        }
        var combat = new CombatState(runState: run); combat.AddPlayer(player);
        combat.AddCreature(combat.CreateCreature(ModelDb.Monster<MegaCrit.Sts2.Core.Models.Monsters.Axebot>().ToMutable(),
            CombatSide.Enemy, null));
        CombatManager.Instance.SetUpCombat(combat);
        AccessTools.Property(typeof(CombatManager), nameof(CombatManager.IsInProgress)).SetValue(CombatManager.Instance, true);
        var context = new BlockingPlayerChoiceContext();
        void Clear() {
            foreach (var pile in new[] { PileType.Draw, PileType.Hand, PileType.Discard })
                foreach (var c in CardPile.Get(pile, player)!.Cards.ToArray()) c.RemoveFromCurrentPile();
        }
        for (int turn = 1; turn <= 2; turn++) {
            await relic.AfterPlayerTurnStart(context, player);
            Check(new[] { PileType.Draw, PileType.Hand, PileType.Discard }.All(p => ManaCmd.InPile(player,p).Count() == turn),
                "连续回合三个牌堆各增加一张且不叠加旧圣徽：" + turn);
        }
        var other = Player.CreateForNewRun<Frieren>(UnlockState.all, 2);
        await relic.AfterPlayerTurnStart(context, other);
        Check(ManaCmd.Total(player) == 6, "其他玩家回合不触发");
        Clear();
        for (int i = 0; i < 10; i++) await CardPileCmd.Add(combat.CreateCard<StrikeFrieren>(player), PileType.Hand);
        await relic.AfterPlayerTurnStart(context, player);
        Check(ManaCmd.InPile(player,PileType.Draw).Count() == 1 && ManaCmd.InPile(player,PileType.Hand).Count() == 0 &&
            ManaCmd.InPile(player,PileType.Discard).Count() == 2, "满手牌时新增手牌魔力按原生规则转入弃牌堆");
        Clear(); await PowerCmd.Apply<BanManaGainPower>(context, player.Creature, 1, player.Creature, null);
        await relic.AfterPlayerTurnStart(context, player);
        Check(ManaCmd.Total(player) == 0, "禁止获得魔力时三个牌堆均不生成");
        Check(MagicDeckStore.Read(player).ToJson() == deck.ToJson(), "回合触发不重复赠送开局因子及魔具");
        LocManager.Instance.SetLanguage("eng");
        return count;
    }
}



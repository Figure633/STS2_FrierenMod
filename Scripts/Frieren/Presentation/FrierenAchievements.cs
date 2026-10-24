using BaseLib.Patches.Hooks;
using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Monsters;
using FrierenMod.Scripts.Powers;
using FrierenMod.Scripts.Relics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace FrierenMod.Scripts.Presentation;

public static class FrierenAchievements
{
    public static bool Applies(Player player) => player.Character is Frieren && LocalContext.IsMe(player) && PresentationSettings.Achievements;
    public static bool Unlock(Player player, string id) => Applies(player) && SlotBackStore.Unlock(id);

    public static void TurnStarted(Player player)
    {
        if (!Applies(player) || player.PlayerCombatState == null) return;
        if (ManaCmd.Total(player) >= 100) Unlock(player, "5001");
        if (player.Creature.Block == 999) Unlock(player, "5002");
        if (player.Creature.Powers.Count(p => p.Type == PowerType.Buff) >= 5) Unlock(player, "5003");
        if (player.GetRelic<HimmelGravestone>() != null && player.GetRelic<StatueOfHimmel>() != null && player.GetRelic<MirroredLotusRing>() != null) Unlock(player, "5004");
        if (player.PlayerCombatState.DiscardPile.Cards.OfType<Laziness>().Count() >= 4) Unlock(player, "5005");
        if (player.PlayerCombatState.DrawPile.Cards.OfType<Dazed>().Count() >= 8) Unlock(player, "5006");
        if (MaxHandSizePatch.GetMaxHandSize(player, CardPile.MaxCardsInHand) == 0) Unlock(player, "5007");
        if (player.Creature.GetPowerAmount<StrengthPower>() >= 30) Unlock(player, "5015");
        var equipped = MagicDeckStore.Read(player).Items.Where(i => i.Slot >= 0).ToArray();
        if (equipped.Length == 3 && equipped.All(i => i.SpellId == "BluePrint")) Unlock(player, "7001");
    }

    public static void CardPlayed(CardPlay play)
    {
        var card = play.Card; var player = card.Owner;
        if (!Applies(player)) return;
        if (ChantCmd.IsChant(card)) Unlock(player, "0004");
        if (card is ILegendaryCard) Unlock(player, "0005");
        if (card is LightningMagic) Unlock(player, "4001");
        if (card is HellFireSummoning) Unlock(player, "4002");
        if (card is Fatalism) Unlock(player, "4007");
        if (card is AccessoriesSpell) Unlock(player, "4010");
    }

    public static void CheckDamage(CardModel card, Creature? target, decimal amount, int threshold, string id)
    {
        if (!Applies(card.Owner)) return;
        var damage = new DamageVar(amount, ValueProp.Move);
        damage.UpdateCardPreview(card, CardPreviewMode.Normal, target, true);
        if (damage.PreviewValue >= threshold) Unlock(card.Owner, id);
    }

    public static void MonsterDied(ICombatState? combat, Creature creature, bool prevented)
    {
        if (combat == null || prevented || creature.IsAlive) return;
        foreach (var player in combat.Players.Where(Applies)) {
            if (creature.Monster is SpiegelFrieren) {
                Unlock(player, "0007");
                if (player.PlayerCombatState!.TurnNumber <= 3) Unlock(player, "6002");
            }
            if (creature.Monster is Monsters.Mimic && CombatManager.Instance.History.CardPlaysStarted.Count() <= 5) Unlock(player, "6001");
        }
    }

    public static void RunEnded(RunState state, Player player, bool victory)
    {
        if (!Applies(player) || RunManager.Instance.IsAbandoned) return;
        if (!victory) {
            if (!player.Creature.IsDead || state.CurrentRoom is not CombatRoom room) return;
            Unlock(player, "0008");
            if (room.Encounter.MonstersWithSlots.Any(m => m.Item1 is HunterKiller)) Unlock(player, "0009");
            if (room.Encounter.MonstersWithSlots.Any(m => m.Item1 is SpiegelFrieren)) Unlock(player, "0010");
            return;
        }
        var deck = MagicDeckStore.Read(player);
        var equipped = deck.Items.Where(i => i.Slot >= 0).ToArray();
        if (player.Deck.Cards.Count >= 40) Unlock(player, "5008");
        if (deck.Items.Count + deck.Props.Count >= 30) Unlock(player, "5009");
        if (equipped.Length == 3) {
            if (equipped.All(i => SpellCatalog.Get(i.SpellId).Rarity == FactorRarity.Uncommon)) Unlock(player, "5010");
            if (equipped.All(i => SpellCatalog.Get(i.SpellId).Rarity == FactorRarity.Common)) Unlock(player, "5011");
            if (equipped.Select(i => i.SpellId).Distinct().Count() == 1) Unlock(player, "5013");
        }
        if (RunManager.Instance.RunTime >= 7200) Unlock(player, "5012");
        if (deck.Props.Count(p => p.PropId == "UnbelievableTool") >= 5) Unlock(player, "5014");
        long startTime = (long)AccessTools.Field(typeof(RunManager), "_startTime").GetValue(RunManager.Instance)!;
        SlotBackStore.GrantVictory($"{startTime}:{state.Rng.StringSeed}:{player.NetId}", state.AscensionLevel);
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterPlayerTurnStart))]
internal static class AchievementTurnHook
{
    private static void Postfix(Player player, ref Task __result) => __result = After(__result, player);
    private static async Task After(Task original, Player player) { await original; FrierenAchievements.TurnStarted(player); }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardPlayed))]
internal static class AchievementCardHook
{
    private static void Postfix(CardPlay cardPlay, ref Task __result) => __result = After(__result, cardPlay);
    private static async Task After(Task original, CardPlay play) { await original; FrierenAchievements.CardPlayed(play); }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterDeath))]
internal static class AchievementDeathHook
{
    private static void Postfix(ICombatState? combatState, Creature creature, bool wasRemovalPrevented, ref Task __result) =>
        __result = After(__result, combatState, creature, wasRemovalPrevented);
    private static async Task After(Task original, ICombatState? combat, Creature creature, bool prevented) {
        await original; FrierenAchievements.MonsterDied(combat, creature, prevented);
    }
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.OnEnded))]
internal static class AchievementRunHook
{
    private static void Postfix(RunManager __instance, bool isVictory)
    {
        // 挂到实际跑局结算，避免依赖当前版本仍为空实现的平台成就辅助方法。
        if (__instance.DebugOnlyGetState() is { } state && LocalContext.GetMe(state) is { } player)
            FrierenAchievements.RunEnded(state, player, isVictory);
    }
}

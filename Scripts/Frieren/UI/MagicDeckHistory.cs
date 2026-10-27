using System.Text.Json.Serialization.Metadata;
using BaseLib.Utils;
using FrierenMod.Core;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Unlocks;

namespace FrierenMod.Scripts.UI;

public static class MagicDeckHistory
{
    public static readonly SpireField<RunHistoryPlayer, string> Data = new(() => "");
    [ThreadStatic] private static Dictionary<ulong, string>? _pending;

    // 历史记录不经过玩家 SavedProperties；给原生 JSON 元数据增加一个可选字段。
    [HarmonyPatch("MegaCrit.Sts2.Core.Saves.MegaCritSerializerContext", "Create_RunHistoryPlayer")]
    private static class HistoryJson
    {
        private static void Postfix(JsonTypeInfo<RunHistoryPlayer> __result)
        {
            var property = __result.CreateJsonPropertyInfo(typeof(string), "frieren_magic_deck");
            property.Get = obj => Data.Get((RunHistoryPlayer)obj);
            property.Set = (obj, value) => Data.Set((RunHistoryPlayer)obj, value as string ?? "");
            property.ShouldSerialize = (_, value) => value is string { Length: > 0 };
            __result.Properties.Add(property);
        }
    }

    [HarmonyPatch(typeof(RunHistoryUtilities), nameof(RunHistoryUtilities.CreateRunHistoryEntry))]
    private static class Capture
    {
        private static void Prefix(SerializableRun run)
        {
            _pending = run.Players.Where(p => p.CharacterId == ModelDb.Character<Frieren>().Id)
                .ToDictionary(p => p.NetId, p => MagicDeckStore.Read(Player.FromSerializable(p)).ToJson());
        }
        private static void Finalizer() => _pending = null;
    }

    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.SaveRunHistory))]
    private static class SaveSnapshot
    {
        private static void Prefix(RunHistory history)
        {
            if (_pending == null) return;
            foreach (var player in history.Players)
                if (_pending.TryGetValue(player.Id, out var json)) Data.Set(player, json);
        }
    }

    [HarmonyPatch(typeof(NRunHistory), "SelectPlayer")]
    private static class HistoryButton
    {
        private static readonly SpireField<NRunHistory, MagicBookScreen?> Books = new(() => null);
        private static readonly SpireField<NRunHistory, MagicFactorButton?> Buttons = new(() => null);
        private static void Postfix(NRunHistory __instance, NRunHistoryPlayerIcon playerIcon)
        {
            if (Books.Get(__instance) is { } old) { old.Close(); old.View.QueueFree(); Books.Set(__instance, null); }
            var button = Buttons.Get(__instance);
            if (button == null)
            {
                button = MagicFactorButton.Create(__instance);
                Buttons.Set(__instance, button);
                button.Released += _ => Books.Get(__instance)?.Open();
            }
            string json = Data.Get(playerIcon.Player) ?? "";
            button.Visible = json.Length > 0 && playerIcon.Player.Character == ModelDb.Character<Frieren>().Id;
            if (!button.Visible) return; // 旧历史没有保存魔法牌组，不能用当前牌组冒充。
            var player = Player.CreateForNewRun<Frieren>(UnlockState.all, playerIcon.Player.Id);
            var deck = MagicDeckState.FromJson(json);
            MagicDeckStore.Data.Set(player, json);
            button.SetItemCount(deck.Items.Count + deck.Props.Count);
            Books.Set(__instance, new MagicBookScreen(__instance, player, readOnly: true));
        }
    }
}

using BaseLib.Utils;
using FrierenMod.Core;
using FrierenMod.Scripts.Relics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;

namespace FrierenMod.Scripts.Mechanics;

public static class MagicDeckStore
{
    // 绑定玩家存档，移除圣徽不会丢失魔法书；string 使用 BaseLib 原生包序列化。
    public static readonly SavedSpireField<Player, string> Data = new(() => "", "FrierenMagicDeck");

    public static MagicDeckState Read(Player player)
    {
        string? json = Data.Get(player);
        if (string.IsNullOrEmpty(json))
        {
            json = player.GetRelic<HolyEmblem>()?.MagicDeckJson ?? new MagicDeckState().ToJson();
            Data.Set(player, json);
        }
        return MagicDeckState.FromJson(json);
    }

    public static void Write(Player player, MagicDeckState deck, int? usedProp = null)
    {
        var before = Read(player);
        Data.Set(player, deck.ToJson());
        if (CombatManager.Instance.IsInProgress && player.Creature.CombatState != null &&
            !before.Items.Where(i => i.Slot >= 0).OrderBy(i => i.Slot)
                .SequenceEqual(deck.Items.Where(i => i.Slot >= 0).OrderBy(i => i.Slot))) {
            // 装载影响手牌的可用条件；原生通知统一重算手牌图标、高亮及当前选择，不改动牌堆。
            player.PlayerCombatState?.Hand.InvokeContentsChanged();
        }
        UI.MagicDeckAnimations.Changed(player, before, deck, usedProp);
    }
}

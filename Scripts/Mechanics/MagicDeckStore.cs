using BaseLib.Utils;
using FrierenMod.Core;
using FrierenMod.Scripts.Relics;
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

    public static void Write(Player player, MagicDeckState deck) => Data.Set(player, deck.ToJson());
}

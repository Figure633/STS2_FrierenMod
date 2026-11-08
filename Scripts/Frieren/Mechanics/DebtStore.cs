using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;

namespace FrierenMod.Scripts.Mechanics;

// 负债属于本局的玩家，交给原生存档与网络存档保存，不扣除金币。
public static class DebtStore
{
    public static readonly SavedSpireField<Player, int> Data = new(() => 0, "FrierenDebt");
    public static int Read(Player player) => Math.Max(0, Data.Get(player));
    public static void Add(Player player, int amount)
    {
        Data.Set(player, (int)Math.Clamp((long)Read(player) + amount, 0, int.MaxValue));
        player.PlayerCombatState?.Hand.InvokeContentsChanged();
    }
}

using System.Text.Json;
using BaseLib.Utils;
using FrierenMod.Core;
using MegaCrit.Sts2.Core.Entities.Players;

namespace FrierenMod.Scripts.Mechanics;

public sealed class DragonBallState
{
    public bool Started { get; set; }
    public int GrantedMask { get; set; }
    public int DropChance { get; set; } = 10;
    public bool EventSeen { get; set; }
    public int EventMisses { get; set; }
    public int LastUnknownFloor { get; set; } = -1;
    public string LastRewardRoom { get; set; } = "";
    public bool WishUsed { get; set; }
    public string Wish { get; set; } = "";
    public bool WishResolved { get; set; }
    public int WishGoldBefore { get; set; }
    public int WishCourierBefore { get; set; }
    public bool Summoning { get; set; }
    public int CampFloor { get; set; } = -1;
    public string[]? CampOptions { get; set; }
}

public static class DragonBallStore
{
    public static readonly SavedSpireField<Player, string> Data = new(() => "", "FrierenDragonBalls");
    public static string Ball(int number) => "DragonBall" + number;
    public static bool IsBall(string id) => id.Length == 11 && id.StartsWith("DragonBall", StringComparison.Ordinal) && id[^1] is >= '1' and <= '7';
    public static DragonBallState Read(Player player)
    {
        string? json = Data.Get(player);
        var state = string.IsNullOrEmpty(json) ? new DragonBallState() : JsonSerializer.Deserialize<DragonBallState>(json)!;
        foreach (var prop in MagicDeckStore.Read(player).Props.Where(p => IsBall(p.PropId))) {
            state.Started = true; state.GrantedMask |= 1 << (prop.PropId[^1] - '1');
        }
        return state;
    }
    public static void Write(Player player, DragonBallState state) => Data.Set(player, JsonSerializer.Serialize(state));
    public static void RecordOwned(Player player, MagicDeckState deck)
    {
        int mask=0;
        foreach(var prop in deck.Props.Where(p=>IsBall(p.PropId))) mask |= 1 << (prop.PropId[^1]-'1');
        if(mask==0)return;
        string? json=Data.Get(player);
        var state=string.IsNullOrEmpty(json)?new DragonBallState():JsonSerializer.Deserialize<DragonBallState>(json)!;
        if(state.Started && (state.GrantedMask & mask)==mask)return;
        state.Started=true;state.GrantedMask|=mask;Write(player,state);
    }
    public static int Next(Player player)
    {
        var state = Read(player);
        if (!state.Started || state.WishUsed) return 0;
        return Enumerable.Range(1, 7).FirstOrDefault(n => (state.GrantedMask & (1 << (n - 1))) == 0);
    }
    public static int Held(Player player) => MagicDeckStore.Read(player).Props.Where(p => IsBall(p.PropId)).Select(p => p.PropId).Distinct().Count();
    public static bool CanSummon(Player player) => !Read(player).WishUsed && Held(player) == 7;
    public static bool GrantNext(Player player, bool start = false)
    {
        var state = Read(player);
        if (start) { state.Started = true; Write(player, state); }
        int number = Next(player);
        if (number == 0) return false;
        var deck = MagicDeckStore.Read(player);
        deck.AddProp(Ball(number)); state.GrantedMask |= 1 << (number - 1);
        Write(player, state); MagicDeckStore.Write(player, deck); return true;
    }
    public static bool ConsumeForWish(Player player, string wish)
    {
        if (!CanSummon(player)) return false;
        var deck = MagicDeckStore.Read(player);
        foreach (var prop in deck.Props.Where(p => IsBall(p.PropId)).ToArray()) deck.Remove(prop.InstanceId);
        var state = Read(player); state.WishUsed = true; state.Wish = wish;
        state.WishGoldBefore=player.Gold;
        state.WishCourierBefore=player.Relics.OfType<MegaCrit.Sts2.Core.Models.Relics.TheCourier>().Count();
        Write(player, state); MagicDeckStore.Write(player, deck); return true;
    }
}

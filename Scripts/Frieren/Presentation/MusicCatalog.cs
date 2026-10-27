using MegaCrit.Sts2.Core.Rooms;

namespace FrierenMod.Scripts.Presentation;

public enum MusicPool { Regular, Event, Boss, Spiegel, Merchant, Defeat }

public sealed record MusicTrack(string Id, string Title, MusicPool Pool)
{
    public string Path => MusicCatalog.Folder + Id + ".ogg";
}

public static class MusicCatalog
{
    public const string Folder = "res://FrierenMod/audio/bgm/";
    public static readonly IReadOnlyList<MusicTrack> All = [
        new("grassy_turtles", "Grassy Turtles and Seed Rats", MusicPool.Event),
        new("time_flows", "Time Flows Ever Onward", MusicPool.Event),
        new("odd_clouds", "Odd Clouds", MusicPool.Event),
        new("heroes_return", "The Party of Heroes Returns", MusicPool.Event),
        new("grimoires", "Too Many Grimoires", MusicPool.Event),
        new("waltz", "Waltz for Stark and Fern", MusicPool.Event),
        new("dragon_smasher", "Dragon Smasher", MusicPool.Regular),
        new("deep_dungeon", "Deep in the Dungeon", MusicPool.Regular),
        new("ready_weapon", "Ready Your Weapon", MusicPool.Regular),
        new("magic_within", "The Magic Within", MusicPool.Regular),
        new("knife_throat", "Knife to the Throat", MusicPool.Regular),
        new("dangerous_medley", "New and Dangerous Magic (Medley)", MusicPool.Boss),
        new("zoltraak", "Zoltraak", MusicPool.Boss),
        new("divine_revolte", "Divine Revolte", MusicPool.Boss),
        new("first_class_mage", "First-Class Mage", MusicPool.Boss),
        new("golden_land", "The Golden Land", MusicPool.Boss),
        new("south_hero", "Hero of the South", MusicPool.Boss),
        new("frieren_slayer", "Frieren the Slayer", MusicPool.Spiegel),
        new("sleepy_frieren", "Sleepy Frieren", MusicPool.Merchant),
        new("peace", "In Times of Peace", MusicPool.Defeat),
    ];
    public static MusicTrack Defeat => All.Single(t=>t.Pool==MusicPool.Defeat);
    public static IReadOnlyList<MusicTrack> For(MusicPool pool) => All.Where(t=>t.Pool==pool).ToArray();
    public static bool Loops(MusicPool pool) => pool is MusicPool.Spiegel or MusicPool.Merchant;
    public static MusicPool Resolve(RoomType room, bool fighting, bool spiegel = false) =>
        fighting && spiegel ? MusicPool.Spiegel :
        fighting && room is RoomType.Boss or RoomType.Elite ? MusicPool.Boss :
        room == RoomType.Shop ? MusicPool.Merchant :
        room is RoomType.Event or RoomType.RestSite ? MusicPool.Event : MusicPool.Regular;
}

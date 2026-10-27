using FrierenMod.Core;
using FrierenMod.Scripts.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using System.Text.Json;

public static class NativeSaveTests
{
    public static int Run()
    {
        int passed = 0;
        void Test(string name, Action action) { action(); passed++; Godot.GD.Print("PASS " + name); }
        void Equal<T>(T expected, T actual) { if (!Equals(expected, actual)) throw new Exception($"Expected {expected}; actual {actual}"); }
Test("原生 SavedProperties JSON 属性载入", () => {
    var a = (HolyEmblem)ModelDb.Relic<HolyEmblem>().ToMutable(); var deck = a.ReadMagicDeck(); deck.Add("BuildUpBody"); a.SaveMagicDeck(deck);
    var props = SavedProperties.FromInternal(a, null)!;
    Equal(a.MagicDeckJson, props.strings!.Single(x => x.name == nameof(HolyEmblem.MagicDeckJson)).value);
    var json = JsonSerializer.Serialize(props, JsonSerializationUtility.Options);
    var restored = JsonSerializer.Deserialize<SavedProperties>(json, JsonSerializationUtility.Options)!;
    var b = (HolyEmblem)ModelDb.Relic<HolyEmblem>().ToMutable(); restored.Fill(b); Equal(a.MagicDeckJson, b.MagicDeckJson);
});
Test("原生网络包往返保持 Magic Deck", () => {
    var a = (HolyEmblem)ModelDb.Relic<HolyEmblem>().ToMutable();
    var props = SavedProperties.FromInternal(a, null)!;
    var writer = new PacketWriter(); props.Serialize(writer);
    var reader = new PacketReader(); reader.Reset(writer.Buffer);
    var restored = new SavedProperties(); restored.Deserialize(reader);
    var b = (HolyEmblem)ModelDb.Relic<HolyEmblem>().ToMutable(); restored.Fill(b); Equal(a.MagicDeckJson, b.MagicDeckJson);
    Equal(writer.BitPosition, reader.BitPosition);
});
return passed;
    }
}

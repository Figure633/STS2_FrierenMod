using System.Text.Json;
using FrierenMod.Scripts.Events;
using Godot;
using MegaCrit.Sts2.Core.Models;

namespace FrierenMod.Scripts.Presentation;

// 焦点是原图内的归一化坐标；目标是屏幕内的归一化坐标，不修改作者图片。
public sealed class EventArtComposition
{
    public string? Artist { get; set; }
    public string? Source { get; set; }
    public string Status { get; set; } = "unconfirmed";
    public float[] Focus { get; set; } = [0.5f, 0.5f];
    public float[] Target { get; set; } = [0.76f, 0.28f];
    public bool Centered { get; set; } = true;
    public bool FullScene { get; set; }

    private static readonly Lazy<Dictionary<string, EventArtComposition>> Catalog = new(() =>
        JsonSerializer.Deserialize<Dictionary<string, EventArtComposition>>(
            Godot.FileAccess.GetFileAsString("res://FrierenMod/data/event_art.json"),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!);
    public static EventArtComposition For(EventModel model) => model switch {
        EventArtPreview preview => Catalog.Value[EventArtPreview.Images[preview.ArtIndex]],
        HollowKnightAdventure => Catalog.Value["BugKnight"],
        JojoAdventure => Catalog.Value["jojo"],
        SealingOreMine mine => new(){FullScene=mine.ArtIndex is 0 or 1},
        _ => new()
    };

    public Rect2 BackgroundRect(Vector2 viewport, Vector2 texture)
    {
        var scaled = texture * Mathf.Max(viewport.X / texture.X, viewport.Y / texture.Y);
        var position = Centered ? (viewport - scaled) / 2
            : viewport * new Vector2(Target[0], Target[1]) - scaled * new Vector2(Focus[0], Focus[1]);
        return new Rect2(position, scaled);
    }
}

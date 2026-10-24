using BaseLib.Abstracts;
using FrierenMod.Scripts.Cards;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace FrierenMod.Scripts.Presentation;

public static class FrierenCardFrames
{
    private static readonly Dictionary<(string Kind, string Style), (Texture2D Texture, ShaderMaterial Material)> Frames = [];

    private static bool Applies(CustomCardModel card) => card.Rarity != CardRarity.Ancient &&
        (card is IManaCard || card.Type is not (CardType.Status or CardType.Curse));

    public static Texture2D? Texture(CustomCardModel card) => Applies(card) ? Get(card).Texture : null;
    public static Material? Material(CustomCardModel card) => Applies(card) ? Get(card).Material : null;

    private static (Texture2D Texture, ShaderMaterial Material) Get(CustomCardModel card)
    {
        string kind = card.Type switch { CardType.Attack => "attack", CardType.Power => "power", _ => "skill" };
        // 一代因子、魔具和魔力共用紫色卡背，七张 FRIEREN_FERN_CARD 另用双色卡背。
        bool shared = card is BabySleeping or ShavedIceSpell or ManaDetection or GrapeSpell or FlyingMagic or Disperse or ClearMind or ManaAffinity or Snuggle;
        string style = card is FactorCard or MagicPropCard or IManaCard ? "magic" : shared ? "shared" : "frieren";
        var key = (kind, style);
        if (Frames.TryGetValue(key, out var cached)) return cached;

        string file = style == "magic" ? style : $"{style}_{kind}";
        var texture = GD.Load<AtlasTexture>($"res://FrierenMod/images/card_frames/{file}.tres");
        var native = GD.Load<AtlasTexture>($"res://images/atlases/ui_atlas.sprites/card/card_frame_{kind}_s.tres");
        var material = new ShaderMaterial { Shader = GD.Load<Shader>("res://FrierenMod/shaders/legacy_card_frame.gdshader") };
        Vector2 sourceSize = texture.Atlas.GetSize(), nativeSize = native.Atlas.GetSize();
        material.SetShaderParameter("source_region", new Vector4(texture.Region.Position.X / sourceSize.X,
            texture.Region.Position.Y / sourceSize.Y, texture.Region.Size.X / sourceSize.X, texture.Region.Size.Y / sourceSize.Y));
        material.SetShaderParameter("frame_atlas", native.Atlas);
        material.SetShaderParameter("frame_region", new Vector4(native.Region.Position.X / nativeSize.X,
            native.Region.Position.Y / nativeSize.Y, native.Region.Size.X / nativeSize.X, native.Region.Size.Y / nativeSize.Y));
        return Frames[key] = (texture, material);
    }
}

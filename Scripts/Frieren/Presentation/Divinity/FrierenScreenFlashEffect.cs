// Adapted from WatcherMod, Copyright (c) 2026 Laurin Liebhart and contributors. MIT; see LICENSE.WATCHER.
using Godot;
using MegaCrit.Sts2.Core.Helpers;

namespace FrierenMod.Scripts.Presentation.Divinity;

[GlobalClass]
public partial class FrierenScreenFlashEffect : CanvasLayer
{
    private const string TexturePath = "res://FrierenMod/images/vfx/screenflash.png";
    private const float FadeInDuration = 0.12f;
    private const float FlashDuration = 0.7f;
    private float _elapsed;
    private Color _flashColor;

    private Texture2D _texture = null!;
    private TextureRect _tex = null!;

    public static void Play(Color color)
    {
        if (NonInteractiveMode.IsActive) return;

        var flash = new FrierenScreenFlashEffect();
        flash._flashColor = color;
        flash.Layer = 100;

        var tree = Engine.GetMainLoop() as SceneTree;
        tree?.Root.AddChild(flash);
    }

    public override void _Ready()
    {
        if (!FrierenStanceVfx.TryGetTexture(TexturePath, ref _texture))
        {
            QueueFree();
            return;
        }

        _tex = new TextureRect();
        _tex.MouseFilter = Control.MouseFilterEnum.Ignore;
        _tex.Texture = _texture;
        _tex.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        _tex.StretchMode = TextureRect.StretchModeEnum.Scale;
        _tex.AnchorRight = 1;
        _tex.AnchorBottom = 1;
        _tex.Modulate = _flashColor;
        AddChild(_tex);
    }

    public override void _Process(double delta)
    {
        if (!IsInstanceValid(_tex))
        {
            QueueFree();
            return;
        }

        _elapsed += (float)delta;
        if (_elapsed >= FlashDuration)
        {
            QueueFree();
            return;
        }

        float alpha;
        if (_elapsed < FadeInDuration)
        {
            alpha = _elapsed / FadeInDuration;
        }
        else
        {
            var fadeOut = (_elapsed - FadeInDuration) / (FlashDuration - FadeInDuration);
            alpha = 1f - fadeOut * fadeOut;
        }

        _tex.Modulate = new Color(_flashColor.R, _flashColor.G, _flashColor.B, alpha);
    }
}

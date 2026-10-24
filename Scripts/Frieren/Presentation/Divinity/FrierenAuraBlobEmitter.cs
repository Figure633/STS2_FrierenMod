// Adapted from WatcherMod, Copyright (c) 2026 Laurin Liebhart and contributors. MIT; see LICENSE.WATCHER.
using Godot;

namespace FrierenMod.Scripts.Presentation.Divinity;

[GlobalClass]
public partial class FrierenAuraBlobEmitter : Node2D
{
    private const string TexturePath = "res://FrierenMod/images/vfx/big_blur.png";

    [Export] public Color BlobColor { get; set; } = new(0.15f, 0f, 0.03f);

    private Texture2D _texture = null!;

    public override void _Ready()
    {
        const float s = FrierenStanceVfx.VfxScale;
        Position *= s;

        if (!FrierenStanceVfx.TryGetTexture(TexturePath, ref _texture))
        {
            QueueFree();
            return;
        }

        var ramp = new Gradient();
        ramp.Offsets = [0f, 0.3f, 0.5f, 0.7f, 1f];
        ramp.Colors =
        [
            new Color(1, 1, 1, 0f),
            new Color(1, 1, 1, 0.6f),
            new Color(1, 1, 1, 0.6f),
            new Color(1, 1, 1, 0.3f),
            new Color(1, 1, 1, 0f)
        ];

        var cpu = new CpuParticles2D();
        cpu.Texture = _texture;
        cpu.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };

        cpu.Amount = 6;
        cpu.Lifetime = 2.0f;
        cpu.Preprocess = 2.0f;
        cpu.Direction = new Vector2(0, -1);
        cpu.Spread = 180f;
        cpu.Gravity = Vector2.Zero;
        cpu.InitialVelocityMin = 0f;
        cpu.InitialVelocityMax = 10f * s;
        cpu.ScaleAmountMin = 3.44f * s;
        cpu.ScaleAmountMax = 4.5f * s;
        cpu.AngleMin = 0f;
        cpu.AngleMax = 360f;
        cpu.AngularVelocityMin = -20f;
        cpu.AngularVelocityMax = 20f;
        cpu.Color = BlobColor;
        cpu.ColorRamp = ramp;
        cpu.EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle;
        cpu.EmissionRectExtents = new Vector2(38f * s, 63f * s);

        cpu.Emitting = true;
        AddChild(cpu);
    }
}

using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Powers;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Rooms;

namespace FrierenMod.Scripts.Presentation.Divinity;

// 光环直接复用 WatcherMod；只将其生命周期接到原生能力和战斗事件。
public partial class DivinityPresentation : Node2D
{
    private Creature _owner = null!;
    private CanvasItem _body = null!;
    private Color _originalTint;
    private Node2D? _aura;
    public bool IsActive => GodotObject.IsInstanceValid(_aura);

    public static DivinityPresentation Attach(Creature owner, Node2D visuals, CanvasItem body)
    {
        if (visuals.GetNodeOrNull<DivinityPresentation>(nameof(DivinityPresentation)) is { } existing) return existing;
        var effect = new DivinityPresentation { Name = nameof(DivinityPresentation), _owner = owner, _body = body };
        visuals.AddChild(effect);
        return effect;
    }

    public override void _Ready()
    {
        _owner.PowerApplied += OnApplied;
        _owner.PowerRemoved += OnRemoved;
        CombatManager.Instance.CombatEnded += OnCombatEnded;
        // 从战斗存档恢复时不重播进入爆发或震屏。
        if (_owner.HasPower<DivinityPower>()) StartAura(false);
    }

    private void OnApplied(PowerModel power) { if (power is DivinityPower) StartAura(true); }
    private void OnRemoved(PowerModel power) { if (power is DivinityPower) StopAura(); }
    private void OnCombatEnded(CombatRoom _) => StopAura();

    private void StartAura(bool entering)
    {
        if (IsActive) return;
        _originalTint = _body.Modulate;
        _body.Modulate *= new Color(1.1f, 0.7f, 1.4f);
        _aura = GD.Load<PackedScene>("res://FrierenMod/scenes/divinity_aura.tscn").Instantiate<Node2D>();
        if (!entering) _aura.GetNode("ActivationBurst").Free();
        AddChild(_aura);
        var loop = (AudioStreamOggVorbis)GD.Load<AudioStreamOggVorbis>("res://FrierenMod/audio/divinity_loop.ogg").Duplicate();
        loop.Loop = true;
        var ambience = new AudioStreamPlayer { Name = "DivinityAmbience", Stream = loop, VolumeDb = -6,
            Bus = AudioServer.GetBusIndex("SFX") >= 0 ? "SFX" : "Master" };
        _aura.AddChild(ambience); ambience.Play();
        if (!entering) return;
        var sound = new AudioStreamPlayer { Stream = GD.Load<AudioStream>("res://FrierenMod/audio/divinity_enter.ogg"), Bus = ambience.Bus };
        _aura.AddChild(sound); sound.Finished += sound.QueueFree; sound.Play();
        if (LocalContext.IsMe(_owner)) {
            FrierenScreenFlashEffect.Play(new Color(0.8f, 0.3f, 1f));
            NGame.Instance?.ScreenShake(ShakeStrength.Strong, ShakeDuration.Short);
        }
    }

    private void StopAura()
    {
        if (!IsActive) return;
        _aura!.QueueFree(); _aura = null;
        if (GodotObject.IsInstanceValid(_body)) _body.Modulate = _originalTint;
    }

    public override void _ExitTree()
    {
        _owner.PowerApplied -= OnApplied;
        _owner.PowerRemoved -= OnRemoved;
        CombatManager.Instance.CombatEnded -= OnCombatEnded;
        StopAura();
    }
}

[HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
public static class DivinityPresentationSetup
{
    [HarmonyPostfix]
    private static void Attach(NCreature __instance)
    {
        if (!NonInteractiveMode.IsActive && __instance.Entity.Player?.Character is Frieren)
            DivinityPresentation.Attach(__instance.Entity, __instance.Visuals,
                __instance.Visuals.GetNodeOrNull<Node2D>("Visuals/Body") ?? __instance.Body);
    }
}

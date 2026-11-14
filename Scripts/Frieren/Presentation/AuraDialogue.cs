using System.Runtime.CompilerServices;
using BaseLib.Audio;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Monsters;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.TestSupport;

namespace FrierenMod.Scripts.Presentation;

public enum AuraDialogueCue { Opening, Success, Failure, Transition, Taunt, Defeat }

public sealed record AuraVoiceLine(string Id, bool IsFrieren = false)
{
    public string Path => "res://FrierenMod/audio/aura/" + Id + ".ogg";
    public LocString Caption => new("monsters", "FRIERENMOD-AURA." + (Id == "aura_taunt" ? "taunt" : "voice." + Id));
}

public static class AuraDialogue
{
    private static readonly ConditionalWeakTable<Aura, Session> Sessions = new();
    private static readonly IReadOnlyList<AuraVoiceLine>[] Sequences = [
        [new("aura_opening"), new("frieren_opening", true)],
        [new("aura_success")], [new("aura_failure")], [new("aura_transition")], [new("aura_taunt")],
        [new("aura_death"), new("frieren_victory", true)]
    ];
    public static IReadOnlyList<AuraVoiceLine> Lines(AuraDialogueCue cue) => Sequences[(int)cue];
    public static IEnumerable<string> AssetPaths => Sequences.SelectMany(s => s).Select(s => s.Path).Concat(NSpeechBubbleVfx.AssetPaths);

    // 只驱动本机表现，不等待语音、不创建联机动作、不参与战斗随机数或状态结算。
    public static void Play(Aura aura, AuraDialogueCue cue)
    {
        if (TestMode.IsOn || NonInteractiveMode.IsActive || NCombatRoom.Instance is not { } room) return;
        if (!Sessions.TryGetValue(aura, out var session)) {
            session = new Session(aura, room, room.CombatVfxContainer); Sessions.Add(aura, session);
        }
        session.Enqueue(cue);
    }

    private sealed class Session
    {
        private sealed record Pending(AuraVoiceLine Line, NSpeechBubbleVfx? Bubble, double Duration);
        private readonly Aura _aura;
        private readonly Node _speechHost;
        private readonly Node _root = new() { Name = "FrierenAuraDialogue" };
        private readonly Queue<Pending> _pending = new();
        private readonly TaskCompletionSource _closed = new();
        private TaskCompletionSource? _interrupt;
        private AudioStreamPlayer? _sound;
        private NSpeechBubbleVfx? _bubble;
        private bool _draining, _opened, _defeated, _stopped;

        public Session(Aura aura, Node room, Node speechHost)
        {
            _aura = aura; _speechHost = speechHost;
            _root.TreeExiting += () => {
                _stopped = true; ClearPending(); _closed.TrySetResult(); StopActive();
            };
            room.AddChildSafely(_root);
        }

        public void Enqueue(AuraDialogueCue cue)
        {
            if (_stopped || _defeated || cue == AuraDialogueCue.Opening && _opened) return;
            if (cue == AuraDialogueCue.Opening) _opened = true;
            if (cue == AuraDialogueCue.Defeat) {
                _defeated = true; ClearPending(); _interrupt?.TrySetResult();
            }
            foreach (var line in Lines(cue)) {
                var speaker = line.IsFrieren ? FrierenSpeaker() : _aura.Creature;
                if (speaker == null) continue;
                var stream = GD.Load<AudioStream>(line.Path);
                double duration = Math.Max(2.2, (stream?.GetLength() ?? 2) + .15);
                // 在入队时用原生工厂捕获说话位置，死亡后仍可在阿乌拉原来的位置显示气泡。
                var bubble = NSpeechBubbleVfx.Create(line.Caption.GetFormattedText(), speaker, duration, VfxColor.White);
                _pending.Enqueue(new(line, bubble, duration));
            }
            if (_draining || _pending.Count == 0) return;
            _draining = true; TaskHelper.RunSafely(Drain());
        }

        private Creature? FrierenSpeaker()
        {
            var players = _aura.CombatState.Players.Where(p => p.Creature.IsAlive && p.Character is Frieren);
            return (players.FirstOrDefault(LocalContext.IsMe) ?? players.FirstOrDefault())?.Creature;
        }

        private async Task Drain()
        {
            try {
                if (!_root.IsInsideTree()) await _root.ToSignal(_root, Node.SignalName.TreeEntered);
                while (!_stopped && _pending.TryDequeue(out var next)) {
                    _interrupt = new();
                    _bubble = next.Bubble;
                    if (_bubble != null) _speechHost.AddChildSafely(_bubble);
                    var ended = new TaskCompletionSource();
                    var sound = ModAudio.PlaySound(next.Line.Path, targetNode: _root);
                    _sound = sound;
                    void SoundExited() { if (ReferenceEquals(_sound, sound)) _sound = null; ended.TrySetResult(); }
                    if (sound != null) sound.TreeExited += SoundExited;
                    else ended.TrySetResult();
                    try {
                        var timer = _root.GetTree().CreateTimer(next.Duration);
                        async Task Hold() => await _root.ToSignal(timer, SceneTreeTimer.SignalName.Timeout);
                        await Task.WhenAny(Task.WhenAll(ended.Task, Hold()), _interrupt.Task, _closed.Task);
                    }
                    finally {
                        if (GodotObject.IsInstanceValid(sound)) sound!.TreeExited -= SoundExited;
                        StopActive();
                    }
                }
            }
            finally { _draining = false; }
        }

        private void StopActive()
        {
            var sound = _sound; _sound = null;
            if (GodotObject.IsInstanceValid(sound) && sound!.GetParent() == _root) {
                sound.Stop();
                if (!_stopped) _root.RemoveChild(sound); // 归还 BaseLib 音频池，不释放池中的播放器。
            }
            if (GodotObject.IsInstanceValid(_bubble)) _bubble!.QueueFree();
            _bubble = null;
        }

        private void ClearPending()
        {
            while (_pending.TryDequeue(out var next)) next.Bubble?.Free();
        }
    }

    [HarmonyPatch(typeof(SfxCmd), nameof(SfxCmd.PlayDeath), [typeof(MonsterModel)])]
    private static class AuraDeathSound
    {
        private static bool Prefix(MonsterModel? monster) => monster is not Aura;
    }
}

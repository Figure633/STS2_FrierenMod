using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Encounters;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace FrierenMod.Scripts.Presentation;

// 音乐属于当前跑局节点。离开跑局后播放器随节点销毁，不修改 FMOD 全局总线。
public static class FrierenMusic
{
    public const string Folder = "res://FrierenMod/audio/";
    private static readonly ConditionalWeakTable<NRunMusicController, MusicSession> Sessions = new();

    public static string ResolveTrack(int actIndex, RoomType room, bool fighting, uint seed)
    {
        int act = Math.Clamp(actIndex + 1, 1, 3);
        if (fighting && room == RoomType.Boss)
            return Folder + (act == 3 ? "STS_Boss3_NewMix_v1.ogg" : $"STS_Boss{act}_NewMix_v1.mp3");
        if (fighting && room == RoomType.Elite) return Folder + "STS_EliteBoss_NewMix_v1.ogg";
        if (room == RoomType.Shop) return Folder + "STS_Merchant_NewMix_v1.ogg";
        if (room is RoomType.RestSite or RoomType.Event) return Folder + "STS_Shrine_NewMix_v1.ogg";
        // 固定种子选版本，不消耗战斗或奖励 RNG，读档也不会突然换曲。
        bool alternate = ((seed + (uint)actIndex) & 1) != 0;
        return Folder + (alternate ? $"STS_Level{act}-2_v2.mp3" : act == 3 ? "STS_Level3_v2.mp3" : $"STS_Level{act}_NewMix_v1.mp3");
    }

    // 只展示从原始文件标签确认的曲名；不把文件名或推测名称当作曲名。
    public static string? TrackTitle(string path) => System.IO.Path.GetFileName(path) switch {
        "Frieren_The_Slayer.mp3" => "Frieren the Slayer",
        "Frieren_DeathStinger.mp3" => "In Times of Peace",
        "STS_Boss2_NewMix_v1.mp3" => "Zoltraak",
        "STS_EliteBoss_NewMix_v1.ogg" => "Knife to the Throat",
        "STS_Merchant_NewMix_v1.ogg" => "Grassy Turtles and Seed Rats",
        "STS_Shrine_NewMix_v1.ogg" => "Phantoms of the Dead",
        _ => null
    };

    public static bool Applies(IRunState run) => LocalContext.NetId is { } id &&
        run.Players.Any(p => p.NetId == id && p.Character is Frieren);

    // 涅奥的主题通过 SfxCmd 循环播放，独立于跑局音乐控制器，stop_music 无法停止它。
    public static bool SuppressOpeningTheme(IRunState? run, string path) =>
        path == "event:/sfx/ambience/act1_neow" && run != null && Applies(run) &&
        PresentationSettings.OriginalMusic && run.CurrentRoom?.IsVictoryRoom != true &&
        ResourceLoader.Exists(ResolveTrack(run.CurrentActIndex, RoomType.Event, false, run.Rng.Seed));

    public static void Refresh(NRunMusicController controller, IRunState run, bool nativeMayHaveStarted = true)
    {
        if (!Applies(run)) return;
        var session = Sessions.GetValue(controller, c => new MusicSession(c, run));
        if (!PresentationSettings.OriginalMusic || run.CurrentRoom?.IsVictoryRoom == true)
        {
            if (session.CurrentTrack != null)
            {
                session.Stop();
                // 清除原生当前曲目缓存，允许关闭开关后原曲立即恢复。
                controller.StopMusic();
                controller.UpdateMusic();
                if (run.CurrentRoom != null) controller.UpdateTrack();
            }
            return;
        }
        var path = ResolveTrack(run.CurrentActIndex, run.CurrentRoom?.RoomType ?? RoomType.Unassigned,
            CombatManager.Instance.IsInProgress, run.Rng.Seed);
        if (CombatManager.Instance.IsInProgress && run.CurrentRoom is CombatRoom { Encounter: SpiegelFrierenEncounter })
            path = Folder + "Frieren_The_Slayer.mp3";
        if (!ResourceLoader.Exists(path)) return; // 缺失资源时保留二代音乐。
        bool wasStopped = session.CurrentTrack == null;
        session.Play(path);
        if (nativeMayHaveStarted || wasStopped)
            controller.GetNodeOrNull<Node>("Proxy")?.Call("stop_music");
    }

    public static void Stop(NRunMusicController controller)
    {
        if (Sessions.TryGetValue(controller, out var session)) session.Stop();
    }

    public sealed class MusicSession
    {
        private readonly Node _root;
        private readonly AudioStreamPlayer[] _players;
        private readonly float[] _gains = [0, 0];
        private Tween? _fade;
        private Tween? _noticeFade;
        private Label? _notice;
        private readonly bool _announce;
        private int _active;
        private bool _lastEnabled = PresentationSettings.OriginalMusic;
        public string? CurrentTrack { get; private set; }

        public MusicSession(Node root, IRunState? run = null)
        {
            _root = root;
            _announce = run != null;
            _players = [new() { Name = "FrierenMusicA", Bus = "Master" }, new() { Name = "FrierenMusicB", Bus = "Master" }];
            foreach (var player in _players) root.AddChild(player);
            var timer = new Godot.Timer { Name = "FrierenMusicVolume", WaitTime = 0.2, Autostart = true };
            timer.Timeout += () =>
            {
                UpdateVolume();
                if (run != null && root is NRunMusicController controller && _lastEnabled != PresentationSettings.OriginalMusic)
                {
                    _lastEnabled = PresentationSettings.OriginalMusic;
                    Refresh(controller, run, false);
                }
                else if (run != null && root is NRunMusicController activeController)
                    Refresh(activeController, run, false);
                else if (run == null && !PresentationSettings.OriginalMusic) Stop();
            };
            root.AddChild(timer);
        }

        public void Play(string path, bool loop = true)
        {
            if (path == CurrentTrack && _players[_active].Playing) return;
            bool changed = path != CurrentTrack;
            var stream = GD.Load<AudioStream>(path)?.Duplicate() as AudioStream;
            if (stream == null) return;
            if (stream is AudioStreamMP3 mp3) mp3.Loop = loop;
            else if (stream is AudioStreamOggVorbis ogg) ogg.Loop = loop;
            _fade?.Kill();
            int old = _active;
            _active = 1 - _active;
            var next = _players[_active];
            next.Stop();
            next.Stream = stream;
            _gains[_active] = 0;
            float from = _gains[old];
            UpdateVolume();
            next.Play();
            CurrentTrack = path;
            if (changed && _announce) Announce(path);
            _fade = _root.CreateTween();
            _fade.TweenMethod(Callable.From<float>(t =>
            {
                _gains[_active] = t;
                _gains[old] = from * (1-t);
                UpdateVolume();
            }), 0f, 1f, 0.45);
            _fade.TweenCallback(Callable.From(() => { _players[old].Stop(); _players[old].Stream = null; }));
        }

        private void Announce(string path)
        {
            _noticeFade?.Kill();
            if (TrackTitle(path) is not { } title) { _notice?.Hide(); return; }
            if (_notice == null) {
                var layer = new CanvasLayer { Name = "FrierenNowPlaying", Layer = 5 };
                _root.AddChild(layer);
                _notice = new Label { Position = new Vector2(24, 112), MouseFilter = Control.MouseFilterEnum.Ignore };
                _notice.AddThemeFontSizeOverride("font_size", 20);
                _notice.AddThemeColorOverride("font_color", new Color("e1d5bd"));
                _notice.AddThemeColorOverride("font_outline_color", Colors.Black);
                _notice.AddThemeConstantOverride("outline_size", 6);
                layer.AddChild(_notice);
            }
            _notice.Text = "♫  " + title; _notice.Modulate = Colors.Transparent; _notice.Show();
            _noticeFade = _root.CreateTween();
            _noticeFade.TweenProperty(_notice, "modulate:a", 1f, 0.3);
            _noticeFade.TweenInterval(3);
            _noticeFade.TweenProperty(_notice, "modulate:a", 0f, 0.8);
        }

        public void UpdateVolume()
        {
            // Master 总线由游戏管理；这里只应用 BGM 滑块。零音量不中断进度。
            float volume = Mathf.Pow(Math.Clamp(SaveManager.Instance.SettingsSave.VolumeBgm, 0, 1), 2);
            for (int i = 0; i < 2; i++) _players[i].VolumeLinear = _gains[i] * volume;
        }

        public void Stop()
        {
            _noticeFade?.Kill(); _notice?.Hide();
            _fade?.Kill();
            foreach (var player in _players) { player.Stop(); player.Stream = null; }
            _gains[0] = _gains[1] = 0;
            CurrentTrack = null;
        }
    }
}

[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Commands.SfxCmd), nameof(MegaCrit.Sts2.Core.Commands.SfxCmd.PlayLoop), [typeof(string), typeof(bool)])]
public static class FrierenOpeningThemePatch
{
    [HarmonyPrefix]
    public static bool Prefix(string sfx) => !FrierenMusic.SuppressOpeningTheme(RunManager.Instance.DebugOnlyGetState(), sfx);
}

[HarmonyPatch]
public static class FrierenMusicPatches
{
    public static IEnumerable<System.Reflection.MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(NRunMusicController), nameof(NRunMusicController.UpdateMusic));
        yield return AccessTools.Method(typeof(NRunMusicController), nameof(NRunMusicController.UpdateTrack), Type.EmptyTypes);
        yield return AccessTools.Method(typeof(NRunMusicController), nameof(NRunMusicController.PlayCustomMusic));
        yield return AccessTools.Method(typeof(NRunMusicController), nameof(NRunMusicController.StopCustomMusic));
    }

    [HarmonyPostfix]
    public static void Refresh(NRunMusicController __instance, IRunState ____runState) => FrierenMusic.Refresh(__instance, ____runState);
}

[HarmonyPatch(typeof(NRunMusicController), nameof(NRunMusicController.StopMusic))]
public static class FrierenMusicStopPatch
{
    [HarmonyPostfix]
    public static void Stop(NRunMusicController __instance) => FrierenMusic.Stop(__instance);
}

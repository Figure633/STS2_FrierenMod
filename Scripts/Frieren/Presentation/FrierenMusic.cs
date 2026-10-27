using System.Runtime.CompilerServices;
using FrierenMod.Core;
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

public static class FrierenMusic
{
    public const string Folder = "res://FrierenMod/audio/";
    private static readonly ConditionalWeakTable<NRunMusicController, MusicSession> Sessions = new();

    // 无状态预览/资源检查入口；正式播放由会话维护上一首及事件实例。
    public static string ResolveTrack(int actIndex, RoomType room, bool fighting, uint seed)
    {
        var candidates=MusicCatalog.For(MusicCatalog.Resolve(room,fighting));
        return candidates[(int)(seed%(uint)candidates.Count)].Path;
    }
    public static string? TrackTitle(string path) => MusicCatalog.All.FirstOrDefault(t=>t.Path==path)?.Title ??
        (System.IO.Path.GetFileName(path) switch {
            "STS_Merchant_NewMix_v1.ogg" => "Grassy Turtles and Seed Rats",
            "Frieren_The_Slayer.mp3" => "Frieren the Slayer",
            "Frieren_DeathStinger.mp3" => "In Times of Peace",
            _ => null
        });
    public static bool Applies(IRunState run) => LocalContext.NetId is { } id &&
        run.Players.Any(p=>p.NetId==id && p.Character is Frieren);
    public static bool SuppressOpeningTheme(IRunState? run,string path) =>
        path=="event:/sfx/ambience/act1_neow" && run!=null && Applies(run) &&
        PresentationSettings.OriginalMusic && run.CurrentRoom?.IsVictoryRoom!=true &&
        ResourceLoader.Exists(ResolveTrack(run.CurrentActIndex,RoomType.Event,false,run.Rng.Seed));

    public static void Refresh(NRunMusicController controller,IRunState run,bool nativeMayHaveStarted=true)
    {
        if (!Applies(run)) return;
        var session=Sessions.GetValue(controller,c=>new MusicSession(c,run));
        // 结算页有独立的单次战败曲，跑局定时器不能再次覆盖它。
        if (run.Players.All(p=>p.Creature.IsDead)) { session.Stop(); return; }
        if (!PresentationSettings.OriginalMusic || run.CurrentRoom?.IsVictoryRoom==true) {
            if (session.CurrentTrack!=null) {
                session.Stop();controller.StopMusic();controller.UpdateMusic();
                if (run.CurrentRoom!=null) controller.UpdateTrack();
            }
            return;
        }
        var pool=MusicCatalog.Resolve(run.CurrentRoom?.RoomType??RoomType.Unassigned,
            CombatManager.Instance.IsInProgress,run.CurrentRoom is CombatRoom {Encounter: SpiegelFrierenEncounter});
        bool wasStopped=session.CurrentTrack==null;
        session.FollowContext(pool,run.CurrentRoom,nativeMayHaveStarted);
        if (session.CurrentTrack!=null && (nativeMayHaveStarted || wasStopped))
            controller.GetNodeOrNull<Node>("Proxy")?.Call("stop_music");
    }
    public static void Stop(NRunMusicController controller)
    {
        if (Sessions.TryGetValue(controller,out var session)) session.Stop();
    }

    public sealed class MusicSession
    {
        private readonly Node _root;
        private readonly AudioStreamPlayer[] _players;
        private readonly float[] _gains=[0,0];
        private readonly MusicPlaylist _playlist;
        private Tween? _fade,_noticeFade;
        private UI.NowPlayingNotice? _notice;
        private readonly bool _announce;
        private int _active;
        private bool _loop,_suspended;
        private bool _lastEnabled=PresentationSettings.OriginalMusic;
        private object? _room;
        public string? CurrentTrack {get;private set;}
        public MusicPool? CurrentPool {get;private set;}
        public MusicSession(Node root,IRunState? run=null)
        {
            _root=root;_announce=run!=null;_playlist=new MusicPlaylist(run?.Rng.Seed??0);
            _players=[new(){Name="FrierenMusicA",Bus="Master"},new(){Name="FrierenMusicB",Bus="Master"}];
            for (int i=0;i<_players.Length;i++) {
                int index=i;_players[i].Finished+=()=>Finished(index);root.AddChild(_players[i]);
            }
            var timer=new Godot.Timer {Name="FrierenMusicVolume",WaitTime=.2,Autostart=true};
            timer.Timeout+=()=> {
                UpdateVolume();
                bool changed=_lastEnabled!=PresentationSettings.OriginalMusic;
                _lastEnabled=PresentationSettings.OriginalMusic;
                if(run!=null && root is NRunMusicController controller) Refresh(controller,run,changed);
                else if(!PresentationSettings.OriginalMusic) Stop();
            };
            root.AddChild(timer);
        }
        public void FollowContext(MusicPool pool,object? room,bool resume=false)
        {
            bool newRoom=!ReferenceEquals(_room,room);
            if(_suspended && !resume && !newRoom) return;
            bool change=CurrentPool!=pool || pool==MusicPool.Event && newRoom || CurrentTrack==null;
            _room=room;_suspended=false;
            if(!change) return;
            CurrentPool=pool;NextTrack();
        }
        private void NextTrack()
        {
            if(CurrentPool is not {} pool) return;
            var tracks=MusicCatalog.For(pool);
            Play(tracks[_playlist.Next(pool.ToString(),tracks.Count)].Path,MusicCatalog.Loops(pool));
        }
        private void Finished(int player)
        {
            if(player!=_active || _loop || CurrentTrack==null || _suspended) return;
            if(CurrentPool is MusicPool.Regular or MusicPool.Event or MusicPool.Boss) NextTrack();
        }
        public void Play(string path,bool loop=true)
        {
            if(path==CurrentTrack && _players[_active].Playing) return;
            var stream=GD.Load<AudioStream>(path)?.Duplicate() as AudioStream;
            if(stream==null) return;
            if(stream is AudioStreamMP3 mp3) mp3.Loop=loop;
            else if(stream is AudioStreamOggVorbis ogg) ogg.Loop=loop;
            bool changed=path!=CurrentTrack;_loop=loop;_suspended=false;
            _fade?.Kill();int old=_active;_active=1-_active;
            var next=_players[_active];next.Stop();next.Stream=stream;_gains[_active]=0;
            float from=_gains[old];UpdateVolume();next.Play();CurrentTrack=path;
            if(changed && _announce) Announce(path);
            _fade=_root.CreateTween();
            _fade.TweenMethod(Callable.From<float>(t=>{_gains[_active]=t;_gains[old]=from*(1-t);UpdateVolume();}),0f,1f,.45);
            _fade.TweenCallback(Callable.From(()=>{_players[old].Stop();_players[old].Stream=null;}));
        }
        private void Announce(string path)
        {
            _noticeFade?.Kill();
            if(TrackTitle(path) is not {} title){_notice?.Hide();return;}
            if(_notice==null){
                var layer=new CanvasLayer {Name="FrierenNowPlaying",Layer=5};_root.AddChild(layer);
                _notice=new UI.NowPlayingNotice();layer.AddChild(_notice);
            }
            _notice.SetTitle(title);_notice.Modulate=Colors.Transparent;_notice.Show();
            _noticeFade=_root.CreateTween();_noticeFade.TweenProperty(_notice,"modulate:a",1f,.3);
            _noticeFade.TweenInterval(3);_noticeFade.TweenProperty(_notice,"modulate:a",0f,.8);
        }
        public void UpdateVolume()
        {
            float volume=Mathf.Pow(Math.Clamp(SaveManager.Instance.SettingsSave.VolumeBgm,0,1),2);
            for(int i=0;i<2;i++)_players[i].VolumeLinear=_gains[i]*volume;
        }
        public void Stop()
        {
            _noticeFade?.Kill();_notice?.Hide();_fade?.Kill();
            foreach(var player in _players){player.Stop();player.Stream=null;}
            _gains[0]=_gains[1]=0;CurrentTrack=null;CurrentPool=null;_suspended=true;
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

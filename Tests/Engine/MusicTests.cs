using System.Runtime.CompilerServices;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Encounters;
using FrierenMod.Scripts.Presentation;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Unlocks;

public static class MusicTests
{
    private static bool Interactive(ref bool __result){__result=false;return false;}
    public static async Task<int> Run(Node root)
    {
        int count=0;
        void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;GD.Print("PASS 音乐："+name);}
        async Task Wait(double seconds)=>await root.ToSignal(root.GetTree().CreateTimer(seconds),SceneTreeTimer.SignalName.Timeout);
        SaveManager.Instance.InitPrefsDataForTest();
        var player=Player.CreateForNewRun<Frieren>(UnlockState.all,1);
        var run=RunState.CreateForTest([player],seed:"MUSIC092");
        RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService());LocalContext.NetId=1;
        RunManager.Instance.GenerateRooms();PresentationSettings.OriginalMusic=true;
        Check(MusicCatalog.All.Count==20 && MusicCatalog.All.Select(t=>t.Id).Distinct().Count()==20 && MusicCatalog.All.Select(t=>t.Title).Distinct().Count()==20,"20首独立曲目，没有重复条目");
        foreach(var (pool,expected) in new[]{(MusicPool.Event,6),(MusicPool.Regular,5),(MusicPool.Boss,6),(MusicPool.Spiegel,1),(MusicPool.Merchant,1),(MusicPool.Defeat,1)})
            Check(MusicCatalog.For(pool).Count==expected,pool+"曲池数量");
        foreach(var track in MusicCatalog.All){
            var stream=GD.Load<AudioStreamOggVorbis>(track.Path);
            Check(stream.GetLength()>20 && stream.InstantiatePlayback()!=null && FrierenMusic.TrackTitle(track.Path)==track.Title,"实际解码及弹窗曲名："+track.Title);
        }
        Check(MusicCatalog.Resolve(RoomType.Elite,true)==MusicPool.Boss && MusicCatalog.Resolve(RoomType.Boss,true)==MusicPool.Boss,"Boss与精英共享同一池");
        for(int act=1;act<3;act++)Check(FrierenMusic.ResolveTrack(act,RoomType.Monster,true,2)==FrierenMusic.ResolveTrack(0,RoomType.Monster,true,2),"第"+(act+1)+"幕共用常规池");
        var musicRoot=new Node();root.AddChild(musicRoot);
        var session=new FrierenMusic.MusicSession(musicRoot,run);
        var players=musicRoot.GetChildren().OfType<AudioStreamPlayer>().ToArray();
        AudioStreamPlayer Active()=>players.Single(p=>p.Playing);
        void FinishSoon(){var active=Active();active.Seek((float)(active.Stream.GetLength()-.06));}
        session.FollowContext(MusicPool.Regular,new object());await Wait(.6);
        string first=session.CurrentTrack!;
        Check(Active().Stream is AudioStreamOggVorbis {Loop:false},"常规曲不循环");
        FinishSoon();await Wait(.8);
        Check(session.CurrentTrack!=first && Active().Playing,"真实Finished信号自动换下一首，且不重复");
        first=session.CurrentTrack!;session.FollowContext(MusicPool.Regular,new object());
        Check(session.CurrentTrack==first,"普通房间切换保持常规播放，不每次重播");
        object eventRoom=new();session.FollowContext(MusicPool.Event,eventRoom);await Wait(.6);first=session.CurrentTrack!;
        session.FollowContext(MusicPool.Event,eventRoom);Check(session.CurrentTrack==first,"同一事件翻页保持曲目");
        session.FollowContext(MusicPool.Event,new object());await Wait(.6);
        Check(session.CurrentTrack!=first,"新事件重新随机且排除上次事件曲");
        first=session.CurrentTrack!;FinishSoon();await Wait(.8);
        Check(session.CurrentTrack!=first && musicRoot.GetNode<CanvasLayer>("FrierenNowPlaying").GetChild<Control>(0).Visible,"事件曲播完接下一首，并显示唱片提示");
        session.FollowContext(MusicPool.Spiegel,new object());await Wait(.6);first=session.CurrentTrack!;
        Check(Active().Stream is AudioStreamOggVorbis {Loop:true},"复制体专属曲循环");FinishSoon();await Wait(.3);
        Check(session.CurrentTrack==first && Active().Playing,"复制体曲到末尾继续同曲");
        session.FollowContext(MusicPool.Boss,new object());await Wait(.6);
        Check(session.CurrentTrack!=first && Active().Stream is AudioStreamOggVorbis {Loop:false},"第二个普通Boss恢复共享Boss池且不循环");
        session.FollowContext(MusicPool.Merchant,new object());await Wait(.6);
        Check(FrierenMusic.TrackTitle(session.CurrentTrack!)=="Sleepy Frieren" && Active().Stream is AudioStreamOggVorbis {Loop:true},"商人使用一代v2 Sleepy Frieren");
        var volume=SaveManager.Instance.SettingsSave.VolumeBgm;SaveManager.Instance.SettingsSave.VolumeBgm=0;session.UpdateVolume();
        Check(players.All(p=>p.VolumeLinear<.0001f),"统一曲目仍服从游戏音乐音量与静音");
        SaveManager.Instance.SettingsSave.VolumeBgm=volume;session.Stop();
        session.FollowContext(MusicPool.Defeat,new object(),true);await Wait(.6);FinishSoon();await Wait(.3);
        Check(!players.Any(p=>p.Playing),"战败结算单次播放，不自动接其他曲");
        session.Stop();musicRoot.QueueFree();await Wait(.1);
        // 真实原生控制器路由，保存系统仍为TestMode内存实现。
        var patch=new Harmony("frieren.tests.music092");
        var nonInteractive=AccessTools.PropertyGetter(typeof(MegaCrit.Sts2.Core.Helpers.NonInteractiveMode),"IsActive");
        patch.Patch(nonInteractive,prefix:new HarmonyMethod(typeof(MusicTests),nameof(Interactive)));
        run.PushRoom(new RestSiteRoom());
        var controller=new NRunMusicController();var proxy=new Node {Name="Proxy"};proxy.SetScript(GD.Load<Script>("res://music_proxy.gd"));
        controller.AddChild(proxy);controller.SetRunState(run);root.AddChild(controller);
        controller.UpdateMusic();await Wait(.6);
        var table=(ConditionalWeakTable<NRunMusicController,FrierenMusic.MusicSession>)AccessTools.Field(typeof(FrierenMusic),"Sessions").GetValue(null)!;
        table.TryGetValue(controller,out var routed);
        Check(routed!.CurrentPool==MusicPool.Event && proxy.Get("stopped").AsInt32()>0,"真实控制器停止原生叠音并路由事件池");
        controller.StopMusic();await Wait(.4);Check(routed.CurrentTrack==null,"主动StopMusic后定时器不会重启同房间音乐");
        controller.UpdateMusic();await Wait(.6);Check(routed.CurrentTrack!=null,"原生更新音乐时恢复模组播放");
        PresentationSettings.OriginalMusic=false;await Wait(.4);
        Check(routed.CurrentTrack==null && proxy.Get("started").AsInt32()>1,"关闭模组音乐立即恢复原生");
        PresentationSettings.OriginalMusic=true;await Wait(.6);Check(routed.CurrentTrack!=null,"重新开启音乐恢复曲池");
        var boss1=new CombatRoom(ModelDb.Encounter<SpiegelFrierenEncounter>().ToMutable(),run);run.PushRoom(boss1);
        AccessTools.Property(typeof(CombatManager),nameof(CombatManager.IsInProgress)).SetValue(CombatManager.Instance,true);
        controller.UpdateMusic();await Wait(.6);Check(routed.CurrentPool==MusicPool.Spiegel,"实际复制体房间使用专属曲");
        run.PopCurrentRoom();run.PushRoom(new CombatRoom(ModelDb.Encounter<QueenBoss>().ToMutable(),run));
        controller.StopMusic();controller.UpdateMusic();await Wait(.6);
        Check(routed.CurrentPool==MusicPool.Boss && routed.CurrentTrack!=MusicCatalog.For(MusicPool.Spiegel)[0].Path,"第二Boss切场后有音乐且不沿用复制体曲");
        player.Creature.SetCurrentHpInternal(0);FrierenMusic.Refresh(controller,run);await Wait(.4);
        Check(routed.CurrentTrack==null,"玩家死亡后跑局定时器不覆盖战败结算曲");
        player.Creature.SetCurrentHpInternal(player.Creature.MaxHp);
        PresentationSettings.OriginalMusic=false;controller.StopMusic();controller.QueueFree();await Wait(.1);
        patch.UnpatchAll(patch.Id);PresentationSettings.OriginalMusic=true;
        AccessTools.Property(typeof(CombatManager),nameof(CombatManager.IsInProgress)).SetValue(CombatManager.Instance,false);
        run.PopCurrentRoom();run.PopCurrentRoom();RunManager.Instance.CleanUp();
        return count;
    }
}

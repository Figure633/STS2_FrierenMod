using System.Reflection;
using BaseLib.Audio;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Presentation;
using FrierenMod.Scripts.Relics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Unlocks;

public static class CardRelicSoundTests
{
    private static readonly List<string> Played = [];
    private static bool Capture(ModSound sound) { Played.Add(Path.GetFileName(sound.File)); return false; }
    public static async Task<int> Run()
    {
        int checks=0;
        void Check(bool ok,string text){if(!ok)throw new Exception(text);checks++;GD.Print("PASS 卡牌遗物音效："+text);}
        Check(!PresentationSettings.CardRelicSounds,"默认不开启音效");
        var harmony=new Harmony("frieren.tests.card-relic-sounds");
        harmony.Patch(AccessTools.Method(typeof(ModAudio),nameof(ModAudio.PlaySoundGlobal)),prefix:new HarmonyMethod(typeof(CardRelicSoundTests),nameof(Capture)));
        SaveManager.Instance.InitPrefsDataForTest();
        var player=Player.CreateForNewRun<Frieren>(UnlockState.all,1);
        var run=RunState.CreateForTest([player],seed:"CARD-RELIC-SOUNDS");
        RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService());
        var combat=new CombatState(runState:run);combat.AddPlayer(player);
        var enemy=combat.CreateCreature(ModelDb.Monster<Axebot>().ToMutable(),CombatSide.Enemy,null);combat.AddCreature(enemy);
        CombatManager.Instance.SetUpCombat(combat);CombatTestState.SetInProgress(true);
        var context=new ThrowingPlayerChoiceContext();
        async Task Play(CardModel card){
            var play=CombatTestState.NewPlay(card,enemy,PileType.Discard,new ResourceInfo{EnergySpent=0,EnergyValue=0,StarsSpent=0,StarValue=0},false,0,1);
            await (Task)AccessTools.Method(card.GetType(),"OnPlay").Invoke(card,[context,play])!;
        }
        try {
            foreach(bool enabled in new[]{false,true}){
                PresentationSettings.CardRelicSounds=enabled;Played.Clear();
                await Play(combat.CreateCard<SleepIn>(player));await Play(combat.CreateCard<Kiss>(player));await Play(combat.CreateCard<OrdinaryOffensiveMagic>(player));
                Check(Played.SequenceEqual(enabled?new[]{"BabySleeping.mp3","kiss.mp3","normal_attack.mp3"}:Array.Empty<string>()),"真实出牌仅在开关开启时播放对应三种音效 "+enabled);
                int before=Played.Count;await Play(combat.CreateCard<BabySleeping>(player));Check(Played.Count==before,"婴儿般的睡眠不播放音效 "+enabled);
                var ring=ModelDb.Relic<MirroredLotusRing>().ToMutable();ring.Owner=player;await ring.BeforeCombatStart();
                for(int i=0;i<3;i++)await ring.AfterPlayerTurnStart(context,player);
                Check(Played.Count==before,"戒指前三回合不播放 "+enabled);
                await ring.AfterPlayerTurnStart(context,player);await ring.AfterPlayerTurnStart(context,player);
                Check(Played.Count==before+(enabled?1:0)&&(!enabled||Played.Last()=="Himmel.mp3"),"戒指仅第四回合触发一次Himmel音效 "+enabled);
            }
            PresentationSettings.CardRelicSounds=false;Played.Clear();CardRelicSound.Play("kiss.mp3");Check(Played.Count==0,"关闭后立即停止后续音效");
            foreach(var file in new[]{"BabySleeping.mp3","kiss.mp3","normal_attack.mp3","Himmel.mp3"})
                Check(GD.Load<AudioStream>("res://FrierenMod/audio/"+file).GetLength()>0,"打包音频可加载："+file);
        } finally {PresentationSettings.CardRelicSounds=false;harmony.UnpatchAll(harmony.Id);CombatTestState.SetInProgress(false);RunManager.Instance.CleanUp();}
        return checks;
    }
}

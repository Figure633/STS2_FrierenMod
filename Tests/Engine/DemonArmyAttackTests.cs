using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Events;
using FrierenMod.Scripts.Mechanics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;

public static class DemonArmyAttackTests
{
    public static CombatRoom? CapturedRoom;
    // 只替代界面换房；事件进入战斗的原生分支、父事件及所选遭遇仍原样执行。
    public static bool CaptureEnterRoom(AbstractRoom room, ref Task __result)
    { CapturedRoom=(CombatRoom)room;__result=Task.CompletedTask;return false; }

    public static async Task<int> Run(Node root)
    {
        int count=0;void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;GD.Print("PASS 魔王军事件："+message);}
        SaveManager.Instance.InitPrefsDataForTest();LocalContext.NetId=1;
        var harmony=new Harmony("frieren.tests.army-event");
        var enter=AccessTools.Method(typeof(RunManager),nameof(RunManager.EnterRoomWithoutExitingCurrentRoom));
        harmony.Patch(enter,prefix:new HarmonyMethod(typeof(DemonArmyAttackTests),nameof(CaptureEnterRoom)));
        try {
            var player=Player.CreateForNewRun<Frieren>(UnlockState.all,1);
            var other=Player.CreateForNewRun<Frieren>(UnlockState.all,2);
            var run=RunState.CreateForTest([player,other],seed:"DEMON-ARMY-EVENT");
            RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService());
            var canonical=ModelDb.Event<DemonArmyAttack>();
            Check(canonical.IsShared&&ModelDb.AllSharedEvents.Contains(canonical),"注册到共享事件池并使用原生投票");
            foreach(int act in new[]{0,1,2}){
                run.CurrentActIndex=act;Check(canonical.IsAllowed(run)==(act==2),"仅第三幕出现："+act);
            }
            Check(run.MapPointHistory.All(a=>a.Count==0)&&canonical.IsAllowed(run),"无需实际击败阿乌拉");
            Check(DemonArmyAttack.BossPool.Count==3&&DemonArmyAttack.BossPool.All(e=>e.GetType().Assembly==typeof(InfestedPrismsElite).Assembly),"候选仅原版二幕三个Boss");
            foreach(string language in new[]{"zhs","eng"}){
                LocManager.Instance.SetLanguage(language);
                var story=(DemonArmyAttack)canonical.ToMutable();await CombatTestState.BeginEvent(story,player);
                var teammate=(DemonArmyAttack)canonical.ToMutable();await CombatTestState.BeginEvent(teammate,other);
                var repeated=(DemonArmyAttack)canonical.ToMutable();await CombatTestState.BeginEvent(repeated,player);
                Check(story.SelectedBoss.Id==teammate.SelectedBoss.Id&&story.SelectedBoss.Id==repeated.SelectedBoss.Id,"同局共享选择及重进事件不重掷："+language);
                Check(story.CurrentOptions.Count==3&&story.CurrentOptions[0].Description.GetFormattedText().Contains(story.SelectedBoss.Title.GetFormattedText()),"选择前显示实际Boss："+language);
                Check(story.CurrentOptions.All(o=>!o.Description.GetFormattedText().Contains("LOC_ERR")&&o.HoverTips.Any()),"双语选项及原生能力悬停完整："+language);
                Check(story.CreateInitialPortrait().GetWidth()>0,"作者配图可加载："+language);
                if(OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))await Snapshot(root,story,language,Check);
                for(int branch=0;branch<3;branch++){
                    var model=(DemonArmyAttack)canonical.ToMutable();await CombatTestState.BeginEvent(model,player);
                    CapturedRoom=null;await model.CurrentOptions[branch].Chosen();var room=CapturedRoom;
                    Check(room!=null&&room.ParentEventId==canonical.Id&&!room.ShouldResumeParentEventAfterCombat,"原生战斗入口保留父事件："+branch);
                    Check(branch switch{0=>room!.Encounter.Id==model.SelectedBoss.Id,1=>room!.Encounter is InfestedPrismsElite,_=>room!.Encounter is HunterKillerNormal},"选项与实际遭遇一致："+branch);
                }
            }
            LocManager.Instance.SetLanguage("zhs");RunManager.Instance.CleanUp();
            foreach(var encounter in DemonArmyAttack.BossPool.Concat(new EncounterModel[]{ModelDb.Encounter<InfestedPrismsElite>(),ModelDb.Encounter<HunterKillerNormal>()})){
                player=Player.CreateForNewRun<Frieren>(UnlockState.all,1);other=Player.CreateForNewRun<Frieren>(UnlockState.all,2);
                run=RunState.CreateForTest([player,other],seed:"ARMY-BATTLE-"+encounter.Id.Entry);run.CurrentActIndex=2;
                RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService());LocalContext.NetId=1;
                var room=new CombatRoom(encounter.ToMutable(),run){ParentEventId=canonical.Id,ShouldResumeParentEventAfterCombat=false};run.PushRoom(room);
                var combat=room.CombatState;combat.AddPlayer(player);combat.AddPlayer(other);
                room.Encounter.GenerateMonstersWithSlots(run);
                foreach(var (monster,slot) in room.Encounter.MonstersWithSlots)combat.AddCreature(combat.CreateCreature(monster,CombatSide.Enemy,slot));
                CombatManager.Instance.SetUpCombat(combat);CombatTestState.SetInProgress(true);
                await Hook.BeforeCombatStart(run,combat);await DemonArmyAttackCombat.Start(combat);
                foreach(var p in run.Players){
                    bool buffs=encounter.RoomType switch{
                        RoomType.Boss=>p.Creature.GetPowerAmount<BarricadePower>()==1&&p.Creature.GetPowerAmount<PlatingPower>()==8,
                        RoomType.Elite=>p.Creature.GetPowerAmount<AfterimagePower>()==1,
                        _=>p.Creature.GetPowerAmount<ArtifactPower>()==1};
                    Check(buffs,"本人及队友开战增益，重复通知不叠加："+encounter.Id.Entry+"/"+p.NetId);
                    var rewards=await RewardsCmd.GenerateForRoomEnd(p,room);
                    int potions=encounter.RoomType==RoomType.Boss?2:encounter.RoomType==RoomType.Elite?1:0;
                    int gold=encounter.RoomType==RoomType.Boss?150:50;
                    var rarity=encounter.RoomType==RoomType.Boss?RelicRarity.Rare:encounter.RoomType==RoomType.Elite?RelicRarity.None:RelicRarity.Common;
                    Check(rewards.Rewards.Count==potions+2&&!rewards.Rewards.OfType<CardReward>().Any()&&rewards.Rewards.OfType<PotionReward>().Count()==potions&&rewards.Rewards.OfType<GoldReward>().Single().Amount==gold,"原生战利品只发指定悬赏："+encounter.Id.Entry+"/"+p.NetId);
                    var relic=rewards.Rewards.OfType<RelicReward>().Single();
                    Check(relic.Rarity==rarity&&relic.Relic!=null&&(rarity==RelicRarity.None||relic.Relic.Rarity==rarity),"遗物抽取稀有度正确："+encounter.Id.Entry+"/"+p.NetId);
                    var saves=rewards.Rewards.Select(r=>r.ToSerializable()).ToArray();
                    var restored=saves.Select(s=>Reward.FromSerializable(s,p)).ToArray();
                    Check(restored.Length==potions+2&&restored.OfType<GoldReward>().Single().Amount==gold&&restored.OfType<RelicReward>().Count()==1&&restored.OfType<PotionReward>().Count()==potions,"原生奖励存档保留类型、数量及金币："+encounter.Id.Entry+"/"+p.NetId);
                }
                Check(room.RoomType!=RoomType.Boss&&new CombatRoom(encounter.ToMutable(),run).RoomType==encounter.RoomType,"仅事件Boss避开终局，原生Boss房保持原样："+encounter.Id.Entry);
                // 原版仅在事件战斗结束后保存子战斗房；战斗中保存的是父事件检查点。
                AccessTools.Field(typeof(CombatRoom),"_isPreFinished").SetValue(room,true);
                var loaded=CombatRoom.FromSerializable(room.ToSerializable(),run);
                Check(loaded.ParentEventId==canonical.Id&&loaded.Encounter.Id==encounter.Id&&loaded.RoomType==room.RoomType&&!loaded.ShouldResumeParentEventAfterCombat,"战斗存档保留遭遇、父事件及安全房间类型："+encounter.Id.Entry);
                CombatTestState.SetInProgress(false);RunManager.Instance.CleanUp();
            }
            var iron=Player.CreateForNewRun<Ironclad>(UnlockState.all,1);player=Player.CreateForNewRun<Frieren>(UnlockState.all,2);
            run=RunState.CreateForTest([iron,player],seed:"ARMY-MIXED");run.CurrentActIndex=2;
            Check(!canonical.IsAllowed(run),"混合角色队伍仍不进入芙莉莲专属事件池");
        }
        finally {harmony.UnpatchAll(harmony.Id);if(RunManager.Instance.DebugOnlyGetState()!=null)RunManager.Instance.CleanUp();}
        return count;
    }

    private static async Task Snapshot(Node root,DemonArmyAttack story,string language,Action<bool,string> check)
    {
        RunManager.Instance.EventSynchronizer.BeginEvent(ModelDb.Event<DemonArmyAttack>());
        story=(DemonArmyAttack)RunManager.Instance.EventSynchronizer.GetLocalEvent();
        if(!InputMap.HasAction(DebugHotkey.hideEventUi))InputMap.AddAction(DebugHotkey.hideEventUi);
        var host=new Control{Size=new Vector2(1920,1080)};root.AddChild(host);
        var layout=story.CreateScene().Instantiate<NEventLayout>();host.AddChild(layout);story.SetNode(layout);
        layout.SetEvent(story);layout.SetTitle(story.Title.GetFormattedText());
        var description=story.Description!;story.DynamicVars.AddTo(description);layout.SetDescription(description.GetFormattedText());
        layout.AddOptions(story.CurrentOptions);layout.OnSetupComplete();
        for(int i=0;i<5;i++)await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
        foreach(var tween in root.GetTree().GetProcessedTweens())tween.CustomStep(10);
        for(int i=0;i<3;i++)await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
        check(layout.OptionButtons.Count()==3&&layout.OptionButtons.All(b=>host.GetGlobalRect().Encloses(b.GetGlobalRect())),"全部原生选项在视口内："+language);
        root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/todos-latest/event-"+language+".png"));
        layout.ClearOptions();host.QueueFree();await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
    }
}

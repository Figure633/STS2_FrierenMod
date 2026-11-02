using BaseLib.Config.UI;
using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.UI;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.Localization;
using System.Text.Json;

public static class CommissionTests
{
    public static async Task<int> Run(Node root)
    {
        int count=0;void Check(bool ok,string label){if(!ok)throw new Exception(label);count++;GD.Print("PASS 委托："+label);}
        async Task Tick(){for(int i=0;i<8;i++)await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);}
        SaveManager.Instance.InitPrefsDataForTest();
        var player=Player.CreateForNewRun<Frieren>(UnlockState.all,1);var other=Player.CreateForNewRun<Frieren>(UnlockState.all,2);
        var run=RunState.CreateForTest([player,other],seed:"COMMISSIONS");RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService());LocalContext.NetId=1;
        run.ActFloor=9;run.PushRoom(new RestSiteRoom());CombatTestState.SetInProgress(false);
        // 夹具直接PushRoom，补上正常进入房间时的动作队列恢复（测试版也默认暂停）。
        RunManager.Instance.ActionExecutor.Unpause();
        var state=CommissionCmd.State(player);string original=state.ToJson();
        Check(state.Quests.Count==6&&CommissionCmd.State(player).ToJson()==original,"界面读取不会重刷任务和奖励");
        Check(CommissionCmd.Read(other).Quests.Count==0,"创建本人的委托不写入另一玩家存档");
        var stale=state.Revision;
        Check(await CommissionCmd.Send(player,1,CommissionOperation.Accept),"原生同步动作队列接取委托");
        Check(!await CommissionCmd.Execute(player,2,CommissionOperation.Accept,stale),"拒绝过期版本的重复输入");
        state=CommissionCmd.State(player);state.Record("test-monsters",CommissionGoal.Monsters,3);CommissionCmd.Write(player,state);
        int gold=player.Gold;
        Check(await CommissionCmd.Execute(player,1,CommissionOperation.Claim,state.Revision)&&player.Gold==gold+90,"提交委托通过原生命令发90金币");
        Check(!await CommissionCmd.Execute(player,1,CommissionOperation.Claim,CommissionCmd.State(player).Revision)&&player.Gold==gold+90,"重复提交不重复奖励");
        state=CommissionCmd.State(player);state.Accept(2,0,9);CommissionCmd.Write(player,state);var spell=state.Quests.Single(q=>q.Id==2);
        Check(!await CommissionCmd.Execute(player,2,CommissionOperation.Claim,state.Revision),"没有指定主牌组魔法不能提交");
        var card=run.CreateCard(CommissionCmd.Spell(spell.Spell),player);CardCmd.Upgrade(card);await CardPileCmd.Add(card,PileType.Deck);
        Check(CommissionCmd.OwnsSpell(player,spell.Spell),"升级版指定魔法符合要求");
        while(player.HasOpenPotionSlots)await PotionCmd.TryToProcure(CommissionCmd.Potion(spell.Reward.Item).ToMutable(),player);
        Check(!await CommissionCmd.Execute(player,2,CommissionOperation.Claim,state.Revision)&&CommissionCmd.Read(player).Quests.Single(q=>q.Id==2).Status==CommissionStatus.Accepted,"药水栏满不消耗委托或丢奖励");
        await PotionCmd.Discard(player.Potions.First());int potions=player.Potions.Count();
        Check(await CommissionCmd.Execute(player,2,CommissionOperation.Claim,CommissionCmd.State(player).Revision)&&player.Potions.Count()==potions+1&&player.Deck.Cards.Contains(card),"空出药水栏后领取固定药水，保留指定魔法");
        state=CommissionCmd.State(player);state.Accept(3,0,9);state.Record("prop",CommissionGoal.FirstTurnBattle,99);CommissionCmd.Write(player,state);
        int props=MagicDeckStore.Read(player).Props.Count;
        Check(await CommissionCmd.Execute(player,3,CommissionOperation.Claim,state.Revision)&&MagicDeckStore.Read(player).Props.Count==props+1,"魔具报酬进入魔法牌组");
        state=CommissionCmd.State(player);state.Accept(4,0,9);state.Record("elite",CommissionGoal.Elites);CommissionCmd.Write(player,state);
        var relic=state.Quests.Single(q=>q.Id==4).Reward;await RelicCmd.Obtain(CommissionCmd.Relic(relic.Item).ToMutable(),player);gold=player.Gold;
        Check(await CommissionCmd.Execute(player,4,CommissionOperation.Claim,state.Revision)&&player.Gold==gold+relic.Amount+relic.BonusGold,"已拥有固定遗物时按说明折算金币");
        var otherState=CommissionCmd.State(other);otherState.Accept(4,0,9);otherState.Record("other-elite",CommissionGoal.Elites);CommissionCmd.Write(other,otherState);
        var otherRelic=otherState.Quests.Single(q=>q.Id==4).Reward;int relicCount=other.Relics.Count;
        Check(await CommissionCmd.Execute(other,4,CommissionOperation.Claim,otherState.Revision)&&other.Relics.Count==relicCount+1&&other.Relics.Any(r=>r.Id==CommissionCmd.Relic(otherRelic.Item).Id),"尚未拥有的固定遗物通过原生命令获得");
        var json=JsonSerializer.Serialize(player.ToSerializable(),JsonSerializationUtility.Options);
        var restored=Player.FromSerializable(JsonSerializer.Deserialize<SerializablePlayer>(json,JsonSerializationUtility.Options)!);
        Check(CommissionCmd.Read(restored).ToJson()==CommissionCmd.Read(player).ToJson(),"原生玩家存读档保留委托与领取去重");
        state=CommissionCmd.State(player);
        // 固定普通讨伐夹具，兼测从旧版本保留的任务；不依赖第六项的随机模板。
        state.Quests[5]=new(){Id=6,Act=0,Template="ROAD",OpensAt=7,Goal=CommissionGoal.Monsters,Target=8,Reward=new(CommissionRewardKind.Gold,"",180)};
        state.Accept(6,0,9);state.Accept(5,0,9);state.Record("ready-before-expiry",CommissionGoal.FlawlessElite);CommissionCmd.Write(player,state);
        var encounter=ModelDb.Encounter<TurretOperatorWeak>().ToMutable();encounter.GenerateMonstersWithSlots(run);
        var room=new CombatRoom(encounter,run);run.PushRoom(room);var combat=room.CombatState;combat.AddPlayer(player);combat.AddPlayer(other);
        var initial=encounter.MonstersWithSlots[0];var enemy=combat.CreateCreature(initial.Item1,CombatSide.Enemy,initial.Item2);combat.AddCreature(enemy);
        CombatManager.Instance.SetUpCombat(combat);CombatTestState.SetInProgress(true);enemy.SetCurrentHpInternal(0);
        var context=new MegaCrit.Sts2.Core.GameActions.Multiplayer.ThrowingPlayerChoiceContext();
        await Hook.AfterDeath(run,combat,enemy,false,0);
        Check(CommissionCmd.Read(player).Quests.Single(q=>q.Id==6).Progress==1,"真实死亡钩子统计普通战斗初始敌人");
        await Hook.AfterDeath(run,combat,enemy,false,0);
        Check(CommissionCmd.Read(player).Quests.Single(q=>q.Id==6).Progress==1,"同一敌人重复死亡不重复计数");
        var summon=combat.CreateCreature(ModelDb.GetById<MonsterModel>(initial.Item1.Id).ToMutable(),CombatSide.Enemy,"summoned");combat.AddCreature(summon);summon.SetCurrentHpInternal(0);
        await Hook.AfterDeath(run,combat,summon,false,0);
        Check(CommissionCmd.Read(player).Quests.Single(q=>q.Id==6).Progress==1,"同模型召唤物也不计入讨伐");
        // 新幕使所有未领奖旧任务过期，已达标也不能留到下一幕。
        run.CurrentActIndex=1;run.ActFloor=1;CommissionCmd.Ensure(run);
        Check(CommissionCmd.Read(player).Quests.Count==12&&CommissionCmd.Read(player).Quests.Single(q=>q.Id==6).Status==CommissionStatus.Abandoned&&CommissionCmd.Read(player).ActiveCount==0,"跨幕自动放弃旧委托并释放名额");
        Check(!await CommissionCmd.Execute(player,5,CommissionOperation.Claim,CommissionCmd.State(player).Revision),"上一幕已达标未领奖的任务也不能提交");
        CombatManager.Instance.Reset(true);
        run.ActFloor=5;state=CommissionCmd.State(player);state.Accept(104,1,5);CommissionCmd.Write(player,state);
        var elite=ModelDb.AllEncounters.First(e=>e.RoomType==RoomType.Elite).ToMutable();elite.GenerateMonstersWithSlots(run);
        var eliteRoom=new CombatRoom(elite,run);eliteRoom.CombatState.AddPlayer(player);eliteRoom.CombatState.AddPlayer(other);run.PushRoom(eliteRoom);
        CombatManager.Instance.SetUpCombat(eliteRoom.CombatState);
        state=CommissionCmd.State(player);state.Accept(105,1,5);CommissionCmd.Write(player,state);
        otherState=CommissionCmd.State(other);otherState.Accept(105,1,5);CommissionCmd.Write(other,otherState);
        await Hook.BeforeCombatStart(run,eliteRoom.CombatState);
        await Hook.AfterCurrentHpChanged(run,eliteRoom.CombatState,other.Creature,-1);
        await Hook.AfterCurrentHpChanged(run,eliteRoom.CombatState,other.Creature,1);
        await Hook.AfterCombatVictory(run,eliteRoom.CombatState,eliteRoom);CommissionCmd.Victory(run,eliteRoom);
        Check(CommissionCmd.Read(player).Quests.Single(q=>q.Id==104).Progress==1,"精英只按胜利场次计数，重复通知不重复累计");
        Check(CommissionCmd.Read(player).Quests.Single(q=>q.Id==105).Progress==1&&CommissionCmd.Read(other).Quests.Single(q=>q.Id==105).Progress==0,"无伤精英按本人整场扣血判定，队友受伤不连带，回血不抹去受伤记录");
        Check(CommissionCmd.Read(player).Quests.Single(q=>q.Id==105).Reward.BonusGold>CommissionCmd.Read(player).Quests.Single(q=>q.Id==104).Reward.BonusGold,"无伤精英报酬高于普通精英讨伐");
        CombatManager.Instance.Reset(true);
        run.ActFloor=8;state=CommissionCmd.State(player);state.Accept(103,1,8);CommissionCmd.Write(player,state);
        var quickEncounter=ModelDb.Encounter<TurretOperatorWeak>().ToMutable();quickEncounter.GenerateMonstersWithSlots(run);
        var quickRoom=new CombatRoom(quickEncounter,run);run.PushRoom(quickRoom);quickRoom.CombatState.AddPlayer(player);CombatManager.Instance.SetUpCombat(quickRoom.CombatState);
        CommissionCmd.BeginCombat(quickRoom.CombatState);quickRoom.CombatState.RoundNumber=2;CommissionCmd.Victory(run,quickRoom);
        Check(CommissionCmd.Read(player).Quests.Single(q=>q.Id==103).Progress==0,"第二回合胜利不满足首回合速胜");
        run.ActFloor=9;quickRoom.CombatState.RoundNumber=1;player.PlayerCombatState!.IncrementTurnNumber();CommissionCmd.Victory(run,quickRoom);
        Check(CommissionCmd.Read(player).Quests.Single(q=>q.Id==103).Progress==0,"第1轮中的额外回合不满足速胜");
        CombatManager.Instance.Reset(true);run.ActFloor=10;
        var firstRoom=new CombatRoom(quickEncounter,run);run.PushRoom(firstRoom);firstRoom.CombatState.AddPlayer(player);CombatManager.Instance.SetUpCombat(firstRoom.CombatState);
        firstRoom.CombatState.RoundNumber=1;CommissionCmd.BeginCombat(firstRoom.CombatState);CommissionCmd.Victory(run,firstRoom);CommissionCmd.Victory(run,firstRoom);
        Check(CommissionCmd.Read(player).Quests.Single(q=>q.Id==103).Progress==1,"第1回合普通战胜利完成速胜，重复通知不重复计数");
        otherState=CommissionCmd.State(other);
        otherState.Quests[otherState.Quests.FindIndex(q=>q.Id==106)]=new(){Id=106,Act=1,Template="CAREFUL",OpensAt=7,Goal=CommissionGoal.FlawlessBattle,Target=1,Reward=new(CommissionRewardKind.Prop,"MysteryBox",2,150)};
        otherState.Accept(106,1,10);CommissionCmd.Write(other,otherState);firstRoom.CombatState.AddPlayer(other);
        CommissionCmd.BeginCombat(firstRoom.CombatState);await Hook.AfterCurrentHpChanged(run,firstRoom.CombatState,other.Creature,-1);CommissionCmd.Victory(run,firstRoom);
        Check(CommissionCmd.Read(other).Quests.Single(q=>q.Id==106).Progress==0,"受伤的普通战不能完成无伤归途");
        run.ActFloor=11;run.AppendToMapPointHistory(MegaCrit.Sts2.Core.Map.MapPointType.Monster,RoomType.Monster,firstRoom.ModelId);
        CommissionCmd.BeginCombat(firstRoom.CombatState);await Hook.AfterCurrentHpChanged(run,firstRoom.CombatState,other.Creature,0);CommissionCmd.Victory(run,firstRoom);
        Check(CommissionCmd.Read(other).Quests.Single(q=>q.Id==106).Progress==1,"下一场未损失生命的普通战可完成无伤归途");
        state=CommissionCmd.State(player);foreach(var quest in state.Quests.Where(q=>q.Status==CommissionStatus.Accepted).ToArray())state.Abandon(quest.Id);CommissionCmd.Write(player,state);
        CombatManager.Instance.Reset(true);run.PushRoom(new RestSiteRoom());CombatTestState.SetInProgress(false);
        state=CommissionCmd.State(player);
        state.Quests[state.Quests.FindIndex(q=>q.Id==106)]=new(){Id=106,Act=1,Template="ESCORT",OpensAt=7,Goal=CommissionGoal.Battles,Target=2,Reward=new(CommissionRewardKind.Prop,"MysteryBox",2,125)};
        state.Accept(106,1,10);state.Record("escort-test",CommissionGoal.Battles,2);CommissionCmd.Write(player,state);
        props=MagicDeckStore.Read(player).Props.Count;gold=player.Gold;
        Check(await CommissionCmd.Execute(player,106,CommissionOperation.Claim,state.Revision)&&MagicDeckStore.Read(player).Props.Count==props+2&&player.Gold==gold+125,"丰厚报酬实际发放两件独立魔具与额外金币");
        CombatManager.Instance.Reset(true);run.ActFloor=1;run.PushRoom(new RestSiteRoom());
        var ui=new Control{Size=new(1920,1080)};root.AddChild(ui);var screen=new CommissionScreen(ui,player);
        foreach(string language in new[]{"zhs","eng"}) {
            LocManager.Instance.SetLanguage(language);screen.Open();await Tick();
            var list=screen.View.GetNode<VBoxContainer>("CommissionCanvas/CommissionScroll/Quests");
            Check(list.GetChildren().Cast<Control>().All(row=>row.FindChild("Illustration",true,false) is TextureRect { Texture: not null }),"每项委托加载对应插图："+language);
            Check(list.GetChildCount()==6,"任务板显示本幕完整六张委托："+language);
            Check(list.FindChildren("*","Label",true,false).OfType<Label>().All(l=>!l.Text.Contains('{')),"任务栏文本参数完整："+language);
            Check(!screen.View.GetNode<Control>("CommissionCanvas/PreviousAct").GetGlobalRect().Intersects(screen.View.GetNode<Control>("CommissionCanvas/NextAct").GetGlobalRect()),"翻页按钮互不遮挡："+language);
            if(language=="zhs") {
                var accept=(NConfigButton)list.GetNode<Control>("Quest101").FindChild("Accept",true,false);
                accept.EmitSignal(NButton.SignalName.Released,accept);await Tick();
                Check(CommissionCmd.Read(player).Quests.Single(q=>q.Id==101).Status==CommissionStatus.Accepted,"任务栏按钮通过原生动作实际接取");
            }
            foreach(var size in new[]{new Vector2(1920,1080),new Vector2(1280,720),new Vector2(2560,1080)}) {
                ui.Size=size;await Tick();var canvas=screen.View.GetNode<Control>("CommissionCanvas");
                Check(new Rect2(Vector2.Zero,size).Grow(1).Encloses(canvas.GetGlobalRect()),"任务板等比适配："+language+"/"+size);
            }
            ui.Size=new(1920,1080);await Tick();
            if(OS.GetCmdlineUserArgs().Contains("--ui-snapshot")) {
                root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/commission-"+language+".png"));
                var scroll=screen.View.GetNode<ScrollContainer>("CommissionCanvas/CommissionScroll");scroll.ScrollVertical=600;await Tick();
                root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/commission-challenges-"+language+".png"));
                scroll.ScrollVertical=10000;await Tick();
                root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/commission-last-"+language+".png"));
                scroll.ScrollVertical=0;
            }
            screen.Close();await Tick();
        }
        screen.View.QueueFree();ui.QueueFree();await Tick();LocManager.Instance.SetLanguage("zhs");RunManager.Instance.CleanUp();return count;
    }
}

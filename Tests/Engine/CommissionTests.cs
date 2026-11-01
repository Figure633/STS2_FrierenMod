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
        var state=CommissionCmd.State(player);string original=state.ToJson();
        Check(state.Quests.Count==6&&CommissionCmd.State(player).ToJson()==original,"界面读取不会重刷任务和奖励");
        Check(CommissionCmd.Read(other).Quests.Count==0,"创建本人的委托不写入另一玩家存档");
        var stale=state.Revision;
        Check(await CommissionCmd.Send(player,1,CommissionOperation.Accept),"原生同步动作队列接取委托");
        Check(!await CommissionCmd.Execute(player,2,CommissionOperation.Accept,stale),"拒绝过期版本的重复输入");
        state=CommissionCmd.State(player);state.Record("test-monsters",CommissionGoal.Monsters,3);CommissionCmd.Write(player,state);
        int gold=player.Gold;
        Check(await CommissionCmd.Execute(player,1,CommissionOperation.Claim,state.Revision)&&player.Gold==gold+40,"提交委托通过原生命令发40金币");
        Check(!await CommissionCmd.Execute(player,1,CommissionOperation.Claim,CommissionCmd.State(player).Revision)&&player.Gold==gold+40,"重复提交不重复奖励");
        state=CommissionCmd.State(player);state.Accept(2,0,9);CommissionCmd.Write(player,state);var spell=state.Quests.Single(q=>q.Id==2);
        Check(!await CommissionCmd.Execute(player,2,CommissionOperation.Claim,state.Revision),"没有指定主牌组魔法不能提交");
        var card=run.CreateCard(CommissionCmd.Spell(spell.Spell),player);CardCmd.Upgrade(card);await CardPileCmd.Add(card,PileType.Deck);
        Check(CommissionCmd.OwnsSpell(player,spell.Spell),"升级版指定魔法符合要求");
        while(player.HasOpenPotionSlots)await PotionCmd.TryToProcure(CommissionCmd.Potion(spell.Reward.Item).ToMutable(),player);
        Check(!await CommissionCmd.Execute(player,2,CommissionOperation.Claim,state.Revision)&&CommissionCmd.Read(player).Quests.Single(q=>q.Id==2).Status==CommissionStatus.Accepted,"药水栏满不消耗委托或丢奖励");
        await PotionCmd.Discard(player.Potions.First());int potions=player.Potions.Count();
        Check(await CommissionCmd.Execute(player,2,CommissionOperation.Claim,CommissionCmd.State(player).Revision)&&player.Potions.Count()==potions+1&&player.Deck.Cards.Contains(card),"空出药水栏后领取固定药水，保留指定魔法");
        state=CommissionCmd.State(player);state.Accept(3,0,9);state.Record("prop",CommissionGoal.Monsters,99);CommissionCmd.Write(player,state);
        int props=MagicDeckStore.Read(player).Props.Count;
        Check(await CommissionCmd.Execute(player,3,CommissionOperation.Claim,state.Revision)&&MagicDeckStore.Read(player).Props.Count==props+1,"魔具报酬进入魔法牌组");
        state=CommissionCmd.State(player);state.Accept(4,0,9);state.Record("elite",CommissionGoal.Elites);CommissionCmd.Write(player,state);
        var relic=state.Quests.Single(q=>q.Id==4).Reward;await RelicCmd.Obtain(CommissionCmd.Relic(relic.Item).ToMutable(),player);gold=player.Gold;
        Check(await CommissionCmd.Execute(player,4,CommissionOperation.Claim,state.Revision)&&player.Gold==gold+relic.Amount,"已拥有固定遗物时按说明折算金币");
        var otherState=CommissionCmd.State(other);otherState.Accept(4,0,9);otherState.Record("other-elite",CommissionGoal.Elites);CommissionCmd.Write(other,otherState);
        var otherRelic=otherState.Quests.Single(q=>q.Id==4).Reward;int relicCount=other.Relics.Count;
        Check(await CommissionCmd.Execute(other,4,CommissionOperation.Claim,otherState.Revision)&&other.Relics.Count==relicCount+1&&other.Relics.Any(r=>r.Id==CommissionCmd.Relic(otherRelic.Item).Id),"尚未拥有的固定遗物通过原生命令获得");
        var json=JsonSerializer.Serialize(player.ToSerializable(),JsonSerializationUtility.Options);
        var restored=Player.FromSerializable(JsonSerializer.Deserialize<SerializablePlayer>(json,JsonSerializationUtility.Options)!);
        Check(CommissionCmd.Read(restored).ToJson()==CommissionCmd.Read(player).ToJson(),"原生玩家存读档保留委托与领取去重");
        state=CommissionCmd.State(player);state.Accept(6,0,9);CommissionCmd.Write(player,state);
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
        // 新幕不覆盖此前接取与领取记录。
        run.CurrentActIndex=1;run.ActFloor=1;CommissionCmd.Ensure(run);
        Check(CommissionCmd.Read(player).Quests.Count==12&&CommissionCmd.Read(player).Quests.Single(q=>q.Id==6).Status==CommissionStatus.Accepted,"跨幕生成新池并保留未完委托");
        CombatManager.Instance.Reset(true);
        run.ActFloor=5;state=CommissionCmd.State(player);state.Accept(104,1,5);CommissionCmd.Write(player,state);
        var elite=ModelDb.AllEncounters.First(e=>e.RoomType==RoomType.Elite).ToMutable();elite.GenerateMonstersWithSlots(run);
        var eliteRoom=new CombatRoom(elite,run);eliteRoom.CombatState.AddPlayer(player);
        CombatManager.Instance.SetUpCombat(eliteRoom.CombatState);
        CommissionCmd.Victory(run,eliteRoom);CommissionCmd.Victory(run,eliteRoom);
        Check(CommissionCmd.Read(player).Quests.Single(q=>q.Id==104).Progress==1,"精英只按胜利场次计数，重复通知不重复累计");
        CombatManager.Instance.Reset(true);run.ActFloor=1;run.PushRoom(new RestSiteRoom());
        var ui=new Control{Size=new(1920,1080)};root.AddChild(ui);var screen=new CommissionScreen(ui,player);
        foreach(string language in new[]{"zhs","eng"}) {
            LocManager.Instance.SetLanguage(language);screen.Open();await Tick();
            var list=screen.View.GetNode<VBoxContainer>("CommissionCanvas/CommissionScroll/Quests");
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
            if(OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/commission-"+language+".png"));
            screen.Close();await Tick();
        }
        screen.View.QueueFree();ui.QueueFree();await Tick();LocManager.Instance.SetLanguage("zhs");RunManager.Instance.CleanUp();return count;
    }
}

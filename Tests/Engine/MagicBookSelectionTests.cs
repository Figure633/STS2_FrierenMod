using System.Text.Json;
using FrierenMod.Core;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.UI;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;

public static class MagicBookSelectionTests
{
    public static async Task<int> Run(Node root)
    {
        int count=0;
        void Check(bool valid,string name) { if(!valid) throw new Exception(name); GD.Print("PASS 魔具取消："+name); count++; }
        var player=Player.CreateForNewRun<Frieren>(UnlockState.all,1);
        var run=RunState.CreateForTest([player],seed:"FRIEREN-CANCEL-PROP");
        RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService()); LocalContext.NetId=1;
        var deck=MagicDeckState.FromJson(JsonSerializer.Serialize(new MagicDeckSave(3,
            MagicDeckState.CreatePrototypeLoadout().Items.ToArray(),true,3)));
        int prop=deck.AddProp("TransmutationCircle").InstanceId;
        int secondProp=deck.AddProp("CrystalBall").InstanceId;
        MagicDeckStore.Write(player,deck);
        var console=new MegaCrit.Sts2.Core.DevConsole.ConsoleCommands.CardConsoleCmd();
        var boxResult=console.Process(player,[MegaCrit.Sts2.Core.Models.ModelDb.Card<FrierenMod.Scripts.Cards.MysteryBox>().Id.Entry]);
        Check(boxResult.success && MagicDeckStore.Read(player).Props.Count==3 &&
            !player.Deck.Cards.OfType<FrierenMod.Scripts.Cards.MagicPropCard>().Any(),"战斗外控制台默认目标将盲盒放入魔法牌组");
        var newFactor=run.CreateCard<FrierenMod.Scripts.Cards.BuildUpBody>(player);
        var normal=run.CreateCard<FrierenMod.Scripts.Cards.DefendFrieren>(player);
        var added=await MegaCrit.Sts2.Core.Commands.CardPileCmd.Add(new MegaCrit.Sts2.Core.Models.CardModel[]{newFactor,normal},
            MegaCrit.Sts2.Core.Entities.Cards.PileType.Deck);
        Check(added.Count==2 && added.All(r=>r.success) && newFactor.Pile==null && normal.Pile==player.Deck &&
            MagicDeckStore.Read(player).Items.Count==4,"混合批量获得时仅因子分流，普通牌正常加入牌组");
        MagicDeckStore.Write(player,deck);
        string before=deck.ToJson();
        var ui=new Control { Size=new(1920,1080) }; root.AddChild(ui);
        ui.AddChild(new ColorRect { Size=ui.Size,Color=new("17202c"),MouseFilter=Control.MouseFilterEnum.Ignore });
        var screen=new MagicBookScreen(ui,player,()=>null); screen.Open();
        NGridCardHolder Holder(string name)=>(NGridCardHolder)screen.View.FindChild(name,true,false);
        var source=Holder("Prop_"+prop);
        var target=Holder("Factor_"+deck.Items[0].InstanceId);
        var another=Holder("Prop_"+secondProp);
        void Click(NGridCardHolder holder,bool right=false) {
            var hitbox=holder.Hitbox;
            hitbox.EmitSignal(NClickableControl.SignalName.Focused,hitbox);
            hitbox.EmitSignal(NClickableControl.SignalName.MousePressed,new InputEventMouseButton {
                ButtonIndex=right?MouseButton.Right:MouseButton.Left,Pressed=true });
            hitbox.EmitSignal(NClickableControl.SignalName.MouseReleased,new InputEventMouseButton {
                ButtonIndex=right?MouseButton.Right:MouseButton.Left,Pressed=false });
        }
        float Width(NGridCardHolder holder)=>((ShaderMaterial)holder.CardNode!.CardHighlight.Material).GetShaderParameter("width").AsSingle();
        bool Clear(NGridCardHolder holder)=>Width(holder)==0 && holder.CardNode!.CardHighlight.Modulate.A==0;
        async Task Tick() { for(int i=0;i<8;i++) await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame); }
        async Task PointerClick(NGridCardHolder holder, bool right=false) {
            Vector2 point=holder.Hitbox.GetGlobalRect().GetCenter();
            root.GetViewport().PushInput(new InputEventMouseMotion { Position=point, GlobalPosition=point },true);
            await Tick();
            root.GetViewport().PushInput(new InputEventMouseButton { Position=point, GlobalPosition=point,
                ButtonIndex=right?MouseButton.Right:MouseButton.Left, Pressed=true },true);
            await Tick();
            root.GetViewport().PushInput(new InputEventMouseButton { Position=point, GlobalPosition=point,
                ButtonIndex=right?MouseButton.Right:MouseButton.Left, Pressed=false },true);
            await Tick();
        }
        async Task Snapshot(string name) {
            if(OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))
                root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/"+name+".png"));
            await Task.CompletedTask;
        }
        await Tick(); await PointerClick(source,true);
        Check(Width(source)>0,"真实鼠标右键经视口命中魔具："+((Label)screen.View.FindChild("BookStatus",true,false)).Text);
        await PointerClick(source,true);
        Check(Clear(source),"真实鼠标再次右键只取消一次，不重复切换");
        source.Hitbox.MouseFilter=Control.MouseFilterEnum.Ignore;
        await PointerClick(source,true);
        Check(Width(source)>0,"卡牌焦点未命中时仍可用实际鼠标启用魔具");
        await PointerClick(target);
        Check(Width(target)>0,"真实鼠标左键选择因子目标");
        await PointerClick(source,true);
        source.Hitbox.MouseFilter=Control.MouseFilterEnum.Stop;
        Click(source,true); Click(target); await Tick();
        Check(Width(source)>0 && Width(target)>0,"炼成阵和首个目标确实进入选中状态");
        await Snapshot("prop-before-cancel");
        Click(source,true); await Tick(); await Snapshot("prop-after-cancel");
        Check(Clear(source) && Clear(target),"右键取消同时清除魔具和目标的颜色与外光");
        Check(MagicDeckStore.Read(player).ToJson()==before && screen.View.Visible,"取消不消耗物品，也不关闭魔法牌组");
        Click(source,true); Click(target); Click(target); await Tick();
        Check(!Clear(source) && Clear(target),"取消单个目标只清除该目标的选中外光");
        Click(target); Click(another,true); await Tick();
        Check(Clear(source) && Clear(target) && Width(another)>0,"切换魔具清除上一组魔具和目标的外光");
        Click(target);
        root.GetViewport().PushInput(new InputEventKey { Keycode=Key.Escape,Pressed=true }); await Tick();
        Check(screen.View.Visible && Clear(another) && Clear(target),"Esc取消选择后没有绿色残留");
        Click(source,true); Click(target); screen.Close();
        Check(Clear(source) && Clear(target),"关闭界面立即清除选择");
        screen.Open(); await Tick();
        Check(screen.View.FindChildren("*","",true,false).OfType<NGridCardHolder>().All(Clear),"重开并复用卡牌节点不会带回旧选择");
        Check(MagicDeckStore.Read(player).ToJson()==before,"全部取消路径保持库存与槽位原样");
        var combat=new MegaCrit.Sts2.Core.Combat.CombatState(runState:run);combat.AddPlayer(player);
        var enemy=combat.CreateCreature(MegaCrit.Sts2.Core.Models.ModelDb.Monster<MegaCrit.Sts2.Core.Models.Monsters.Axebot>().ToMutable(),
            MegaCrit.Sts2.Core.Combat.CombatSide.Enemy,null);combat.AddCreature(enemy);enemy.SetCurrentHpInternal(50);
        MegaCrit.Sts2.Core.Combat.CombatManager.Instance.SetUpCombat(combat);
        HarmonyLib.AccessTools.Property(typeof(MegaCrit.Sts2.Core.Combat.CombatManager),"IsInProgress")
            .SetValue(MegaCrit.Sts2.Core.Combat.CombatManager.Instance,true);
        HarmonyLib.AccessTools.Property(typeof(MegaCrit.Sts2.Core.Combat.CombatManager),"IsStarting")
            .SetValue(MegaCrit.Sts2.Core.Combat.CombatManager.Instance,false);
        source=Holder("Prop_"+prop);target=Holder("Factor_"+deck.Items[0].InstanceId);
        await PointerClick(source,true);await PointerClick(target);
        Check(Width(source)>0 && Width(target)>0,"战斗中实际鼠标右键启用、左键选目标");
        screen.View.ProcessMode=Node.ProcessModeEnum.Disabled;
        await PointerClick(source,true);
        Check(Width(source)>0,"菜单禁用魔法牌组输入时不会误操作");
        screen.View.ProcessMode=Node.ProcessModeEnum.Inherit;
        await PointerClick(source,true);
        Check(Clear(source) && Clear(target),"战斗中实际鼠标再次右键取消");
        int toolCount=MagicDeckStore.Read(player).Props.Count;
        var combatTool=combat.CreateCard<FrierenMod.Scripts.Cards.UnbelievableTool>(player);
        await MegaCrit.Sts2.Core.Commands.CardPileCmd.Add(combatTool,MegaCrit.Sts2.Core.Entities.Cards.PileType.Hand);
        Check(combatTool.Pile==null && MagicDeckStore.Read(player).Props.Count==toolCount+1,
            "战斗中加入手牌的魔具也改为魔法牌组库存");
        var factorResult=console.Process(player,[MegaCrit.Sts2.Core.Models.ModelDb.Card<FrierenMod.Scripts.Cards.AdvancedDefensiveMagic>().Id.Entry,"deck"]);
        Check(factorResult.success && !player.Deck.Cards.OfType<FrierenMod.Scripts.Cards.FactorCard>().Any(),
            "指定主牌组的控制台因子仍进入魔法牌组");
        screen.Close(); ui.QueueFree(); await Tick();
        return count;
    }
}

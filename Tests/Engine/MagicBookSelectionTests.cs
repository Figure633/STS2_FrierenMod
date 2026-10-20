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
        async Task Snapshot(string name) {
            if(OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))
                root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/"+name+".png"));
            await Task.CompletedTask;
        }
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
        screen.Close(); ui.QueueFree(); await Tick();
        return count;
    }
}

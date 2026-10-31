using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Powers;
using FrierenMod.Scripts.Presentation;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Unlocks;
using System.Text.Json;

public static class PowerIconTests
{
    public static async Task<int> Run(Node root)
    {
        int checks=0;void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
        LocManager.Instance.SetLanguage("zhs");
        using var atlasLoader=new MegaCrit.Sts2.Core.Assets.AtlasResourceLoader();
        ResourceLoader.AddResourceFormatLoader(atlasLoader,true);
        var player=Player.CreateForNewRun<Frieren>(UnlockState.all,1);
        var powers=typeof(FrierenPower).Assembly.GetTypes().Where(t=>!t.IsAbstract&&typeof(FrierenPower).IsAssignableFrom(t))
            .OrderBy(t=>t.Name).Select(t=>(FrierenPower)ModelDb.GetById<PowerModel>(ModelDb.GetId(t))).ToArray();
        var records=new List<object>();
        foreach(var power in powers){
            Check(ResourceLoader.Exists(power.CustomPackedIconPath)&&ResourceLoader.Exists(power.CustomBigIconPath),"图标文件缺失："+power.Id);
            Check(power.Icon.GetWidth()>0&&power.BigIcon.GetHeight()>0,"图标不能加载："+power.Id);
            records.Add(new{model=power.GetType().Name,name=power.Title.GetFormattedText(),icon=power.CustomPackedIconPath,bigIcon=power.CustomBigIconPath,
                width=power.Icon.GetWidth(),height=power.Icon.GetHeight(),retired=power is TrickPower or SpiegelMalleablePower});
        }
        Check(ModelDb.Power<FrierenTheSlayerPower>().Icon.GetSize()==new Vector2(84,84),"无五官头像按84x84导入");
        var scene=GD.Load<PackedScene>("res://scenes/combat/power.tscn");
        var canvas=new Control();root.AddChild(canvas);
        canvas.AddChild(new ColorRect{Size=new(1920,1080),Color=new("17202c")});
        var originals=new List<TextureRect>();var liveIcons=new List<NPower>();var labels=new List<Label>();
        canvas.AddChild(new Label{Text="左：原图　右：游戏内轻微提亮后",Position=new(60,20)});
        for(int i=0;i<powers.Length;i++){
            var model=powers[i].ToMutable();model.ApplyInternal(player.Creature,1);
            var node=scene.Instantiate<NPower>();node.Model=model;canvas.AddChild(node);
            node.Position=new(100+(i%8)*235,80+(i/8)*120);node.Scale=Vector2.One*1.6f;
            var icon=node.GetNode<TextureRect>("%Icon");
            Check(icon.SelfModulate.IsEqualApprox(PowerIconPresentation.IconTint),"战斗图标提亮："+model.Id);
            var material=icon.Material;
            AccessTools.Method(typeof(NPower),"Reload").Invoke(node,null);
            Check(icon.SelfModulate.IsEqualApprox(PowerIconPresentation.IconTint)&&icon.Material==material,"重复加载不累积提亮且保留原生材质："+model.Id);
            // 静态对照图去掉入场动画和能力爆闪，保留真实图标节点及其原生材质。
            (AccessTools.Field(typeof(NPower),"_animInTween").GetValue(node) as Tween)?.Kill();
            node.Modulate=Colors.White;icon.Position=Vector2.Zero;node.GetNode<CpuParticles2D>("%PowerFlash").Visible=false;
            liveIcons.Add(node);
            var original=new TextureRect{Texture=powers[i].Icon,Position=node.Position+new Vector2(-70,0),Size=new(64,64),
                ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered};
            canvas.AddChild(original);originals.Add(original);
            var label=new Label{Text=powers[i].Title.GetFormattedText(),Position=node.Position+new Vector2(-70,70),Size=new(215,30),HorizontalAlignment=HorizontalAlignment.Center};
            canvas.AddChild(label);labels.Add(label);
        }
        var hover=new Control();root.AddChild(hover);
        var hoverIcon=new TextureRect{Texture=ModelDb.Power<DomainExpansionPower>().Icon};hover.AddChild(hoverIcon);
        PowerIconPresentation.ApplyToHoverIcons(hover);PowerIconPresentation.ApplyToHoverIcons(hover);
        Check(hoverIcon.SelfModulate.IsEqualApprox(PowerIconPresentation.IconTint),"悬停图标提亮且幂等");
        hoverIcon.Texture=null;PowerIconPresentation.ApplyToHoverIcons(hover);Check(hoverIcon.SelfModulate==Colors.White,"换为非模组图标恢复颜色");
        var vanilla=ModelDb.Power<StrengthPower>().ToMutable();vanilla.ApplyInternal(player.Creature,1);
        var native=scene.Instantiate<NPower>();native.Model=vanilla;hover.AddChild(native);native.Visible=false;
        Check(native.GetNode<TextureRect>("%Icon").SelfModulate==Colors.White,"原版能力图标不变");
        for(int i=0;i<45;i++)await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
        if(OS.GetCmdlineUserArgs().Contains("--ui-snapshot")) {
            root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/power-icon-review/native-all-icons.png"));
            int shown=0;
            for(int i=0;i<powers.Length;i++) {
                bool selected=powers[i] is PaperPlanePower or RecyclePower or SwampManaNextTurnPower;
                liveIcons[i].Visible=originals[i].Visible=labels[i].Visible=selected;
                if(!selected)continue;
                var origin=new Vector2(70+shown%4*470,190+shown/4*390);
                originals[i].Position=origin;originals[i].Size=new(128,128);
                liveIcons[i].Position=origin+new Vector2(155,0);liveIcons[i].Scale=Vector2.One*3.2f;
                labels[i].Position=origin+new Vector2(0,165);labels[i].Size=new(300,35);
                shown++;
            }
            for(int i=0;i<3;i++)await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
            root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/power-icon-review/native-icons.png"));
        }
        File.WriteAllText(ProjectSettings.GlobalizePath("res://../../artifacts/power-icon-review/power-icons.json"),JsonSerializer.Serialize(records,new JsonSerializerOptions{WriteIndented=true,Encoder=System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping}));
        canvas.QueueFree();hover.QueueFree();await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
        ResourceLoader.RemoveResourceFormatLoader(atlasLoader);
        GD.Print("PASS 全部能力图标真实路径/小图/大图及原生显示提亮："+checks);return checks;
    }
}

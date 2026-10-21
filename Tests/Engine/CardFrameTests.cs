using BaseLib.Abstracts;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Pooling;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;

public static class CardFrameTests
{
    public static async Task<int> Run(Node root)
    {
        int count=0;
        void Check(bool valid,string name) { if(!valid) throw new Exception(name); GD.Print("PASS 卡背："+name); count++; }
        var player=Player.CreateForNewRun<Frieren>(UnlockState.all,1);
        var run=RunState.CreateForTest([player],seed:"FRIEREN-CARD-FRAMES");
        RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService()); LocalContext.NetId=1;
        CardModel Card(Type type) {
            var card=ModelDb.GetById<CardModel>(ModelDb.GetId(type)).ToMutable(); card.Owner=player; return card;
        }
        var ui=new Control { Size=new(1920,1080) }; root.AddChild(ui);
        ui.AddChild(new ColorRect { Size=ui.Size,Color=new("17202c"),MouseFilter=Control.MouseFilterEnum.Ignore });
        var originalStyles=new[] {
            (typeof(StrikeFrieren),"frieren_attack"),(typeof(DefendFrieren),"frieren_skill"),(typeof(RitesPreparation),"frieren_power"),
            (typeof(ManaDetection),"shared_attack"),(typeof(GrapeSpell),"shared_skill"),(typeof(FlyingMagic),"shared_power")
        };
        foreach(var (type,style) in originalStyles)
            Check(Card(type).Frame.ResourcePath.EndsWith(style+".tres"),type.Name+"保持已验收的通用卡背");
        var samples=new[] {
            (typeof(BuildUpBody),"magic"),(typeof(HighSpeedMovement),"magic"),(typeof(GreatMageComposure),"magic"),
            (typeof(TransmutationCircle),"magic"),(typeof(Mana),"magic"),(typeof(HighDensityMana),"magic")
        };
        var nodes=new List<NCard>();
        for(int i=0;i<samples.Length;i++) {
            var (type,style)=samples[i]; var model=Card(type);
            var frame=(AtlasTexture)model.Frame;
            Check(frame.ResourcePath.EndsWith(style+".tres") && frame.Atlas.GetSize()==new Vector2(1024,1024) &&
                frame.Region.Size==new Vector2(600,844), type.Name+"使用对应的一代原图与卡框区域");
            var material=model.FrameMaterial as ShaderMaterial;
            Check(material?.Shader.ResourcePath.EndsWith("legacy_card_frame.gdshader")==true &&
                material.GetShaderParameter("frame_atlas").AsGodotObject() is Texture2D,
                type.Name+"通过原生卡面遮罩保留插画开口");
            var node=NodePool.Get<NCard>(); AccessTools.Property(typeof(NCard),nameof(NCard.Model)).SetValue(node,model);
            node.Position=new(420+(i%3)*540,280+(i/3)*510); node.Scale=Vector2.One*.9f;
            ui.AddChild(node); node.UpdateVisuals(PileType.None,CardPreviewMode.Normal); nodes.Add(node);
        }
        foreach(var definition in FrierenMod.Core.SpellCatalog.All)
            if(!FactorCard.Create(player,definition.Id).Frame.ResourcePath.EndsWith("magic.tres"))
                throw new Exception("遗漏紫色因子卡背："+definition.Id);
        foreach(var definition in FrierenMod.Core.MagicPropRules.All)
            if(!MagicPropCard.Create(player,definition.Id).Frame.ResourcePath.EndsWith("magic.tres"))
                throw new Exception("遗漏紫色魔具卡背："+definition.Id);
        Check(true,"全部28种因子与8种魔具使用一代紫色卡背");
        foreach(var type in new[]{typeof(BabySleeping),typeof(ShavedIceSpell),typeof(ManaDetection),typeof(GrapeSpell),typeof(FlyingMagic),typeof(Disperse),typeof(ClearMind)})
            if(!Card(type).Frame.ResourcePath.Contains("shared_")) throw new Exception("双色卡遗漏："+type.Name);
        Check(true,"七张一代双色卡全部保留公共卡背");
        foreach(var type in new[]{typeof(LongevityPain),typeof(Laziness)}) {
            var model=(CustomCardModel)Card(type);
            Check(model.CustomFrame==null && model.CreateCustomFrameMaterial==null &&
                model.Frame.ResourcePath=="res://images/atlases/ui_atlas.sprites/card/card_frame_skill_s.tres" &&
                model.VisualCardPool is MegaCrit.Sts2.Core.Models.CardPools.StatusCardPool or MegaCrit.Sts2.Core.Models.CardPools.CurseCardPool,
                type.Name+"状态或诅咒使用原版卡框");
        }
        foreach(var language in new[]{"zhs","eng"}) {
            MegaCrit.Sts2.Core.Localization.LocManager.Instance.SetLanguage(language);
            foreach(var model in new[]{Card(typeof(Mana)),Card(typeof(HighDensityMana))}) {
                var node=NodePool.Get<NCard>();AccessTools.Property(typeof(NCard),nameof(NCard.Model)).SetValue(node,model);
                ui.AddChild(node);node.UpdateVisuals(PileType.None,CardPreviewMode.Normal);
                Check(node.GetNode<MegaCrit.Sts2.addons.mega_text.MegaLabel>("%TypeLabel").Text==(language=="zhs"?"魔力":"Mana"),
                    model.GetType().Name+"实际类型牌匾显示魔力："+language);
                Check(model.Type==CardType.Status && model.Frame.ResourcePath.EndsWith("magic.tres"),"魔力身份及紫色卡框保持兼容");
                ui.RemoveChild(node);node.QueueFreeSafely();
            }
        }
        MegaCrit.Sts2.Core.Localization.LocManager.Instance.SetLanguage("eng");
        var time=Card(typeof(TimeConcept));
        Check(!time.ShouldShowInCardLibrary && !time.CanBeGeneratedInCombat && !time.CanBeGeneratedByModifiers &&
            !player.Character.CardPool.GetUnlockedCards(player.UnlockState,CardMultiplayerConstraint.SingleplayerOnly).Any(c=>c is TimeConcept),
            "TimeConcept不再进入卡牌库、奖励、商店或生成入口");
        Check(!(bool)AccessTools.Property(typeof(CardModel),"IsPlayable").GetValue(time)!,"旧TimeConcept不能再发动");
        Check(player.Character.CardPool.AllCards.Any(c=>c.Id==time.Id) && ModelDb.GetById<CardModel>(time.Id) is TimeConcept,
            "TimeConcept保留原模型ID与卡池归属以兼容旧存档");
        Check(CardModel.FromSerializable(time.ToSerializable()) is TimeConcept restored && restored.Id==time.Id,
            "旧TimeConcept卡牌存档仍可读取");
        for(int i=0;i<5;i++) await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
        if(OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))
            root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/card-frames.png"));
        foreach(var node in nodes) { ui.RemoveChild(node); node.QueueFreeSafely(); }
        ui.QueueFree();
        for(int i=0;i<4;i++) await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
        return count;
    }
}

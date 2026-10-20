using System.Text.RegularExpressions;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Powers;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Pooling;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;

public static class SynchroDisplayTests
{
    public static async Task<int> Run(Node root)
    {
        int count=0;
        void Check(bool valid,string name) { if(!valid) throw new Exception(name); GD.Print("PASS 同调显示："+name); count++; }
        var player=Player.CreateForNewRun<Frieren>(UnlockState.all,1);
        var run=RunState.CreateForTest([player],seed:"FRIEREN-SYNCHRO-DISPLAY");
        RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService()); LocalContext.NetId=1;
        var combat=new CombatState(runState:run); combat.AddPlayer(player);
        var enemy=combat.CreateCreature(ModelDb.Monster<MegaCrit.Sts2.Core.Models.Monsters.Axebot>().ToMutable(),CombatSide.Enemy,null);
        combat.AddCreature(enemy); CombatManager.Instance.SetUpCombat(combat);
        AccessTools.Property(typeof(CombatManager),nameof(CombatManager.IsInProgress)).SetValue(CombatManager.Instance,true);
        var context=new BlockingPlayerChoiceContext();
        T Card<T>(bool upgrade=false) where T:CardModel {
            var card=(T)ModelDb.GetById<CardModel>(ModelDb.GetId(typeof(T))).ToMutable(); card.Owner=player;
            if(upgrade) { card.UpgradeInternal(); card.FinalizeUpgradeInternal(); } return card;
        }
        var mana=Card<Mana>(); var dense=Card<HighDensityMana>();
        await CardPileCmd.Add(mana,PileType.Hand); await CardPileCmd.Add(dense,PileType.Hand);
        var ui=new Control { Size=new(1920,1080) }; root.AddChild(ui);
        ui.AddChild(new ColorRect { Size=ui.Size,Color=new("17202c"),MouseFilter=Control.MouseFilterEnum.Ignore });
        NCard Node(CardModel model,Vector2 position) {
            var node=NodePool.Get<NCard>(); AccessTools.Property(typeof(NCard),nameof(NCard.Model)).SetValue(node,model);
            node.Position=position; node.Scale=Vector2.One*1.15f; ui.AddChild(node);
            node.UpdateVisuals(PileType.Hand,CardPreviewMode.Normal); return node;
        }
        var manaNode=Node(mana,new(570,470)); var denseNode=Node(dense,new(1320,470));
        var pileMana=new List<CardModel>();
        foreach(var pile in new[]{PileType.Draw,PileType.Discard,PileType.Exhaust}) {
            var card=Card<Mana>(); await CardPileCmd.Add(card,pile); pileMana.Add(card);
        }
        async Task Snapshot(string name) {
            if(!OS.GetCmdlineUserArgs().Contains("--ui-snapshot")) return;
            for(int i=0;i<4;i++) await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
            root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/"+name+".png"));
        }
        string Plain(string value)=>Regex.Replace(value,@"\[[^\]]*\]","");
        void State(string key,string art) {
            string title=new LocString("frieren","TIP."+key+".title").GetFormattedText();
            Check(manaNode.GetNode<TextureRect>("%Portrait").Texture.ResourcePath.EndsWith(art+".png") &&
                manaNode.GetNode<RichTextLabel>("%DescriptionLabel").Text.Contains("[gold]"+title+"[/gold]") &&
                denseNode.GetNode<RichTextLabel>("%DescriptionLabel").Text.Contains("[gold]"+title+"[/gold]"),
                "能力事件立即同步卡图和两种魔力的关键词："+key);
            Check(pileMana.All(c=>c.PortraitPath.EndsWith(art+".png")) &&
                Card<Mana>().PortraitPath.EndsWith(art+".png"),"其他牌堆与新生成魔力同步状态："+key);
            Check(mana.HoverTips.OfType<HoverTip>().Single(t=>t.Title==title).Description==
                new LocString("frieren","TIP."+key+".description").GetFormattedText(),"悬停使用当前同调效果："+key);
        }
        foreach(string lang in new[]{"zhs","eng"}) {
            LocManager.Instance.SetLanguage(lang);
            manaNode.UpdateVisuals(PileType.Hand,CardPreviewMode.Normal); denseNode.UpdateVisuals(PileType.Hand,CardPreviewMode.Normal);
            State("SYNCHRO","Mana"); await Snapshot("synchro-"+lang+"-normal");
            var accelerate=await PowerCmd.Apply<AccelerateFlowPower>(context,player.Creature,1,player.Creature,null);
            State("ACCELERATED_SYNCHRO","Mana2"); await Snapshot("synchro-"+lang+"-accelerated");
            var thunder=await PowerCmd.Apply<ThunderPower>(context,player.Creature,1,player.Creature,null);
            State("ULTIMATE_ACCELERATED_SYNCHRO","Mana4"); await Snapshot("synchro-"+lang+"-both");
            await accelerate!.AfterSideTurnEnd(context,CombatSide.Player,[player.Creature]);
            State("ULTIMATE_SYNCHRO","Mana3"); await Snapshot("synchro-"+lang+"-ultimate");
            await PowerCmd.Remove(thunder);
            State("SYNCHRO","Mana");
            thunder=await PowerCmd.Apply<ThunderPower>(context,player.Creature,1,player.Creature,null);
            accelerate=await PowerCmd.Apply<AccelerateFlowPower>(context,player.Creature,1,player.Creature,null);
            State("ULTIMATE_ACCELERATED_SYNCHRO","Mana4");
            await PowerCmd.Remove(thunder);
            State("ACCELERATED_SYNCHRO","Mana2"); await PowerCmd.Remove(accelerate);

            foreach(var model in ModelDb.AllCards.Where(c=>c is FrierenCard)) {
                var card=model.ToMutable(); card.Owner=player;
                string formatted=card.GetDescriptionForPile(PileType.None);
                string text=Plain(formatted);
                string uncolored=Regex.Replace(formatted,@"\[gold\].*?\[/gold\]","",RegexOptions.Singleline);
                string pattern=lang=="zhs"?@"同调|咏唱|传说魔法|魔力|封印|回火":@"\b(?:Synchro|Chant|Mana|Legendary Magic|Sealed|Backfire)\b";
                if(Regex.IsMatch(uncolored,pattern,RegexOptions.IgnoreCase)) throw new Exception("关键词未变金色："+lang+"/"+card.Id);
                var previews=card.HoverTips.OfType<CardHoverTip>().ToArray();
                if(card is not Mana && text.Contains(lang=="zhs"?"魔力":"Mana",StringComparison.OrdinalIgnoreCase) &&
                    !previews.Any(p=>p.Card is Mana)) throw new Exception("魔力预览遗漏："+lang+"/"+card.Id);
                if(previews.Any(p=>p.Card.GetType()==card.GetType()) || previews.Select(p=>p.Id).Distinct().Count()!=previews.Length)
                    throw new Exception("衍生预览自引用或重复："+card.Id);
            }
            Check(true,lang+"卡面所有魔力引用都有原生卡牌预览，且无自引用和重复");
            Check(true,lang+"卡面核心关键词均已标记为金色");
            foreach(var (source,target) in new[]{(typeof(FlowerFieldSpell),typeof(BlueMoonWeed)),
                (typeof(DefensiveMagic),typeof(SecondDefensiveMagic)),(typeof(FireSpell),typeof(LittleFire)),
                (typeof(RedAppleSpell),typeof(GreenApple))}) {
                var card=ModelDb.GetById<CardModel>(ModelDb.GetId(source)).ToMutable(); card.Owner=player;
                card.UpgradeInternal(); card.FinalizeUpgradeInternal();
                Check(card.HoverTips.OfType<CardHoverTip>().Any(p=>p.Card.GetType()==target && p.Card.IsUpgraded),
                    source.Name+"正确预览升级后的衍生卡");
            }
            Check(Card<ElementsBombing>().HoverTips.OfType<CardHoverTip>().Count()==5,"元素生成牌预览全部五种可能的元素");
            foreach(string keyword in new[]{"SYNCHRO","ACCELERATED_SYNCHRO","ULTIMATE_SYNCHRO","ULTIMATE_ACCELERATED_SYNCHRO"}) {
                string tip=Plain(new LocString("frieren","TIP."+keyword+".description").GetFormattedText());
                Check(tip.Length<(lang=="zhs"?40:90),keyword+"说明保持简短");
            }
        }
        LocManager.Instance.SetLanguage("zhs");
        count+=PuzzlePresentationTests.Run(player);
        ui.RemoveChild(manaNode);
        string detachedPortrait=manaNode.GetNode<TextureRect>("%Portrait").Texture.ResourcePath;
        var detachedPower=await PowerCmd.Apply<AccelerateFlowPower>(context,player.Creature,1,player.Creature,null);
        Check(manaNode.GetNode<TextureRect>("%Portrait").Texture.ResourcePath==detachedPortrait,"移出场景后不再接收旧能力事件");
        ui.AddChild(manaNode); manaNode.UpdateVisuals(PileType.Hand,CardPreviewMode.Normal);
        Check(manaNode.GetNode<TextureRect>("%Portrait").Texture.ResourcePath.EndsWith("Mana2.png"),"节点重入场景后正确订阅并显示当前状态");
        await PowerCmd.Remove(detachedPower);
        var previewSource=Card<FlowerFieldSpell>(true);
        var preview=previewSource.HoverTips.OfType<CardHoverTip>().Single(p=>p.Card is BlueMoonWeed).Card;
        ui.RemoveChild(manaNode); ui.RemoveChild(denseNode); manaNode.QueueFreeSafely(); denseNode.QueueFreeSafely();
        var sourceNode=Node(previewSource,new(570,470)); var previewNode=Node(preview,new(1320,470));
        await Snapshot("derived-card-preview");
        ui.RemoveChild(sourceNode); ui.RemoveChild(previewNode); sourceNode.QueueFreeSafely(); previewNode.QueueFreeSafely();
        ui.QueueFree();
        for(int i=0;i<5;i++) await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
        AccessTools.Property(typeof(CombatManager),nameof(CombatManager.IsInProgress)).SetValue(CombatManager.Instance,false);
        return count;
    }
}

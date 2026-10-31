using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;
using System.Text.RegularExpressions;

public static class CardPresentationFixTests
{
    public static async Task<int> Run(Node root, bool ancientOnly = false)
    {
        int checks=0;
        void Check(bool ok,string message) { if(!ok)throw new Exception(message);checks++; }
        var player=Player.CreateForNewRun<Frieren>(UnlockState.all,1);
        var run=RunState.CreateForTest([player],seed:"PRESENTATION105");
        RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService());
        var room=new CombatRoom(ModelDb.Encounter<TurretOperatorWeak>().ToMutable(),run);run.PushRoom(room);
        var combat=room.CombatState;combat.AddPlayer(player);
        var enemy=combat.CreateCreature(ModelDb.Monster<Axebot>().ToMutable(),CombatSide.Enemy,null);combat.AddCreature(enemy);
        CombatManager.Instance.SetUpCombat(combat);CombatTestState.SetInProgress(true);
        var context=new ThrowingPlayerChoiceContext();
        var underestimated=combat.CreateCard<Underestimated>(player);
        await underestimated.OnPlayWrapper(context,null,false,new ResourceInfo{EnergySpent=1,EnergyValue=1,StarsSpent=0,StarValue=0},true);
        Check(player.Creature.GetPowerAmount<ShrinkPower>()==1,"看扁仅获得1回合缩小");
        await player.Creature.GetPower<ShrinkPower>()!.AfterSideTurnEnd(context,CombatSide.Player,[player.Creature]);
        Check(!player.Creature.HasPower<ShrinkPower>(),"缩小在本回合结束消失");
        foreach(var model in new CardModel[]{ModelDb.Card<Trick>(),ModelDb.Card<ManaConcealment>()}) {
            Check(FrierenCardAvailability.IsRetired(model),model.Id+"已弃用");
            Check(!model.CanBeGeneratedInCombat&&!model.CanBeGeneratedByModifiers,model.Id+"不再生成");
            Check(!FrierenCardAvailability.Cards([model],player).Any(),model.Id+"不在商店/奖励候选");
            Check(!FrierenTheSlayerPower.SpellPool().Contains(model),model.Id+"不在先古魔法池");
            Check(ModelDb.GetById<CardModel>(model.Id)==model,model.Id+"保留旧档ID");
        }
        int hand=player.PlayerCombatState!.Hand.Cards.Count;
        await PowerCmd.Apply<TrickPower>(context,player.Creature,1,player.Creature,null);
        await player.Creature.GetPower<TrickPower>()!.AfterPlayerTurnStart(context,player);
        Check(player.PlayerCombatState.Hand.Cards.Count==hand,"旧档小把戏能力停止生成魔力隐藏/抑制");
        foreach(var language in new[]{"zhs","eng"}) {
            LocManager.Instance.SetLanguage(language);
            var shrink=underestimated.HoverTips.OfType<HoverTip>().Single(t=>t.Id==ModelDb.Power<ShrinkPower>().Id.ToString());
            Check(Regex.Replace(shrink.Description,@"\[[^\]]+\]","").Contains(language=="zhs"?"1回合":"1 turn"),"原生缩小悬停正确显示1回合："+shrink.Description);
        }
        LocManager.Instance.SetLanguage("zhs");
        void Gold(string text,string id) {
            int depth=0;
            foreach(string part in Regex.Split(text,@"(\[[^\]]+\])")) {
                if(part=="[gold]")depth++;
                else if(part=="[/gold]")depth--;
                else if(Regex.IsMatch(part,"抽牌堆|手牌|弃牌堆"))Check(depth>0,"未标黄："+id+" "+text);
            }
        }
        foreach(var template in ModelDb.AllCards.OfType<FrierenCard>()) {
            var card=(CardModel)template.ToMutable();card.Owner=player;
            Gold(card.GetDescriptionForPile(PileType.None),card.Id.ToString());
            if(card.MaxUpgradeLevel>0) {card.UpgradeInternal();Gold(card.GetDescriptionForPile(PileType.None),card.Id+"+");}
        }
        foreach(var factor in SpellCatalog.All) foreach(var slot in Enum.GetValues<ManaSlot>()) {
            Gold(FactorCard.Create(player,factor.Id,slot).GetDescriptionForPile(PileType.None),factor.Id+"初始");
            Gold(FactorCard.Create(player,factor.Id,slot,new ChantOption(slot,factor.Id,2,3,4)).GetDescriptionForPile(PileType.None),factor.Id+"咏唱");
        }
        var canvas=new Control();root.AddChild(canvas);
        canvas.AddChild(new ColorRect{Size=new Vector2(1920,1080),Color=new Color("17202c")});
        // 游戏启动时注册此原生加载器；独立场景也必须注册，先古边框/正文底板才会加载。
        using var atlasLoader=new MegaCrit.Sts2.Core.Assets.AtlasResourceLoader();
        ResourceLoader.AddResourceFormatLoader(atlasLoader,true);
        var scene=GD.Load<PackedScene>("res://scenes/cards/card.tscn");
        CardModel[] cards=ancientOnly
            ? [ModelDb.Card<EndlessFlow>(),ModelDb.Card<FrierenTheSlayer>(),ModelDb.Card<EndlessFlow>(),ModelDb.Card<FrierenTheSlayer>()]
            : [ModelDb.Card<EndlessFlow>(),ModelDb.Card<FrierenTheSlayer>(),ModelDb.Card<RingletForm>(),ModelDb.Card<AccessoriesSpell>(),ModelDb.Card<FlyingMagic>(),ModelDb.Card<RitesPreparation>(),ModelDb.Card<GoodLuckSpell>(),ModelDb.Card<Underestimated>()];
        var nodes=new List<NCard>();
        for(int i=0;i<cards.Length;i++) {
            var model=(CardModel)cards[i].ToMutable();model.Owner=player;
            if(ancientOnly&&i>=2)model.UpgradeInternal();
            var node=scene.Instantiate<NCard>();AccessTools.Property(typeof(NCard),nameof(NCard.Model)).SetValue(node,model);
            canvas.AddChild(node);node.Position=new Vector2(270+i%4*455,ancientOnly?550:280+i/4*520);
            if(ancientOnly)node.Scale=Vector2.One*1.25f;
            node.UpdateVisuals(PileType.None,CardPreviewMode.Normal);nodes.Add(node);
            var portrait=node.GetNode<TextureRect>(model.Rarity==CardRarity.Ancient?"%AncientPortrait":"%Portrait");
            var expected=model.Rarity==CardRarity.Ancient?new Vector2(598,842):new Vector2(500,380);
            Check(portrait.Texture.GetSize()==expected,model.Id+"插画比例符合原生画框");
        }
        for(int i=0;i<4;i++)await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
        foreach(var node in nodes) {
            var label=node.GetNode<RichTextLabel>("%DescriptionLabel");
            Check(label.GetContentHeight()<=label.Size.Y+2,node.Model!.Id+"正文无溢出");
        }
        if(OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath(ancientOnly?"res://../../artifacts/ancient-review/native-cards.png":"res://../../artifacts/card-art-fix/native-cards.png"));
        canvas.QueueFree();
        await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
        ResourceLoader.RemoveResourceFormatLoader(atlasLoader);
        GD.Print("PASS 卡图、1回合缩小悬停、弃用模型和全部卡面牌堆标黄："+checks);
        return checks;
    }
}

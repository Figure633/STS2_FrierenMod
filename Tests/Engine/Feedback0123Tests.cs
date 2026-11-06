using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Monsters;
using FrierenMod.Scripts.Powers;
using FrierenMod.Scripts.UI;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.ValueProps;

public static class Feedback0123Tests
{
    public static async Task<int> Run(Node root)
    {
        int count=0;
        void Check(bool value,string message){if(!value)throw new Exception(message);count++;GD.Print("PASS 反馈修复："+message);}
        var player=Player.CreateForNewRun<Frieren>(UnlockState.all,1);
        var other=Player.CreateForNewRun<Frieren>(UnlockState.all,2);
        var run=RunState.CreateForTest([player,other],seed:"FEEDBACK0123");
        RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService());LocalContext.NetId=1;
        var combat=new CombatState(runState:run);combat.AddPlayer(player);combat.AddPlayer(other);
        var enemy=combat.CreateCreature(ModelDb.Monster<Axebot>().ToMutable(),CombatSide.Enemy,null);combat.AddCreature(enemy);
        enemy.SetMaxHpInternal(9999);enemy.SetCurrentHpInternal(9999);
        CombatManager.Instance.SetUpCombat(combat);CombatTestState.SetInProgress(true);
        var context=new BlockingPlayerChoiceContext();
        T Card<T>(bool upgraded=false)where T:CardModel{var c=combat.CreateCard<T>(player);if(upgraded)CardCmd.Upgrade(c);return c;}
        async Task Use(CardModel card){await CardPileCmd.Add(card,PileType.Hand);await card.OnPlayWrapper(context,enemy,false,new ResourceInfo{EnergySpent=0,EnergyValue=0,StarsSpent=0,StarValue=0},true);}
        await Use(Card<OilSpell>(true));
        Check(enemy.CurrentHp==9975&&enemy.GetPowerAmount<VulnerablePower>()==2,"油渍升级两段各12伤，伤后施加易伤");
        foreach(var power in enemy.Powers.ToArray())await PowerCmd.Remove(power);
        foreach(bool upgraded in new[]{false,true}){
            var card=Card<RestraintMagic>(upgraded);Check(card.EnergyCost.GetWithModifiers(CostModifiers.Local)==3,"拘束魔法普通／升级均3费："+upgraded);
            await Use(card);var power=enemy.GetPower<BindingStrengthDownPower>()!;
            Check(enemy.GetPowerAmount<StrengthPower>()==-(upgraded?15:10)&&!power.HoverTips.OfType<CardHoverTip>().Any(),"暂时减力量不带尖啸卡牌预览");
            await power.AfterSideTurnEnd(context,CombatSide.Enemy,[enemy]);
            Check(enemy.GetPowerAmount<StrengthPower>()==0&&!enemy.HasPower<BindingStrengthDownPower>(),"回合结束恢复力量并移除临时能力");
        }
        foreach(var power in enemy.Powers.ToArray())await PowerCmd.Remove(power);
        await PowerCmd.Apply<ArtifactPower>(context,enemy,1,enemy,null);await Use(Card<RestraintMagic>());
        Check(enemy.GetPowerAmount<StrengthPower>()==0&&!enemy.HasPower<BindingStrengthDownPower>(),"人工制品阻止临时减力量，力量不被错误恢复");
        var elite=await PowerCmd.Apply<CommissionElitePower>(context,enemy,2,enemy,null);
        Check(elite!.Amount==2&&elite.StackType==MegaCrit.Sts2.Core.Entities.Powers.PowerStackType.None,"委托强化保留幕数计算但隐藏图标数字");
        var powerNode=MegaCrit.Sts2.Core.Nodes.Combat.NPower.Create(elite);root.AddChild(powerNode);
        Check(powerNode.GetNode("%AmountLabel").Get("text").AsString()==string.Empty,"原生能力图标的数字文本实际为空");powerNode.QueueFree();
        foreach(var pile in player.PlayerCombatState!.AllPiles)foreach(var c in pile.Cards.ToArray())c.RemoveFromCurrentPile();
        var aura=(Aura)ModelDb.Monster<Aura>().ToMutable();combat.AddCreature(combat.CreateCreature(aura,CombatSide.Enemy,"Boss"));
        await aura.BeforeHandDraw(player,context,combat);await aura.BeforeHandDraw(other,context,combat);
        foreach(var p in new[]{player,other}){
            var dispel=p.PlayerCombatState!.Hand.Cards.Single();
            Check(dispel is AuraDispel&&dispel.Rarity==CardRarity.Rare&&dispel is IGeneratedOnlyCard,"每位玩家开场得到金边且不进入常规卡池的解除魔法");
            for(int i=0;i<5;i++)await CardPileCmd.Add(combat.CreateCard<StrikeFrieren>(p),PileType.Draw);
            await CardPileCmd.Draw(context,5,p);
            Check(p.PlayerCombatState.Hand.Cards[0]==dispel,"开局抽牌后解除魔法仍在左手第一张");
        }
        foreach(var lang in new[]{"zhs","eng"}){
            LocManager.Instance.SetLanguage(lang);
            foreach(var id in FrierenMod.Core.SlotBackCatalog.Ids)Check(!SlotBackLibrary.Text("BACK."+id+".description").Contains("[gold]"),"成就描述无裸露标签："+lang+"/"+id);
            Check(!new LocString("events","FRIERENMOD-MIMIC_FIGHT.pages.INITIAL.options.FIGHT.description").GetRawText().Contains("forgery")&&!new LocString("events","FRIERENMOD-MIMIC_FIGHT.pages.INITIAL.options.FIGHT.description").GetRawText().Contains("持有伪作"),"宝箱怪选项省略伪作条件："+lang);
            Check(!Card<ReadyStance>().Description.GetRawText().Contains("优先"),"准备架势简化描述："+lang);
        }
        var mana=Card<Mana>();Check(mana.Type==CardType.Status,"终极同调仍使用状态牌魔力，未改变类型");
        LocManager.Instance.SetLanguage("zhs");
        var canvas=new Control();root.AddChild(canvas);
        canvas.AddChild(new ColorRect{Size=new Vector2(1920,1080),Color=new Color("17202c")});
        using var atlasLoader=new MegaCrit.Sts2.Core.Assets.AtlasResourceLoader();
        ResourceLoader.AddResourceFormatLoader(atlasLoader,true);
        var scene=GD.Load<PackedScene>("res://scenes/cards/card.tscn");
        CardModel[] display=[Card<AuraDispel>(),Card<RestraintMagic>(),Card<OilSpell>(true),Card<ReadyStance>()];
        for(int i=0;i<display.Length;i++){
            var node=scene.Instantiate<MegaCrit.Sts2.Core.Nodes.Cards.NCard>();
            HarmonyLib.AccessTools.Property(node.GetType(),"Model").SetValue(node,display[i]);canvas.AddChild(node);
            node.Position=new Vector2(270+455*i,330);node.UpdateVisuals(PileType.None,CardPreviewMode.Normal);
        }
        for(int i=0;i<2;i++){
            string relic=i==0?"AuraCompanion":"MimicHead";
            canvas.AddChild(new TextureRect{Texture=GD.Load<Texture2D>("res://FrierenMod/images/relics/"+relic+".tres"),Position=new Vector2(500+i*600,700),Size=new Vector2(200,200),ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered});
        }
        for(int i=0;i<4;i++)await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
        if(OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/feedback-20261107/native-review.png"));
        canvas.QueueFree();await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);ResourceLoader.RemoveResourceFormatLoader(atlasLoader);
        LocManager.Instance.SetLanguage("eng");RunManager.Instance.CleanUp();return count;
    }
}

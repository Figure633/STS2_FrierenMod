using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using FrierenMod.Scripts.Presentation;
using FrierenMod.Scripts.UI;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;

public static class Todo104Tests
{
    public static async Task<int> Run(Node root)
    {
        int count=0;void Check(bool ok,string text){if(!ok)throw new Exception(text);count++;GD.Print("PASS TODO104: "+text);}
        var player=Player.CreateForNewRun<Frieren>(UnlockState.all,1);
        var run=RunState.CreateForTest([player],seed:"TODO104");RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService());LocalContext.NetId=1;
        var combat=new CombatState(runState:run);combat.AddPlayer(player);
        var enemies=Enumerable.Range(0,2).Select(_=>combat.CreateCreature(ModelDb.Monster<Axebot>().ToMutable(),CombatSide.Enemy,null)).ToArray();
        foreach(var e in enemies){combat.AddCreature(e);e.SetMaxHpInternal(9999);e.SetCurrentHpInternal(9999);}
        CombatManager.Instance.SetUpCombat(combat);CombatTestState.SetInProgress(true);var context=new ThrowingPlayerChoiceContext();
        T New<T>(bool up=false) where T:CardModel {var c=combat.CreateCard<T>(player);if(up)CardCmd.Upgrade(c);return c;}
        async Task Use(CardModel c,int x=0){if(c.Pile?.Type!=PileType.Hand)await CardPileCmd.Add(c,PileType.Hand);if(c.EnergyCost.CostsX)c.EnergyCost.CapturedXValue=x;await c.OnPlayWrapper(context,enemies[0],false,new ResourceInfo{EnergySpent=x,EnergyValue=x,StarsSpent=0,StarValue=0},true);}
        async Task Reset(){foreach(var power in player.Creature.Powers.ToArray())await PowerCmd.Remove(power);foreach(var pile in player.PlayerCombatState!.AllPiles)foreach(var c in pile.Cards.ToArray())pile.RemoveInternal(c);foreach(var e in enemies){e.SetCurrentHpInternal(9999);e.LoseBlockInternal(e.Block);}CombatManager.Instance.History.Clear();}
        foreach(var card in new CardModel[]{New<BackFlow>(),New<BreaksBarriersSpell>(),New<FireSpell>()}){
            Check(!FrierenCardAvailability.IsRandomCandidate(card),"弃用牌退出普通获取池："+card.Id);
            Check(!FrierenTheSlayerPower.SpellPool().Any(c=>c.Id==card.Id),"先古能力不再生成弃用牌："+card.Id);
            Check(CardModel.FromSerializable(card.ToSerializable()).Id==card.Id,"旧存档模型保留："+card.Id);
        }
        player.PlayerCombatState!.Energy = 0;
        foreach (var mana in new CardModel[] { New<Mana>(), New<HighDensityMana>(), New<HighDensityMana>(true) })
        {
            await CardPileCmd.Add(mana, PileType.Hand);
            mana.EnergyCost.AddThisCombat(3);
            Check(mana.EnergyCost.GetWithModifiers(CostModifiers.All) < 0 && mana.CanPlay(), "加费后魔力仍隐藏费用，零能量可打出：" + mana.Id);
            var (energy, stars) = await mana.SpendResources();
            Check(energy == 0 && stars == 0, "魔力出牌免费：" + mana.Id);
        }
        await Reset();
        var spark=New<KindlingSpark>();await Use(spark,0);
        Check(enemies.All(e=>e.CurrentHp==9986)&&!player.PlayerCombatState!.Hand.Cards.OfType<LittleFire>().Any(),"0能量仍造成13群伤，普通不生成火花");
        Check(spark.Pile?.Type==PileType.Exhaust,"星星之火使用原生消耗去向");
        await Reset();await Use(New<KindlingSpark>(),2);
        Check(enemies.All(e=>e.CurrentHp==9986)&&player.PlayerCombatState!.Hand.Cards.OfType<LittleFire>().Count()==2,"2能量固定13群伤并生成2张火花");
        await Reset();await Use(New<KindlingSpark>(true),2);
        Check(enemies.All(e=>e.CurrentHp==9981)&&player.PlayerCombatState!.Hand.Cards.OfType<LittleFire>().Count()==2&&player.PlayerCombatState.Hand.Cards.OfType<LittleFire>().All(c=>!c.IsUpgraded),"升级18群伤，2能量生成2张普通火花");
        Check(DerivedCardPreviews.For(New<KindlingSpark>(true)).OfType<CardHoverTip>().Single().Card is LittleFire {IsUpgraded:false},"升级来源的悬停预览保持普通火花");
        await Reset();await PowerCmd.Apply<StrengthPower>(context,player.Creature,2,player.Creature,null);await Use(New<KindlingSpark>(),1);
        Check(enemies.All(e=>e.CurrentHp==9984),"固定群伤受原生力量加成");
        await Reset();for(int i=0;i<9;i++)await CardPileCmd.Add(New<StrikeFrieren>(),PileType.Hand);var full=New<KindlingSpark>(true);await CardPileCmd.Add(full,PileType.Hand);await Use(full,2);
        Check(player.PlayerCombatState!.Hand.Cards.OfType<LittleFire>().Count()==1&&player.PlayerCombatState.DiscardPile.Cards.OfType<LittleFire>().Count()==1,"满手牌按原版将多余火花放入弃牌堆");
        await Reset();var fire=New<LittleFire>();var old=New<LittleFire>();await CardPileCmd.Add(old,PileType.Exhaust);await Use(fire);
        Check(enemies[0].CurrentHp==9996&&fire.DynamicVars.Damage.BaseValue==5&&old.DynamicVars.Damage.BaseValue==5&&New<LittleFire>().DynamicVars.Damage.BaseValue==3,"火花从3伤开始，只强化战斗中已经存在的火花");
        Check(fire.EnergyCost.GetAmountToSpend()==1&&New<LittleFire>(true).DynamicVars.Damage.BaseValue==5&&fire.Rarity==CardRarity.Uncommon,"火花升级5伤、保留回火加费，稀有度改蓝");
        await Reset();var multi=New<MultipleOffensiveMagic>();await Use(multi,2);
        Check(enemies.Sum(e=>9999-e.CurrentHp)==12&&ManaCmd.InPile(player,PileType.Hand).Count()==2&&multi.Rarity==CardRarity.Common,"多重攻击维持原X+4伤、生成魔力与白卡稀有度");
        await Reset();await RelicCmd.Obtain<ChemicalX>(player);await Use(New<KindlingSpark>(true),0);
        Check(enemies.All(e=>e.CurrentHp==9981)&&player.PlayerCombatState!.Hand.Cards.OfType<LittleFire>().Count()==2,"化学物X仅增加火花数量，不增加升级18点群伤");
        await Reset();var preview=New<KindlingSpark>(true);await CardPileCmd.Add(preview,PileType.Hand);player.PlayerCombatState!.Energy=2;
        foreach(string language in new[]{"zhs","eng"}){
            LocManager.Instance.SetLanguage(language);preview.UpdateDynamicVarPreview(CardPreviewMode.Normal,enemies[0],preview.DynamicVars);string text=preview.GetDescriptionForPile(PileType.Hand);
            Check(text.Contains("X")&&!text.Contains("X+1")&&text.Contains("4")&&!text.Contains("{"),"X费正文保留规则，括号反映含化学物X的4张火花："+language);
            var canvas=new Control{Size=new(1920,1080)};root.AddChild(canvas);canvas.AddChild(new ColorRect{Size=canvas.Size,Color=new("17202c")});
            var scene=GD.Load<PackedScene>("res://scenes/cards/card.tscn");
            // 卡面截图只使用本机正式版完整资源；测试版共用PCK不包含新富文本场景脚本。
            if(OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))
            foreach(var (model,pos) in new[]{((CardModel)New<KindlingSpark>(),new Vector2(260,350)),((CardModel)New<KindlingSpark>(true),new Vector2(620,350)),((CardModel)New<LittleFire>(true),new Vector2(980,350))}){
                var node=scene.Instantiate<NCard>();AccessTools.Property(typeof(NCard),nameof(NCard.Model)).SetValue(node,model);canvas.AddChild(node);node.Position=pos;node.UpdateVisuals(PileType.None,CardPreviewMode.Normal);
            }
            var tip=new LegacyAchievementTip{Position=new(1220,170),CustomMinimumSize=new(360,0)};canvas.AddChild(tip);var box=new VBoxContainer();tip.AddChild(box);
            var title=new Label{Text=language=="zhs"?"不曾停下的旅途":"A Journey Without End"};box.AddChild(title);LegacyAchievementTip.ApplyFont(title,true);
            var body=new Label{Text=language=="zhs"?"通关时用时超过1小时。\n解锁新的卡槽背景。":"Win a run in over an hour.\nUnlock a new slot background."};box.AddChild(body);LegacyAchievementTip.ApplyFont(body,false);
            Check(((FontVariation)title.GetThemeFont("font")).BaseFont.ResourcePath.StartsWith("res://themes/")&&title.GetThemeFontSize("font_size")==23&&body.GetThemeFontSize("font_size")==22,"成就提示复用原版字体，保留23/22字号："+language);
            for(int i=0;i<4;i++)await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
            foreach(var node in canvas.GetChildren().OfType<NCard>()){var label=node.GetNode<RichTextLabel>("%DescriptionLabel");Check(label.GetContentHeight()<=label.Size.Y+2,"新卡原生正文无溢出："+language+node.Model!.Id);}
            if(OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/todo-review-current/cards-fonts-"+language+".png"));
            canvas.QueueFree();await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
        }
        var pool=ModelDb.CardPool<FrierenCardPool>().AllCards.Where(FrierenCardAvailability.IsRandomCandidate).ToArray();
        var statistics=new {total=pool.Length,rarities=pool.GroupBy(c=>c.Rarity.ToString()).ToDictionary(g=>g.Key,g=>g.Count()),types=pool.GroupBy(c=>c.Type.ToString()).ToDictionary(g=>g.Key,g=>g.Count()),costs=pool.GroupBy(c=>c.EnergyCost.CostsX?"X":c.EnergyCost.Canonical.ToString()).ToDictionary(g=>g.Key,g=>g.Count())};
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://../../artifacts/todo-review-current/frieren-statistics.json"),System.Text.Json.JsonSerializer.Serialize(statistics));
        LocManager.Instance.SetLanguage("zhs");
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://../../artifacts/todo-review-current/spell-pool.json"),System.Text.Json.JsonSerializer.Serialize(FrierenTheSlayerPower.SpellPool().Select(c=>new {model=c.GetType().Name,name=c.Title,id=c.Id.Entry}),new System.Text.Json.JsonSerializerOptions{Encoder=System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping}));
        RunManager.Instance.CleanUp();return count;
    }
}

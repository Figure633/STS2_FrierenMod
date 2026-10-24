using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Potions;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Potions;
using FrierenMod.Core;
using FrierenMod.Scripts.Encounters;
using FrierenMod.Scripts.Relics;
using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.HoverTips;

public static class UpdatedTodoTests
{
    public static async Task<int> Run(Node root)
    {
        int count=0;
        void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;GD.Print("PASS 显示修正："+name);}
        var player=Player.CreateForNewRun<Frieren>(UnlockState.all,1);
        var run=RunState.CreateForTest([player],seed:"DISPLAY-UPDATES");RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService());LocalContext.NetId=1;
        foreach(var potion in new PotionModel[]{ModelDb.Potion<FirePotion>(),ModelDb.Potion<BlockPotion>(),ModelDb.Potion<EnergyPotion>(),
            ModelDb.Potion<BottledMana>(),ModelDb.Potion<EmperorWine>(),ModelDb.Potion<DissolveClothPotion>()})
        {
            var used=potion.Image.GetImage().GetUsedRect();
            Check(potion.Image.GetSize()==new Vector2(80,80) && Math.Max(used.Size.X,used.Size.Y)>=78,"原版与模组药水瓶身尺寸："+potion.Id.Entry);
        }
        var stage=GD.Load<PackedScene>("res://FrierenMod/scenes/spiegel_encounter.tscn").Instantiate();
        var reference=GD.Load<PackedScene>("res://scenes/encounters/queen_boss.tscn").Instantiate();
        Check(stage.GetNode<Marker2D>("Boss").Position.Y==reference.GetNode<Marker2D>("queen").Position.Y,"复制体对齐原版第三幕Boss站位");stage.Free();reference.Free();
        Check(MagicPropRules.All.All(p=>MagicPropCard.Create(player,p.Id).Rarity==CardRarity.Event),"全部魔具采用原生紫色事件稀有度");
        foreach(var language in new[]{"zhs","eng"}){
            LocManager.Instance.SetLanguage(language);
            var accessories=(AccessoriesSpell)ModelDb.Card<AccessoriesSpell>().ToMutable();accessories.Owner=player;
            Check(accessories.GetDescriptionForPile(PileType.None).Contains(language=="zhs"?"传说魔法":"Legendary Spell") && accessories.HoverTips.OfType<HoverTip>().Any(t=>t.Title?.Contains(language=="zhs"?"传说魔法":"Legendary Spell")==true),language+"饰品魔法正文和悬停均显示传说魔法");
            foreach(var id in new[]{"NatureBeauty","Strategizing"}){
                var card=FactorCard.Create(player,id);var option=ChantRules.Preview(ManaSlot.Draw,SpellCatalog.Get(id).Rules,2);
                Check(card.GetDescriptionForPile(PileType.None).Contains("[img]") && FactorCard.Create(player,id,ManaSlot.Draw,option).GetDescriptionForPile(PileType.None).Contains("[img]"),language+"因子规则与实际收益显示能量图标："+id);
            }
            var horn=MagicPropCard.Create(player,"ShadowDragonHorn");Check(horn.GetDescriptionForPile(PileType.None).Contains("[img]"),language+"暗黑龙的角能量图标");
            foreach(var model in new CardModel[]{ModelDb.Card<LureTheEnemyInDeep>(),ModelDb.Card<TrueColours>(),ModelDb.Card<HighDensityMana>()}){
                var card=model.ToMutable();card.Owner=player;Check(card.GetDescriptionForPile(PileType.None).Contains("[img]"),language+"其他卡牌能量图标："+model.Id.Entry);
            }
            var choice=(LegendaryCraftChoice)ModelDb.Card<LegendaryCraftChoice>().ToMutable();choice.Owner=player;choice.Effect=new("Energy",2);
            Check(choice.GetDescriptionForPile(PileType.None).Contains("[img]"),language+"自定义魔法收益选项能量图标");
            var relic=ModelDb.Relic<HimmelGravestone>().ToMutable();relic.Owner=player;Check(relic.DynamicDescription.GetFormattedText().Contains("[img]"),language+"遗物收益能量图标");
        }
        var host=new Node2D();root.AddChild(host);
        int column=0;
        foreach(var potion in new PotionModel[]{ModelDb.Potion<FirePotion>(),ModelDb.Potion<BlockPotion>(),ModelDb.Potion<EnergyPotion>(),ModelDb.Potion<BottledMana>(),ModelDb.Potion<EmperorWine>(),ModelDb.Potion<DissolveClothPotion>()})
            host.AddChild(new Sprite2D{Texture=potion.Image,Position=new Vector2(140+column++*130,200)});
        for(int i=0;i<5;i++)await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
        if(OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/potion-size-updated.png"));
        host.QueueFree();LocManager.Instance.SetLanguage("eng");RunManager.Instance.CleanUp();return count;
    }
}

using System.Text.Json;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.ValueProps;

public static class HonestLivingTests
{
    public static async Task<int> Run(Node root)
    {
        int checks=0;void Check(bool ok,string text){if(!ok)throw new Exception(text);checks++;GD.Print("PASS 老实赚钱："+text);}
        SaveManager.Instance.InitPrefsDataForTest();
        var p=Player.CreateForNewRun<Frieren>(UnlockState.all,1);var other=Player.CreateForNewRun<Frieren>(UnlockState.all,2);
        var run=RunState.CreateForTest([p,other],seed:"HONEST-LIVING");RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService());LocalContext.NetId=1;
        var combat=new CombatState(runState:run);combat.AddPlayer(p);combat.AddPlayer(other);
        var enemies=Enumerable.Range(0,3).Select(_=>combat.CreateCreature(ModelDb.Monster<Axebot>().ToMutable(),CombatSide.Enemy,null)).ToArray();
        foreach(var e in enemies){combat.AddCreature(e);e.SetMaxHpInternal(9999);e.SetCurrentHpInternal(9999);}
        CombatManager.Instance.SetUpCombat(combat);CombatTestState.SetInProgress(true);p.PlayerCombatState!.Energy=10;
        var context=new ThrowingPlayerChoiceContext();
        HonestLiving Card(bool upgrade=false){var c=combat.CreateCard<HonestLiving>(p);if(upgrade)CardCmd.Upgrade(c);return c;}
        async Task Use(CardModel c){await CardPileCmd.Add(c,PileType.Hand);await c.OnPlayWrapper(context,null,false,new ResourceInfo{EnergySpent=1,EnergyValue=1,StarsSpent=0,StarValue=0},true);}
        int[] Seek(int min,Func<int[],bool> predicate){
            var rng=run.Rng.Niche;
            for(int counter=rng.ToSerializable().counter;counter<rng.ToSerializable().counter+20000;counter++){
                var probe=new Rng(rng.ToSerializable());for(int advance=rng.ToSerializable().counter;advance<counter;advance++)probe.NextInt(8);
                int[] rolls=Enumerable.Range(0,3).Select(_=>probe.NextInt(min,8)).ToArray();
                if(predicate(rolls)){while(rng.ToSerializable().counter<counter)rng.NextInt(8);return rolls;}
            }
            throw new Exception("未找到本测试需要的随机序列");
        }
        int Hp()=>enemies.Sum(e=>e.CurrentHp);
        Check(DebtStore.Read(p)==0&&DebtStore.Read(other)==0,"新局及没有字段的旧档负债为0");
        var card=Card();Check(card.EnergyCost.Canonical==1&&card.Rarity==CardRarity.Rare&&card.Type==CardType.Attack&&card.TargetType==TargetType.RandomEnemy,"1费稀有攻击、原生随机敌人目标");
        Check(card.Keywords.Contains(CardKeyword.Exhaust)&&FrierenCardAvailability.IsRandomCandidate(card),"正常稀有池可获得，使用后消耗");
        await PowerCmd.Apply<StrengthPower>(context,p.Creature,20,p.Creature,null);
        await PowerCmd.Apply<WeakPower>(context,p.Creature,2,p.Creature,null);
        foreach(var e in enemies)await PowerCmd.Apply<VulnerablePower>(context,e,2,p.Creature,null);
        int[] rolls=Seek(4,r=>r.Contains(4)&&r.Any(n=>n!=7));int hp=Hp(),gold=p.Gold,counter=run.Rng.Niche.ToSerializable().counter;
        await Use(card);
        Check(hp-Hp()==rolls.Sum(),"普通4～7三段固定伤害，不受力量／虚弱／易伤影响");
        Check(run.Rng.Niche.ToSerializable().counter==counter+3,"独立三次整数投点，使用可存档的原生随机数流");
        Check(p.Gold==gold&&DebtStore.Read(p)==7&&DebtStore.Read(other)==0,"失败只累积7负债，不扣金币且不影响队友");
        Check(card.Pile?.Type==PileType.Exhaust,"实际使用后进入消耗堆");
        card=Card(true);rolls=Seek(5,r=>r.Contains(5)&&r.Any(n=>n!=7));hp=Hp();await Use(card);
        Check(hp-Hp()==rolls.Sum()&&rolls.All(n=>n is >=5 and <=7),"升级5～7，仍为固定三段伤害");
        Check(DebtStore.Read(p)==14&&p.Gold==gold,"连续失败累积负债，仍不扣钱");
        foreach(var e in enemies)await CreatureCmd.GainBlock(e,50,ValueProp.Unpowered,null);
        rolls=Seek(4,r=>r.All(n=>n==7));hp=Hp();await Use(Card());
        Check(Hp()==hp&&p.Gold==gold+777&&DebtStore.Read(p)==14,"三次7即得777，格挡不妨碍判奖且不清除原负债");
        // Player 通过 BaseLib 的 ExtendedSaveHandlers 保存，不能伪装成卡牌 SavedProperties。
        var savedPlayer=p.ToSerializable();
        var json=JsonSerializer.Serialize(savedPlayer,JsonSerializationUtility.Options);var restored=JsonSerializer.Deserialize<SerializablePlayer>(json,JsonSerializationUtility.Options)!;
        var loaded=Player.FromSerializable(restored);Check(DebtStore.Read(loaded)==14,"完整玩家存档JSON往返保留负债");
        var writer=new PacketWriter();savedPlayer.Serialize(writer);var reader=new PacketReader();reader.Reset(writer.Buffer);var packet=new SerializablePlayer();packet.Deserialize(reader);
        loaded=Player.FromSerializable(packet);
        Check(DebtStore.Read(loaded)==14&&writer.BitPosition==reader.BitPosition,"原生网络存档往返保留玩家负债");
        foreach(var lang in new[]{"zhs","eng"}){
            LocManager.Instance.SetLanguage(lang);var preview=Card();preview.UpgradeInternal();
            string text=preview.GetDescriptionForUpgradePreview();
            Check(text.Contains("[green]5[/green]")&&text.Contains("14")&&!text.Contains("LOC_ERR")&&!text.Contains('{'),lang+"升级下限绿字及实时累计负债");
            card=Card(true);var saved=CardModel.FromSerializable(card.ToSerializable());
            Check(saved is HonestLiving&&saved.IsUpgraded&&saved.DynamicVars["MinDamage"].IntValue==5,"新卡普通／升级模型原生存读 "+lang);
        }
        using var atlas=new MegaCrit.Sts2.Core.Assets.AtlasResourceLoader();ResourceLoader.AddResourceFormatLoader(atlas,true);
        var canvas=new Control{Size=new(1920,1080)};root.AddChild(canvas);canvas.AddChild(new ColorRect{Size=canvas.Size,Color=new("17202c")});
        var scene=GD.Load<PackedScene>("res://scenes/cards/card.tscn");
        foreach(var lang in new[]{"zhs","eng"}){
            LocManager.Instance.SetLanguage(lang);var upgraded=Card();upgraded.UpgradeInternal();
            var nodes=new List<MegaCrit.Sts2.Core.Nodes.Cards.NCard>();CardModel[] models=[Card(),upgraded];
            for(int i=0;i<models.Length;i++){
                var node=scene.Instantiate<MegaCrit.Sts2.Core.Nodes.Cards.NCard>();AccessTools.Property(node.GetType(),"Model").SetValue(node,models[i]);canvas.AddChild(node);
                node.Position=new(580+650*i,510);node.Scale=Vector2.One*1.1f;node.UpdateVisuals(PileType.None,i==0?CardPreviewMode.Normal:CardPreviewMode.Upgrade);nodes.Add(node);
            }
            for(int i=0;i<5;i++)await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
            foreach(var node in nodes){var label=node.GetNode<RichTextLabel>("%DescriptionLabel");Check(label.GetContentHeight()<=label.Size.Y+2,lang+"卡面正文无溢出 "+node.Model!.IsUpgraded);}
            if(OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath($"res://../../artifacts/honest-living-20261108/cards-{lang}.png"));
            foreach(var node in nodes){canvas.RemoveChild(node);node.QueueFree();}
        }
        canvas.QueueFree();await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);ResourceLoader.RemoveResourceFormatLoader(atlas);
        RunManager.Instance.CleanUp();return checks;
    }
}

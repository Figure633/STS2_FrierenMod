using System.Text.Json;
using FrierenMod.Core;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Relics;
using FrierenMod.Scripts.Rewards;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;

public static class MagicRewardDropTests
{
    public static async Task<int> Run()
    {
        int count=0;
        void Check(bool ok,string name) { if(!ok)throw new Exception(name); count++;GD.Print("PASS 魔法掉落："+name); }
        var player=Player.CreateForNewRun<Frieren>(UnlockState.all,1);
        var other=Player.CreateForNewRun<MegaCrit.Sts2.Core.Models.Characters.Ironclad>(UnlockState.all,2);
        var run=RunState.CreateForTest([player,other],seed:"FRIEREN-MAGIC-REWARDS");
        RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService());LocalContext.NetId=1;
        run.PushRoom(new RestSiteRoom());
        var deck=MagicDeckState.FromJson(JsonSerializer.Serialize(new MagicDeckSave(3,
            MagicDeckState.CreatePrototypeLoadout().Items.ToArray(),true,3,StartingPropsGranted:true)));
        deck.RewardPity=100;MagicDeckStore.Write(player,deck);
        Check(run.IterateHookListeners(null).OfType<MagicItemRewardHooks>().Count()==1,
            "BaseLib 原生跑局订阅自动接入掉落钩子且只注册一次");
        CombatRoom Room<T>(bool eventCombat=false) where T:EncounterModel => new(ModelDb.Encounter<T>().ToMutable(),run) {
            ParentEventId=eventCombat ? new ModelId("EVENT","FAKE_MERCHANT") : null };
        async Task Victory(CombatRoom room) => await Hook.AfterCombatVictory(run,room.CombatState,room);
        void SetPity(int pity) { var state=MagicDeckStore.Read(player);state.RewardPity=pity;MagicDeckStore.Write(player,state); }
        var room=Room<TurretOperatorWeak>();
        await Victory(room);
        var reward=room.ExtraRewards[player].OfType<FactorReward>().Single();
        Check(reward.IsPopulated && reward.Choices.Length==3 && reward.Choices.Distinct().Count()==3 &&
            MagicDeckStore.Read(player).RewardPity==90,"战斗胜利经原生额外奖励入口生成固定的三选一，并减少掉落补偿");
        Check(!room.ExtraRewards.ContainsKey(other),"其他角色不获得芙莉莲魔法物品奖励");
        string stateAfter=MagicDeckStore.Read(player).ToJson();int rngAfter=player.PlayerRng.Rewards.Counter;
        await Victory(room);
        Check(room.ExtraRewards[player].Count==1 && MagicDeckStore.Read(player).ToJson()==stateAfter && player.PlayerRng.Rewards.Counter==rngAfter,
            "同场胜利重复通知不会重复掉落或推进随机数");
        await RelicCmd.Remove(player.GetRelic<HolyEmblem>()!);SetPity(100);
        var withoutEmblem=Room<TurretOperatorWeak>();await Victory(withoutEmblem);
        Check(withoutEmblem.ExtraRewards[player].OfType<FactorReward>().Count()==1,"移除圣徽后仍按角色正常掉落");
        SetPity(100);var boss=Room<CeremonialBeastBoss>();await Victory(boss);
        Check(boss.ExtraRewards[player].OfType<FactorReward>().Count()==1,"Boss 按一代普通房规则参与掉落");
        SetPity(-100);var miss=Room<TurretOperatorWeak>();await Victory(miss);
        Check(!miss.ExtraRewards.ContainsKey(player) && MagicDeckStore.Read(player).RewardPity==-90,"未掉落时补偿增加10");
        int missRng=player.PlayerRng.Rewards.Counter;await Victory(miss);
        Check(player.PlayerRng.Rewards.Counter==missRng && MagicDeckStore.Read(player).RewardPity==-90,"未掉落场次也不会重复判定");
        void Escape(CombatRoom battle) {
            var enemy=battle.CombatState.CreateCreature(ModelDb.Monster<MegaCrit.Sts2.Core.Models.Monsters.Axebot>().ToMutable(),CombatSide.Enemy,null);
            battle.CombatState.AddCreature(enemy);battle.CombatState.CreatureEscaped(enemy);
        }
        SetPity(100);var escaped=Room<TurretOperatorWeak>();Escape(escaped);await Victory(escaped);
        Check(!escaped.ExtraRewards.ContainsKey(player) && MagicDeckStore.Read(player).RewardPity==110,"普通战斗有敌人逃跑时不掉落");
        SetPity(100);var eventRoom=Room<TurretOperatorWeak>(true);Escape(eventRoom);await Victory(eventRoom);
        Check(eventRoom.ExtraRewards[player].OfType<FactorReward>().Count()==1,"事件战斗沿用一代掉落判定");

        int seed=Enumerable.Range(0,1000).First(s=> {
            var offer=MagicRewardRules.Offer(5,new Rng((uint)s).NextInt);
            return offer.Choices.Any(id=>MagicPropRules.All.Any(p=>p.Id==id)) && offer.Choices.Any(id=>SpellCatalog.All.Any(f=>f.Id==id));
        });
        var current=MagicDeckStore.Read(player);current.RewardRarityOffset=5;MagicDeckStore.Write(player,current);
        reward=new FactorReward(player){Seed=seed};reward.Populate();
        string[] offered=reward.Choices;stateAfter=MagicDeckStore.Read(player).ToJson();rngAfter=player.PlayerRng.Rewards.Counter;
        reward.Populate();
        Check(reward.Choices.SequenceEqual(offered) && MagicDeckStore.Read(player).ToJson()==stateAfter && player.PlayerRng.Rewards.Counter==rngAfter,
            "混合奖励重复展开不重掷、不重复更新补偿");
        var save=reward.ToSerializable();
        string json=JsonSerializer.Serialize(save,JsonSerializationUtility.Options);
        var saved=JsonSerializer.Deserialize<SerializableReward>(json,JsonSerializationUtility.Options)!;
        var restored=(FactorReward)Reward.FromSerializable(saved,player);
        Check(json.Contains("FrierenMagicItemChoices") && restored.Choices.SequenceEqual(offered),"BaseLib 扩展 JSON 保存完整混合选项");
        Check(MagicDeckStore.Read(player).ToJson()==stateAfter && player.PlayerRng.Rewards.Counter==rngAfter,
            "恢复奖励不重算稀有度，也不推进玩家 RNG");
        var writer=new PacketWriter();save.Serialize(writer);var reader=new PacketReader();reader.Reset(writer.Buffer);
        var packet=new SerializableReward();packet.Deserialize(reader);
        var remote=(FactorReward)Reward.FromSerializable(packet,other);
        Check(remote.Player==other && remote.Choices.SequenceEqual(offered) && writer.BitPosition==reader.BitPosition,
            "原生数据包往返保留选项及所属玩家");
        room=Room<TurretOperatorWeak>();room.AddExtraReward(player,reward);room.MarkPreFinished();
        var roomSave=JsonSerializer.Deserialize<SerializableRoom>(JsonSerializer.Serialize(room.ToSerializable(),JsonSerializationUtility.Options),JsonSerializationUtility.Options)!;
        var restoredRoom=CombatRoom.FromSerializable(roomSave,run);
        Check(restoredRoom.ExtraRewards[player].OfType<FactorReward>().Single().Choices.SequenceEqual(offered),"实际战斗房间存档保留未领取的混合奖励");
        await Victory(restoredRoom);
        Check(restoredRoom.ExtraRewards[player].Count==1 && MagicDeckStore.Read(player).ToJson()==stateAfter,
            "恢复已结束房间不再判定掉落或更新补偿");
        var legacy=(FactorReward)Reward.FromSerializable(new SerializableReward{RewardType=FactorReward.FrierenFactor,GoldAmount=92376},player);
        Check(legacy.Choices.Length==3 && legacy.Choices.All(id=>SpellCatalog.All.Any(f=>f.Id==id)) &&
            MagicDeckStore.Read(player).ToJson()==stateAfter,"旧版仅有种子的奖励保留旧因子选项，不改写为魔具");

        var selector=new TestCardSelector();using var choices=CardSelectCmd.UseSelector(selector);
        selector.PrepareToSelect(Array.Empty<int>());
        Check(!await restored.SelectUnsynchronized() && !restored.SuccessfullySelected &&
            MagicDeckStore.Read(player).ToJson()==stateAfter,"取消混合选牌不消耗奖励或变更库存");
        int propIndex=Array.FindIndex(offered,id=>MagicPropRules.All.Any(p=>p.Id==id));
        int propCount=MagicDeckStore.Read(player).Props.Count;int itemCount=MagicDeckStore.Read(player).Items.Count;int mainCount=player.Deck.Cards.Count;
        selector.PrepareToSelect([propIndex]);
        Check(await restored.SelectUnsynchronized() && restored.SuccessfullySelected && MagicDeckStore.Read(player).Props.Count==propCount+1 &&
            MagicDeckStore.Read(player).Props.Last().PropId==offered[propIndex] && MagicDeckStore.Read(player).Items.Count==itemCount && player.Deck.Cards.Count==mainCount,
            "奖励中的魔具实际选中后只进入魔法牌组");
        int factorIndex=Array.FindIndex(offered,id=>SpellCatalog.All.Any(f=>f.Id==id));selector.PrepareToSelect([factorIndex]);
        Check(await MagicDeckCmd.ChooseReward(new BlockingPlayerChoiceContext(),player,offered) && MagicDeckStore.Read(player).Items.Count==itemCount+1 &&
            MagicDeckStore.Read(player).Items.Last().SpellId==offered[factorIndex] && player.Deck.Cards.Count==mainCount,
            "混合奖励中的因子仍正常进入未装载库存");
        foreach(string language in new[]{"zhs","eng"}) {
            MegaCrit.Sts2.Core.Localization.LocManager.Instance.SetLanguage(language);
            string description=restored.Description.GetFormattedText();
            Check(description.Contains(language=="zhs" ? "魔具" : "Tool"),"奖励描述包含因子与魔具："+language);
        }
        selector.Cleanup();
        return count;
    }
}

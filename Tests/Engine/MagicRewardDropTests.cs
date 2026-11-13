using FrierenMod.Scripts.Shared.Compatibility;
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
    public static async Task<int> RunBlitz(Node root)
    {
        int checks = 0;
        void Check(bool ok, string text) { if (!ok) throw new Exception(text); checks++; GD.Print("PASS 极速：" + text); }
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], gameMode: GameMode.Blitz, seed: "FRIEREN-BLITZ");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = 1;
        run.PushRoom(new RestSiteRoom());
        CommissionCmd.Ensure(run);
        Check(!CommissionCmd.Enabled(run) && CommissionCmd.State(player).Quests.Count == 0 &&
            string.IsNullOrEmpty(CommissionCmd.Data.Get(player)), "极速开局不生成或保存委托");
        var host = new Control(); root.AddChild(host);
        FrierenMod.Scripts.UI.CommissionScreen.Attach(host, player, () => null);
        Check(host.FindChild("FrierenCommissionButton", true, false) == null, "极速模式不创建委托入口");
        var legacy = new CommissionState { CurrentAct = 0, Quests = [
            new() { Id = 10001, Act = 0, Stars = 3, Template = "HUNT", Status = CommissionStatus.Accepted,
                Goal = CommissionGoal.EmpoweredElite, Progress = 1, Reward = new(CommissionRewardKind.Gold, "", 999) }] };
        CommissionCmd.Data.Set(player, legacy.ToJson());
        Check(CommissionCmd.State(player).Quests.Count == 0 &&
            !await CommissionCmd.Execute(player, 10001, CommissionOperation.Claim, legacy.Revision),
            "旧极速存档中的委托也不可领取");
        var deck = MagicDeckState.CreatePrototypeLoadout(); deck.RewardPity = -10000;
        MagicDeckStore.Write(player, deck);
        bool allDrop = true, sameRarityRule = true;
        for (int battle = 0; battle < 20; battle++)
        {
            int previousOffset = MagicDeckStore.Read(player).RewardRarityOffset;
            var room = new CombatRoom(ModelDb.Encounter<TurretOperatorWeak>().ToMutable(), run);
            await Hook.AfterCombatVictory(run, room.CombatState, room);
            var reward = room.ExtraRewards.GetValueOrDefault(player)?.OfType<FactorReward>().SingleOrDefault();
            allDrop &= reward != null;
            if (reward != null)
            {
                var expected = MagicRewardRules.Offer(previousOffset, GameApi.CreateRng((uint)reward.Seed).NextInt);
                sameRarityRule &= expected.Choices.SequenceEqual(reward.Choices) &&
                    MagicDeckStore.Read(player).RewardRarityOffset == expected.RarityOffset;
            }
            await Hook.AfterCombatVictory(run, room.CombatState, room);
            allDrop &= room.ExtraRewards.GetValueOrDefault(player)?.OfType<FactorReward>().Count() == 1;
        }
        Check(allDrop, "极速20场胜利每场必掉一次魔法牌组奖励，重复通知不多发");
        Check(sameRarityRule, "每次奖励沿用原有稀有度公式及补偿");
        Check(MagicDeckStore.Read(player).RewardPity == -10000, "极速掉落不受普通模式掉落补偿影响");
        Check(CommissionCmd.Read(player).Quests.Single().Status == CommissionStatus.Accepted, "战斗胜利不推进旧极速委托");
        host.QueueFree(); RunManager.Instance.CleanUp();
        return checks;
    }

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
        string stateAfter=MagicDeckStore.Read(player).ToJson();int rngAfter=CombatTestState.RngCounter(player.PlayerRng.Rewards);
        await Victory(room);
        Check(room.ExtraRewards[player].Count==1 && MagicDeckStore.Read(player).ToJson()==stateAfter && CombatTestState.RngCounter(player.PlayerRng.Rewards)==rngAfter,
            "同场胜利重复通知不会重复掉落或推进随机数");
        await RelicCmd.Remove(player.GetRelic<HolyEmblem>()!);SetPity(100);
        var withoutEmblem=Room<TurretOperatorWeak>();await Victory(withoutEmblem);
        Check(withoutEmblem.ExtraRewards[player].OfType<FactorReward>().Count()==1,"移除圣徽后仍按角色正常掉落");
        SetPity(100);var boss=Room<CeremonialBeastBoss>();await Victory(boss);
        Check(boss.ExtraRewards[player].OfType<FactorReward>().Count()==1,"Boss 按一代普通房规则参与掉落");
        SetPity(-100);var miss=Room<TurretOperatorWeak>();await Victory(miss);
        Check(!miss.ExtraRewards.ContainsKey(player) && MagicDeckStore.Read(player).RewardPity==-90,"未掉落时补偿增加10");
        int missRng=CombatTestState.RngCounter(player.PlayerRng.Rewards);await Victory(miss);
        Check(CombatTestState.RngCounter(player.PlayerRng.Rewards)==missRng && MagicDeckStore.Read(player).RewardPity==-90,"未掉落场次也不会重复判定");
        void Escape(CombatRoom battle) {
            var enemy=battle.CombatState.CreateCreature(ModelDb.Monster<MegaCrit.Sts2.Core.Models.Monsters.Axebot>().ToMutable(),CombatSide.Enemy,null);
            battle.CombatState.AddCreature(enemy);battle.CombatState.CreatureEscaped(enemy);
        }
        SetPity(100);var escaped=Room<TurretOperatorWeak>();Escape(escaped);await Victory(escaped);
        Check(!escaped.ExtraRewards.ContainsKey(player) && MagicDeckStore.Read(player).RewardPity==110,"普通战斗有敌人逃跑时不掉落");
        SetPity(100);var eventRoom=Room<TurretOperatorWeak>(true);Escape(eventRoom);await Victory(eventRoom);
        Check(eventRoom.ExtraRewards[player].OfType<FactorReward>().Count()==1,"事件战斗沿用一代掉落判定");

        int seed=Enumerable.Range(0,1000).First(s=> {
            var offer=MagicRewardRules.Offer(5,GameApi.CreateRng((uint)s).NextInt);
            return offer.Choices.Any(id=>MagicPropRules.All.Any(p=>p.Id==id)) && offer.Choices.Any(id=>SpellCatalog.All.Any(f=>f.Id==id));
        });
        var current=MagicDeckStore.Read(player);current.RewardRarityOffset=5;MagicDeckStore.Write(player,current);
        reward=new FactorReward(player){Seed=seed};reward.Populate();
        string[] offered=reward.Choices;stateAfter=MagicDeckStore.Read(player).ToJson();rngAfter=CombatTestState.RngCounter(player.PlayerRng.Rewards);
        reward.Populate();
        Check(reward.Choices.SequenceEqual(offered) && MagicDeckStore.Read(player).ToJson()==stateAfter && CombatTestState.RngCounter(player.PlayerRng.Rewards)==rngAfter,
            "混合奖励重复展开不重掷、不重复更新补偿");
        var save=reward.ToSerializable();
        string json=JsonSerializer.Serialize(save,JsonSerializationUtility.Options);
        var saved=JsonSerializer.Deserialize<SerializableReward>(json,JsonSerializationUtility.Options)!;
        var restored=(FactorReward)Reward.FromSerializable(saved,player);
        Check(json.Contains("FrierenMagicItemChoices") && restored.Choices.SequenceEqual(offered),"BaseLib 扩展 JSON 保存完整混合选项");
        Check(MagicDeckStore.Read(player).ToJson()==stateAfter && CombatTestState.RngCounter(player.PlayerRng.Rewards)==rngAfter,
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

        var selector=new TestCardSelector();using var choices=CombatTestState.UseSelector(selector);
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

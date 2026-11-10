using BaseLib.Utils;
using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Events;
using FrierenMod.Scripts.Mechanics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;

namespace FrierenMod.Scripts.Presentation;

// 条件逐项来自作者的二代背景解锁表。平台统计可补发的项目直接读取原生档案。
public static class NewSlotBackAchievements
{
    private static readonly SavedSpireField<Player,string> HealBattle=new(()=>"","FrierenAchievementHealBattle");
    private static readonly SavedSpireField<Player,int> Healed=new(()=>0,"FrierenAchievementHealed");

    public static void ReconcileProfile()
    {
        if(!PresentationSettings.Achievements)return;
        var progress=SaveManager.Instance.Progress;
        var character=ModelDb.Character<Frieren>().Id;
        if(progress.CharacterStats.TryGetValue(character,out var stats)) {
            if(stats.TotalWins+stats.TotalLosses>=2)SlotBackStore.Unlock("8001");
            if(stats.TotalWins>=5)SlotBackStore.Unlock("8004");
            if(stats.TotalWins>=10)SlotBackStore.Unlock("8012");
            if(stats.FastestWinTime is >=0 and <=1800)SlotBackStore.Unlock("8010");
        }
        var seen=progress.DiscoveredCards;
        bool Obtained(CardModel card)=>progress.CardStats.TryGetValue(card.Id,out var cardStats) &&
            (cardStats.TimesPicked>0 || cardStats.TimesWon>0 || cardStats.TimesLost>0);
        if(Obtained(ModelDb.Card<FrierenTheSlayer>()))SlotBackStore.Unlock("8006");
        if(Obtained(ModelDb.Card<EndlessFlow>()))SlotBackStore.Unlock("8020");
        var legendary=ModelDb.AllCards.Where(c=>c is ILegendaryCard && c.ShouldShowInCardLibrary).ToArray();
        if(legendary.Length>0 && legendary.All(c=>seen.Contains(c.Id)))SlotBackStore.Unlock("8018");
        foreach(var (monster,id) in new[]{(ModelDb.Monster<HunterKiller>().Id,"8022"),(ModelDb.Monster<InfestedPrism>().Id,"8023")})
            if(progress.EnemyStats.TryGetValue(monster,out var enemy) && enemy.FightStats.Any(s=>s.Character==character && s.Wins>0))SlotBackStore.Unlock(id);
    }

    public static void Equipped(Player player,MagicDeckState deck)
    {
        if(!FrierenAchievements.Applies(player))return;
        var factors=deck.Items.Where(i=>i.Slot>=0).Select(i=>i.SpellId).ToHashSet();
        if(factors.Contains("Hypnosis")&&factors.Contains("BlackGoldWings"))FrierenAchievements.Unlock(player,"8003");
    }

    public static void CheckCards(Player player)
    {
        if(!FrierenAchievements.Applies(player))return;
        if(player.Deck.Cards.Any(c=>c is FrierenTheSlayer))FrierenAchievements.Unlock(player,"8006");
        if(player.Deck.Cards.Any(c=>c is EndlessFlow))FrierenAchievements.Unlock(player,"8020");
        Commissions(player,CommissionCmd.Read(player));
    }

    public static void RunVictory(Player player,double seconds)
    {
        if(seconds<=1800)FrierenAchievements.Unlock(player,"8010");
        if(player.Deck.Cards.Count(FrierenCardFrames.IsShared)>=3)FrierenAchievements.Unlock(player,"8021");
    }

    public static void Commissions(Player player,CommissionState state)
    {
        if(!FrierenAchievements.Applies(player))return;
        // 同一局开始时间由原生存档恢复；同一委托反复刷新、读档、领奖只计一次。
        long start=(long)AccessTools.Field(typeof(RunManager),"_startTime").GetValue(RunManager.Instance)!;
        var completed=state.Quests.Where(q=>!q.Legacy &&
            (q.Completed || q.Status==CommissionStatus.Claimed ||
             q.Status==CommissionStatus.Accepted && CommissionState.Ready(q,s=>CommissionCmd.OwnsSpell(player,s)))).ToArray();
        int count=SlotBackStore.RecordCommissions(completed.Select(q=>$"{start}:{player.RunState.Rng.StringSeed}:{player.NetId}:{q.Id}"));
        if(count>=1)FrierenAchievements.Unlock(player,"8014");
        if(count>=20)FrierenAchievements.Unlock(player,"8009");
        if(completed.Any(q=>q.Goal==CommissionGoal.EmpoweredElite))FrierenAchievements.Unlock(player,"8013");
        if(player.Gold>999)FrierenAchievements.Unlock(player,"8016");
    }

    public static void HealedInCombat(Creature creature,int previousHp)
    {
        if(creature.Player is not {} player || !FrierenAchievements.Applies(player) || !CombatManager.Instance.IsInProgress ||
            creature.CombatState?.RunState.CurrentRoom is not CombatRoom room || creature.CurrentHp<=previousHp)return;
        string key=CommissionCmd.BattleKey(player.RunState,room);
        if(HealBattle.Get(player)!=key){HealBattle.Set(player,key);Healed.Set(player,0);}
        int healed=Healed.Get(player)+creature.CurrentHp-previousHp;Healed.Set(player,healed);
        if(healed>=20)FrierenAchievements.Unlock(player,"8008");
    }

    public static void Victory(CombatRoom room)
    {
        if(MagicExamEscape.IsEscaped(room.CombatState))return;
        foreach(var p in room.CombatState.Players.Where(p=>FrierenAchievements.Applies(p)&&p.Creature.IsAlive)) {
            if(room.RoomType==RoomType.Boss && p.PlayerCombatState?.TurnNumber==1)FrierenAchievements.Unlock(p,"8007");
            if(room.Encounter.MonstersWithSlots.Any(m=>m.Item1 is HunterKiller))FrierenAchievements.Unlock(p,"8022");
            if(room.Encounter.MonstersWithSlots.Any(m=>m.Item1 is InfestedPrism))FrierenAchievements.Unlock(p,"8023");
        }
    }

    [HarmonyPatch(typeof(Creature),nameof(Creature.HealInternal))]
    private static class Healing {
        private static void Prefix(Creature __instance,out int __state)=>__state=__instance.CurrentHp;
        private static void Postfix(Creature __instance,int __state)=>HealedInCombat(__instance,__state);
    }
    [HarmonyPatch(typeof(Hook),nameof(Hook.AfterGoldGained))]
    private static class Gold {
        private static void Postfix(Player player,ref Task __result)=>__result=After(__result,player);
        private static async Task After(Task task,Player p){await task;if(p.Gold>999)FrierenAchievements.Unlock(p,"8016");}
    }
    [HarmonyPatch(typeof(EventModel),nameof(EventModel.BeginEvent))]
    private static class Story {
        private static void Postfix(EventModel __instance,ref Task __result)=>__result=After(__result,__instance);
        private static async Task After(Task task,EventModel story){
            await task;
            if(story.Owner is {} p && story is AnimalWell or HollowKnightAdventure)FrierenAchievements.Unlock(p,"8002");
        }
    }
    [HarmonyPatch(typeof(Hook),nameof(Hook.AfterCombatVictory))]
    private static class CombatVictory {
        private static void Postfix(CombatRoom room,ref Task __result)=>__result=After(__result,room);
        private static async Task After(Task task,CombatRoom room){await task;Victory(room);}
    }
    [HarmonyPatch(typeof(Hook),nameof(Hook.AfterCardChangedPiles))]
    private static class Cards {
        private static void Postfix(CardModel card,ref Task __result)=>__result=After(__result,card);
        private static async Task After(Task task,CardModel card){await task;if(card.Pile?.Type==PileType.Deck)CheckCards(card.Owner);}
    }
    [HarmonyPatch(typeof(ProgressSaveManager),nameof(ProgressSaveManager.MarkCardAsSeen))]
    private static class Encyclopedia {
        private static void Postfix(CardModel card){
            if(card.IsMutable && card.Owner is {} p && FrierenAchievements.Applies(p))ReconcileProfile();
        }
    }
}

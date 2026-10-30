using FrierenMod.Scripts.Mechanics;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Powers;

public sealed class PaperPlanePower : FrierenPower
{
    private static readonly HarmonyLib.AccessTools.FieldRef<MegaCrit.Sts2.Core.Combat.History.CombatHistoryEntry, Dictionary<ulong,int>> PlayerTurns =
        HarmonyLib.AccessTools.FieldRefAccess<MegaCrit.Sts2.Core.Combat.History.CombatHistoryEntry, Dictionary<ulong,int>>("_playerTurnNumbers");
    [SavedProperty]
    public bool AllowEqual { get; set; }
    public override PowerStackType StackType=>PowerStackType.None;
    public static int TurnRecord(Player? player,bool previous=false)
    {
        if(player?.Creature.CombatState==null)return 0;
        int current=0,record=0;
        foreach(var entry in CombatManager.Instance.History.CardPlaysStarted.Where(e=>e.Actor==player.Creature &&
            (previous?e.HappenedLastPlayerTurn(player):e.HappenedThisTurn(player.Creature.CombatState)))) {
            current=ManaCmd.IsMana(entry.CardPlay.Card)?current+1:0;
            record=Math.Max(record,current);
        }
        return record;
    }
    public int CurrentRecord=>TurnRecord(Owner?.Player);
    public static int BattleRecord(Player? player)
    {
        if(player?.Creature.CombatState is not {} combat)return 0;
        int current=0,record=0,turn=-1;
        foreach(var entry in CombatManager.Instance.History.CardPlaysStarted.Where(e=>e.Actor==player.Creature && !e.HappenedThisTurn(combat))) {
            // 每个回合重新开始计连续次数，不能把跨回合的魔力拼成一串。
            if(!PlayerTurns(entry).TryGetValue(player.NetId,out int entryTurn))continue;
            if(entryTurn!=turn){current=0;turn=entryTurn;}
            current=ManaCmd.IsMana(entry.CardPlay.Card)?current+1:0;
            record=Math.Max(record,current);
        }
        return record;
    }
    public int PreviousRecord=>BattleRecord(Owner?.Player);
    public override int DisplayAmount=>CurrentRecord;
    public override LocString Description { get {var text=base.Description;text.Add("Current",CurrentRecord);text.Add("Previous",PreviousRecord);text.Add("AllowEqual",AllowEqual);return text;} }
    internal decimal DamageMultiplier(Creature? target,decimal amount,ValueProp props,Creature? dealer,CardModel? source,CardPlay? play)=>
        dealer==Owner && source!=null && props.IsPoweredAttack() &&
        (AllowEqual?CurrentRecord>=PreviousRecord:CurrentRecord>PreviousRecord)?2:1;
    public override Task AfterCardPlayed(PlayerChoiceContext context,CardPlay play)
    {
        if(play.Card.Owner==Owner.Player)InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }
    public override Task AfterPlayerTurnStart(PlayerChoiceContext context,Player player)
    {
        if(player==Owner.Player)InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }
}

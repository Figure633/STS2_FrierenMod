using MegaCrit.Sts2.Core.Combat;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

public static class CombatTestState
{
    public static void SetInProgress(bool active)
    {
        var oldProperty=AccessTools.Property(typeof(CombatManager),"IsInProgress");
        if(oldProperty.SetMethod!=null) { oldProperty.SetValue(CombatManager.Instance,active);return; }
        var state = AccessTools.Field(typeof(CombatManager), "_turnState").GetValue(CombatManager.Instance);
        if (state == null) { if (active) throw new InvalidOperationException("先建立测试战斗"); return; }
        AccessTools.Property(state.GetType(), "IsInProgress").SetValue(state, active);
    }
    public static CardPlay BindPlayer(CardPlay play)
    { AccessTools.Property(typeof(CardPlay),"Player")?.SetValue(play,play.Card.Owner);return play; }
    public static CardPlay NewPlay(CardModel card, MegaCrit.Sts2.Core.Entities.Creatures.Creature? target,
        PileType resultPile, ResourceInfo resources, bool isAutoPlay=false, int playIndex=0, int playCount=1)
    {
        // 不能在对象初始化器中引用只存在于新版的 required Player，也不能在新版漏填它。
        var play=Activator.CreateInstance<CardPlay>();
        foreach(var (name,value) in new (string,object?)[]{("Card",card),("Target",target),("ResultPile",resultPile),
            ("Resources",resources),("IsAutoPlay",isAutoPlay),("PlayIndex",playIndex),("PlayCount",playCount)})
            AccessTools.Property(typeof(CardPlay),name).SetValue(play,value);
        return BindPlayer(play);
    }
    public static Task BeginEvent(EventModel model, Player player, bool finished=false)
    {
        var method=AccessTools.Method(typeof(EventModel),"BeginEvent");
        return (Task)method.Invoke(model,method.GetParameters().Length==3?[player,null,finished]:[player,finished])!;
    }
    public static Task EndTurn(MegaCrit.Sts2.Core.Combat.ICombatState combat, CombatSide side,
        IEnumerable<MegaCrit.Sts2.Core.Entities.Creatures.Creature> participants) =>
        (Task)(AccessTools.Method(typeof(MegaCrit.Sts2.Core.Hooks.Hook),"AfterSideTurnEnd") ??
            AccessTools.Method(typeof(MegaCrit.Sts2.Core.Hooks.Hook),"AfterTurnEnd")).Invoke(null,[combat,side,participants])!;
    public static int RngCounter(object rng) => (int)AccessTools.Field(rng.GetType(),"_counter").GetValue(rng)!;
    public static IDisposable UseSelector(MegaCrit.Sts2.Core.TestSupport.ICardSelector selector)
    {
        var method=AccessTools.Method(typeof(MegaCrit.Sts2.Core.Commands.CardSelectCmd),"UseSelector");
        return (IDisposable)method.Invoke(null,method.GetParameters().Length==1?[selector]:[selector,false])!;
    }
    public static void CompleteUnlocks()
    {
        var progress=MegaCrit.Sts2.Core.Saves.SaveManager.Instance.Progress;
        if(AccessTools.Method(progress.GetType(),"GrantNextUnlock") is {} grant) {while(grant.Invoke(progress,null)!=null){} }
        else AccessTools.Property(progress.GetType(),"TotalUnlocks").SetValue(progress,18);
    }
}

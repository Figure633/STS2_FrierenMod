using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using HarmonyLib;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace FrierenMod.Scripts.Events;

public sealed class SummonDragon : FrierenStoryEvent
{
    private Task? _resolving;
    protected override string[] Portraits => ["SummonDragon"];
    public override bool IsAllowed(IRunState runState)=>false;
    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var collector=DragonSummoning.Collector(Owner!.RunState);
        if(collector!=Owner) return [Choice("WAIT",()=>Task.CompletedTask,false)];
        return [Choice("LIFE",()=>Wish("LIFE")),Choice("ENERGY",()=>Wish("ENERGY")),
            new(this,()=>Wish("JOY"),$"{Id.Entry}.pages.INITIAL.options.JOY",HoverTipFactory.FromRelic<TheCourier>())];
    }
    public override async Task AfterEventStarted()
    {
        if(DragonSummoning.Collector(Owner!.RunState) is {} collector && DragonBallStore.Read(collector).WishUsed)
            await ((SummonDragon)RunManager.Instance.EventSynchronizer.GetEventForPlayer(collector)).ResolveWish();
    }
    internal async Task Wish(string wish)
    {
        if(wish is not ("LIFE" or "ENERGY" or "JOY")) return;
        if(DragonSummoning.Collector(Owner!.RunState)!=Owner || !DragonBallStore.ConsumeForWish(Owner,wish)) return;
        await ResolveWish();
    }
    private Task ResolveWish()=>_resolving??=GrantWish();
    private async Task GrantWish()
    {
        var owner=Owner!;
        var state=DragonBallStore.Read(owner);
        if(state.WishResolved){DragonSummoning.ReturnToCamp(owner);return;}
        switch(state.Wish) {
            case "LIFE": await CreatureCmd.SetMaxHp(owner.Creature,9999); await CreatureCmd.SetCurrentHp(owner.Creature,9999); break;
            case "ENERGY": owner.MaxEnergy=999; break;
            case "JOY":
                // 原生命令的后置钩子可能让出一帧；中途保存后只补尚未到账的奖励。
                if(owner.Gold<=state.WishGoldBefore)await PlayerCmd.GainGold(9999,owner);
                if(owner.Relics.OfType<TheCourier>().Count()<=state.WishCourierBefore)await RelicCmd.Obtain<TheCourier>(owner);
                break;
            default: throw new InvalidOperationException("未知神龙愿望："+state.Wish);
        }
        state=DragonBallStore.Read(owner);state.WishResolved=true;DragonBallStore.Write(owner,state);
        DragonSummoning.ReturnToCamp(owner);
    }
    [HarmonyPatch(typeof(Hook),nameof(Hook.ModifyMaxEnergy))]
    private static class InfiniteEnergy
    {
        private static void Postfix(Player player,ref decimal __result)
        { if(DragonBallStore.Read(player).Wish=="ENERGY") __result=999; }
    }
}

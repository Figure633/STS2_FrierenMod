using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace FrierenMod.Scripts.Powers;

public sealed class OpenTheWaygatePower : FrierenPower
{
    private List<int>? _requirements;
    private IReadOnlyList<int> Requirements=>_requirements ?? (IReadOnlyList<int>)Array.Empty<int>();
    [SavedProperty]
    public string Opportunities { get=>string.Join(",",Requirements);set{AssertMutable();_requirements=value.Split(',',StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();} }
    [SavedProperty]
    public int PendingTurns {get;set;}
    [SavedProperty]
    public int Consecutive {get;set;}
    public int RemainingOpportunities=>Requirements.Count;
    public override int DisplayAmount=>RemainingOpportunities+PendingTurns;
    public override PowerStackType StackType=>PowerStackType.None;
    public override LocString Description {get {var text=base.Description;text.Add("Remaining",RemainingOpportunities);text.Add("Pending",PendingTurns);text.Add("Consecutive",Consecutive);text.Add("Threshold",Requirements.FirstOrDefault());text.Add("HasOpportunity",RemainingOpportunities>0);return text;} }
    public void AddOpportunity(int threshold)
    {
        AssertMutable();(_requirements??=[]).Add(Math.Max(1,threshold));InvokeDisplayAmountChanged();
    }
    public override Task AfterApplied(Creature? applier,CardModel? cardSource)
    {
        AddOpportunity(Amount);return Task.CompletedTask;
    }
    public override Task AfterCardPlayed(PlayerChoiceContext context,CardPlay play)
    {
        if(play.Card.Owner!=Owner.Player || Requirements.Count==0)return Task.CompletedTask;
        Consecutive=ManaCmd.IsMana(play.Card)?Consecutive+1:0;
        if(Consecutive>=Requirements[0]) {
            _requirements!.RemoveAt(0);Consecutive=0;PendingTurns++;
            Flash();
            if(NCombatRoom.Instance is {} room) {
                if(NPowerFlashVfx.Create(this) is {} flash)room.CombatVfxContainer.AddChild(flash);
                if(NThoughtBubbleVfx.Create(new LocString("frieren","WAYGATE_EXTRA_TURN").GetFormattedText(),Owner,2) is {} notice)
                    room.CombatVfxContainer.AddChild(notice);
            }
        }
        InvokeDisplayAmountChanged();return Task.CompletedTask;
    }
    public override bool ShouldTakeExtraTurn(Player player)=>player==Owner.Player&&PendingTurns>0;
    public override async Task AfterTakingExtraTurn(Player player)
    {
        if(player!=Owner.Player || PendingTurns==0)return;
        PendingTurns--;
        if(PendingTurns==0&&Requirements.Count==0)await PowerCmd.Remove(this);
        else InvokeDisplayAmountChanged();
    }
    public override Task AfterPlayerTurnStart(PlayerChoiceContext context,Player player)
    {
        if(player==Owner.Player){Consecutive=0;InvokeDisplayAmountChanged();}
        return Task.CompletedTask;
    }
}

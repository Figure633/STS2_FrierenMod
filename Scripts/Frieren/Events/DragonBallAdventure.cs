using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Potions;
using FrierenMod.Scripts.Shared.Compatibility;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Events;

public sealed class DragonBallAdventure : FrierenStoryEvent
{
    protected override string[] Portraits => ["PreviewDragonBall"];
    public override bool IsShared => true;
    // 单独按未知房间累计概率，不加入原生均匀随机事件池。
    public override bool IsAllowed(IRunState runState) => false;
    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var trial = Choice("KORIN",Trial); trial.Description.Add("Hp",100 * (Owner!.RunState.CurrentActIndex + 1));
        trial.HoverTips=[HoverTipFactory.FromPotion<SenzuBean>()];
        return [trial,CardChoice<Kamehameha>("TURTLE",Turtle,Owner.Deck.Cards.Any(c => c is Famehameha && c.IsTransformable)),
            CardChoice<DragonBall1>("STEAL",Steal,!DragonBallStore.Read(Owner).Started).ThatDoesDamage(10)];
    }
    private Task Trial()
    {
        var encounter = (BattlewornDummyEventEncounter)ModelDb.Encounter<BattlewornDummyEventEncounter>().ToMutable();
        encounter.Setting = BattlewornDummyEventEncounter.DummySetting.Setting1;
        EnterCombatWithoutExitingEvent(encounter,[],shouldResumeAfterCombat:true); return Task.CompletedTask;
    }
    public override async Task Resume(AbstractRoom room)
    {
        bool failed = ((BattlewornDummyEventEncounter)((CombatRoom)room).Encounter).RanOutOfTime;
        Finish(failed ? "FAILURE" : "KORIN",0);
        if (!failed) await RewardsCmd.OfferCustom(Owner!,[new PotionReward(ModelDb.Potion<SenzuBean>().ToMutable(),Owner!)]);
    }
    internal static CardModel TurtleReplacement(CardModel original)
    {
        var replacement = original.Owner.RunState.CreateCard<Kamehameha>(original.Owner);
        if (original.IsUpgraded) { replacement.UpgradeInternal(); replacement.FinalizeUpgradeInternal(); }
        if (original.Enchantment is {} enchantment) {
            var save = enchantment.ToSerializable();
            replacement.EnchantInternal(EnchantmentModel.FromSerializable(save),save.Amount);
        }
        return replacement;
    }
    private async Task Turtle()
    {
        var sources = Owner!.Deck.Cards.Where(c => c is Famehameha && c.IsTransformable).ToArray();
        if (sources.Length == 0) { Finish("TURTLE",0); return; }
        var original = (await CardSelectCmd.FromSimpleGrid(new BlockingPlayerChoiceContext(),sources,Owner,
            new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt,1))).SingleOrDefault();
        if (original != null) await CardCmd.Transform(original,TurtleReplacement(original),CardPreviewStyle.EventLayout);
        Finish("TURTLE",0);
    }
    private async Task Steal()
    {
        if (DragonBallStore.Read(Owner!).Started) { Finish("STEAL",0); return; }
        await GameApi.Damage(new ThrowingPlayerChoiceContext(),Owner!.Creature,10,ValueProp.Unblockable | ValueProp.Unpowered,null,null);
        if (Owner.Creature.IsDead) return;
        DragonBallStore.GrantNext(Owner,true); Finish("STEAL",0);
    }

    [HarmonyPatch(typeof(RunManager),"CreateRoom")]
    private static class UnknownRoomChance
    {
        private static void Prefix(RunManager __instance, ref RoomType roomType, MapPointType mapPointType, ref AbstractModel? model)
        {
            if (model != null || mapPointType != MapPointType.Unknown || __instance.DebugOnlyGetState() is not {} run ||
                !Encounters.FrierenEncounterEligibility.IsFrierenRun(run)) return;
            var eligible = run.Players.Where(p => !DragonBallStore.Read(p).EventSeen && p.Deck.Cards.Any(c => c is Famehameha)).ToArray();
            if (eligible.Length == 0) return;
            if (eligible.All(p => DragonBallStore.Read(p).LastUnknownFloor == run.TotalFloor)) return;
            int chance = Math.Min(100,10 * (1 + eligible.Max(p => DragonBallStore.Read(p).EventMisses)));
            bool found = run.Rng.Niche.NextInt(100) < chance;
            foreach (var player in eligible) { var state = DragonBallStore.Read(player); state.LastUnknownFloor=run.TotalFloor; state.EventMisses++; DragonBallStore.Write(player,state); }
            if (!found) return;
            var adventure = ModelDb.Event<DragonBallAdventure>();
            foreach (var player in run.Players) { var state = DragonBallStore.Read(player); state.EventSeen=true; DragonBallStore.Write(player,state); }
            run.AddVisitedEvent(adventure); roomType=RoomType.Event; model=adventure;
        }
    }

    [HarmonyPatch(typeof(Hook),nameof(Hook.BeforeCombatStart))]
    private static class TrialHp
    {
        private static void Postfix(ICombatState? combatState,ref Task __result) => __result = SetHp(__result,combatState);
        private static async Task SetHp(Task original,ICombatState? combat)
        {
            await original;
            if (combat?.RunState.CurrentRoom is not CombatRoom room || room.ParentEventId != ModelDb.Event<DragonBallAdventure>().Id) return;
            foreach (var enemy in combat.Enemies) { await CreatureCmd.SetMaxHp(enemy,100 * (combat.RunState.CurrentActIndex + 1)); await CreatureCmd.SetCurrentHp(enemy,enemy.MaxHp); }
        }
    }
}

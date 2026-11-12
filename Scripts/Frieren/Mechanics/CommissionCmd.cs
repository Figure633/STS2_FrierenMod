using BaseLib.Utils;
using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Relics;
using FrierenMod.Scripts.Powers;
using FrierenMod.Scripts.Shared.Compatibility;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Map;
using System.Security.Cryptography;
using System.Text;

namespace FrierenMod.Scripts.Mechanics;

public static class CommissionCmd
{
    public static readonly SavedSpireField<Player,string> Data=new(()=>"","FrierenCommissions");
    private static readonly Type[] Potions=[typeof(FirePotion),typeof(BlockPotion),typeof(StrengthPotion),typeof(EnergyPotion)];
    public static readonly Type[] BossRelics=[typeof(ComfortableBed),typeof(HimmelGravestone),typeof(IcicleCherryBlossom),typeof(ScaleOfObedience)];
    private static readonly HashSet<Player> Pending=[];
    private static readonly HashSet<string> ManaMakers=["MultipleOffensiveMagic","ManaDetection","DomainExpansion","ContinualChant","ClearMind","ButtSlam","TreasureVaultSpell","EndlessFlow","Snuggle","ManaCollapse","Disperse","ManaParcel","RapidChant","GrapeSpell","ShavedIceSpell","GoodLuckSpell","ManaExpansion","TrueColours"];
    public static bool IsBusy(Player p)=>Pending.Contains(p);
    public static CommissionState Read(Player p){
        var json=Data.Get(p);if(string.IsNullOrEmpty(json))return new();
        var state=CommissionState.FromJson(json);
        if(state.MigrateGrimoireRewards())Data.Set(p,state.ToJson());
        return state;
    }
    public static void Write(Player p,CommissionState state){Data.Set(p,state.ToJson());Presentation.NewSlotBackAchievements.Commissions(p,state);}
    private static uint Seed(string text)=>BitConverter.ToUInt32(SHA256.HashData(Encoding.UTF8.GetBytes(text)),0);
    public static CommissionState State(Player p){
        var state=Read(p);if(p.Character is not Frieren)return state;int act=p.RunState.CurrentActIndex;
        Presentation.NewSlotBackAchievements.Commissions(p,state);
        // 原生地图历史保存进入前的问号类型；包括未接任务前的房间，且事件内战斗不重复计数。
        bool counted=state.SetUnknownRooms(act,p.RunState.MapPointHistory.ElementAtOrDefault(act)?.Count(e=>e.MapPointType==MapPointType.Unknown)??0);
        if(state.CurrentAct==act&&state.GeneratedActs.Contains(act)){if(counted)Write(p,state);return state;}
        var rng=GameApi.CreateRng(Seed($"frieren-commissions-v3/{p.RunState.Rng.StringSeed}/{p.NetId}/{act}"));
        int affix=(int)(Seed($"frieren-elite/{p.RunState.Rng.StringSeed}/{act}")%4);
        var common=ModelDb.RelicPool<SharedRelicPool>().AllRelics.Concat(p.Character.RelicPool.AllRelics).Where(r=>r.Rarity==RelicRarity.Common).DistinctBy(r=>r.Id).OrderBy(r=>r.Id.ToString()).Select(r=>r.GetType().Name).ToArray();
        var rares=ModelDb.CardPool<FrierenCardPool>().AllCards.Where(c=>c.Rarity==CardRarity.Rare&&FrierenCardAvailability.IsRandomCandidate(c)).OrderBy(c=>c.Id.ToString()).Select(c=>c.GetType().Name).ToArray();
        if(state.EnsureAct(act,rng.NextInt,rares,MagicPropRules.All.Select(x=>x.Id).ToArray(),Potions.Select(t=>t.Name).ToArray(),common,rares,BossRelics.Select(t=>t.Name).ToArray(),affix,s=>OwnsSpell(p,s),id=>p.Relics.Any(r=>r.GetType().Name==id))||counted)Write(p,state);
        return state;
    }
    public static CardModel Spell(string id)=>ModelDb.AllCards.First(c=>c.GetType().Name==id);
    public static RelicModel Relic(string id)=>ModelDb.AllRelics.First(r=>r.GetType().Name==id);
    public static PotionModel Potion(string id)=>ModelDb.GetById<PotionModel>(ModelDb.GetId(Potions.Single(t=>t.Name==id)));
    public static bool IsManaMaker(CardModel c)=>ManaMakers.Contains(c.GetType().Name)||c is CustomLegendarySpell spell&&spell.Effects.Any(e=>e.Kind.StartsWith("Mana",StringComparison.Ordinal));
    public static (int Mana,int Chants) TutorCounts(Player p)=>(p.Deck.Cards.Where(IsManaMaker).Select(c=>c.Id).Distinct().Count(),p.Deck.Cards.Count(ChantCmd.IsChant));
    public static bool OwnsSpell(Player p,string requirement){
        var cards=p.Deck.Cards;
        return requirement switch {
            "tutor"=>TutorCounts(p) is var n&&(n.Mana>=3||n.Chants>=4),
            "rare-pair"=>cards.Where(c=>c.Rarity==CardRarity.Rare).Select(c=>c.Id).Distinct().Count()>=2,
            "legendary-three"=>cards.Where(c=>c is ILegendaryCard).Select(c=>c.Id).Distinct().Count()>=3,
            "upgraded-five"=>cards.Count(c=>c.IsUpgraded)>=5,
            "upgraded-four"=>cards.Count(c=>c.IsUpgraded)>=4,
            "zero-three"=>cards.Where(c=>c.EnergyCost.Canonical==0&&!c.EnergyCost.CostsX&&c is not IManaCard).Select(c=>c.Id).Distinct().Count()>=3,
            "power-three"=>cards.Where(c=>c.Type==CardType.Power).Select(c=>c.Id).Distinct().Count()>=3,
            // 保留旧存档的条件键，实际门槛按作者新文案改为15种。
            "distinct-twelve"=>cards.Select(c=>c.Id).Distinct().Count()>=15,
            _=>cards.Any(c=>c.GetType().Name==requirement)
        };
    }
    public static int Floor(Player p)=>Math.Max(1,p.RunState is RunState r?r.ActFloor:1);
    public static bool CanAct(Player p,bool input=true){
        var m=RunManager.Instance;var c=CombatManager.Instance;
        return p.Character is Frieren&&ReferenceEquals(m.DebugOnlyGetState(),p.RunState)&&!m.IsCleaningUp&&!p.Creature.IsDead&&!c.IsStarting&&!c.IsEnding&&
            (!input||!MagicPropCmd.IsBusy(p))&&(!input||!DragonSummoning.IsTransitioning&&!Pending.Contains(p)&&!m.NetService.IsGameLoading&&LocalContext.IsMe(p)&&!m.ActionExecutor.IsRunning&&MagicPropCmd.AllowsUseOver(NOverlayStack.Instance?.Peek()))&&
            (!c.IsInProgress||!c.PlayerActionsDisabled&&!c.IsEnemyTurnStarted&&!c.EndingPlayerTurnPhaseOne&&!c.EndingPlayerTurnPhaseTwo&&(p.RunState.Players.Count==1||p.PlayerCombatState?.Phase==PlayerTurnPhase.Play));
    }
    public static bool CanReceive(Player p,CommissionReward r)=>r.Kind!=CommissionRewardKind.Potion||p.HasOpenPotionSlots&&Hook.ShouldProcurePotion(p.RunState,p.Creature.CombatState,Potion(r.Item).ToMutable(),p);
    public static async Task<bool> Send(Player p,int id,CommissionOperation op){
        if(!CanAct(p)||!Pending.Add(p))return false;
        try{bool ok=await MagicDeckNetwork.SendCommission(p,new(){Quest=id,Operation=op,Revision=State(p).Revision});if(ok&&op==CommissionOperation.Claim)await UI.MagicDeckAnimations.WhenIdle(p);return ok;}
        finally{Pending.Remove(p);}
    }
    public static async Task<bool> Execute(Player p,int id,CommissionOperation op,int revision,PlayerChoiceContext? context=null){
        if(!CanAct(p,false)||!Enum.IsDefined(op))return false;var state=State(p);if(state.Revision!=revision)return false;
        var q=state.Quests.SingleOrDefault(q=>q.Id==id);if(q==null||q.Legacy||q.Act!=p.RunState.CurrentActIndex&&!(op==CommissionOperation.Claim&&q.Completed))return false;
        if(op==CommissionOperation.Accept){
            if(!state.Accept(id,p.RunState.CurrentActIndex,Floor(p)))return false;Write(p,state);
            if(q.Goal==CommissionGoal.Cargo)await CommissionCargo.Add(p,id);return true;
        }
        if(op==CommissionOperation.Abandon){if(!state.Abandon(id))return false;Write(p,state);if(q.Goal==CommissionGoal.Cargo)await CommissionCargo.Remove(p,id);return true;}
        if(!CommissionState.Ready(q,s=>OwnsSpell(p,s))||!CanReceive(p,q.Reward)||!state.Claim(id,s=>OwnsSpell(p,s)))return false;
        Write(p,state);var reward=q.Reward;context??=new BlockingPlayerChoiceContext();
        bool duplicateRelic=false,received=true;
        int previousCards=reward.Kind==CommissionRewardKind.Card?p.Deck.Cards.Count(c=>c.GetType().Name==reward.Item):0;
        switch(reward.Kind){
            case CommissionRewardKind.Gold:await PlayerCmd.GainGold(reward.Amount,p);break;
            case CommissionRewardKind.Prop:GiveProp(p,reward.Item,reward.Amount);if(reward.ExtraProp.Length>0)GiveProp(p,reward.ExtraProp,1);break;
            case CommissionRewardKind.Relic:
                var relic=Relic(reward.Item);
                if(p.Relics.Any(r=>r.Id==relic.Id)){duplicateRelic=true;if(reward.FallbackProp.Length>0)GiveProp(p,reward.FallbackProp,1);else await PlayerCmd.GainGold(reward.DuplicateGold>=0?reward.DuplicateGold:reward.Amount,p);}
                else await RelicCmd.Obtain(relic.ToMutable(),p);break;
            case CommissionRewardKind.Potion:
                if(!(await PotionCmd.TryToProcure(Potion(reward.Item).ToMutable(),p)).success)return Restore();break;
            case CommissionRewardKind.Card:
            case CommissionRewardKind.RareChoice:
                var rewards = new List<Reward>();
                for (int i=0;i<reward.Amount;i++) {
                    var ids = reward.Kind==CommissionRewardKind.Card ? new[]{reward.Item} : reward.Choices??[];
                    if (ids.Length==0) return Restore();
                    var cards=ids.Select(id=>{
                        var card=p.RunState.CreateCard(Spell(id),p);
                        if(reward.Kind==CommissionRewardKind.Card)CardCmd.Upgrade(card);
                        return card;
                    }).ToArray();
                    rewards.Add(new CardReward(cards,CardCreationSource.Other,p,CardCreationOptions.ForRoom(p,RoomType.Monster)){CanReroll=false,CanSkip=true});
                }
                await GameApi.BeginPlayerChoice(context,p,PlayerChoiceOptions.None);
                try { await RewardsCmd.OfferCustom(p,rewards); }
                finally { await context.SignalPlayerChoiceEnded(); }
                if(reward.Kind==CommissionRewardKind.Card)received=p.Deck.Cards.Count(c=>c.GetType().Name==reward.Item)>previousCards;
                break;
        }
        int gold=q.BaseGold+(duplicateRelic&&reward.DuplicateGold>=0?0:reward.BonusGold);
        if(gold>0)await PlayerCmd.GainGold(gold,p);
        q.RewardReceived=received;if(received&&reward.UniqueKey.Length>0)state.ClaimedRewards.Add(reward.UniqueKey);state.Revision++;Write(p,state);
        if(q.Stars==3)DragonBallStore.GrantNext(p);
        return true;
        bool Restore(){q.Status=CommissionStatus.Accepted;state.Revision++;Write(p,state);return false;}
    }
    private static void GiveProp(Player p,string id,int count){var deck=MagicDeckStore.Read(p);for(int i=0;i<count;i++)deck.AddProp(id);MagicDeckStore.Write(p,deck);}
    public static string BattleKey(IRunState r,CombatRoom room)=>$"{r.CurrentActIndex}/{r.TotalFloor}/{room.ModelId}";
    public static string AffixText(int affix,int act,int players=1){var text=new LocString("frieren","COMMISSION.AFFIX."+affix);text.Add("Strength",2+act);text.Add("Armor",4+act*2);text.Add("Heal",3+act*2);return text.GetFormattedText();}
    public static async Task Empower(ICombatState? combat){
        if(combat?.RunState.CurrentRoom is not CombatRoom {RoomType:RoomType.Elite} room)return;
        var contracts=combat.Players.Where(p=>p.Character is Frieren&&p.Creature.IsAlive).Select(p=>(Player:p,State:State(p))).Where(x=>x.State.Active?.Goal==CommissionGoal.EmpoweredElite&&x.State.Active.Progress==0).ToArray();
        if(contracts.Length==0)return;string key=BattleKey(combat.RunState,room);
        foreach(var (p,state) in contracts)if(state.BindElite(key))Write(p,state);
        if(!contracts.Any(x=>x.State.Active?.EliteBattle==key))return;
        int affix=contracts[0].State.Active!.Affix;
        foreach(var enemy in combat.Enemies.Where(e=>e.IsAlive).ToArray()){
            if(enemy.GetPower<CommissionElitePower>()!=null)continue;
            var power=(CommissionElitePower)ModelDb.Power<CommissionElitePower>().ToMutable();power.Affix=affix;
            var context=new ThrowingPlayerChoiceContext();await PowerCmd.Apply(context,power,enemy,combat.RunState.CurrentActIndex+1,enemy,null);
            if(enemy.GetPower<CommissionElitePower>()==power)await power.Empower(context);
        }
    }
    public static void Victory(IRunState run,CombatRoom room){
        if(MagicExamEscape.IsEscaped(room.CombatState))return;string key=BattleKey(run,room);
        // 战后原生可能已经清空能力；是否强化过由绑定的开战记录判定。
        foreach(var p in room.CombatState.Players.Where(p=>p.Character is Frieren&&p.Creature.IsAlive)){
            var state=State(p);bool changed=false;
            if(room.RoomType==RoomType.Elite){changed|=state.WinElite(key);changed|=state.Record("elite/"+key,CommissionGoal.Elites);}
            if(p.PlayerCombatState?.TurnNumber==1)changed|=state.Record("first-turn/"+key,CommissionGoal.FirstTurnBattle);
            if(state.CombatKey==key&&!state.LostHp)changed|=state.Record("flawless/"+key,CommissionGoal.FlawlessBattle);
            if(changed)Write(p,state);
        }
    }
    public static void Ensure(IRunState run){foreach(var p in run.Players.Where(p=>p.Character is Frieren))State(p);}
    public static async Task Entered(IRunState run,AbstractRoom? room=null){Ensure(run);var current=room??run.CurrentRoom;if(current==null)return;foreach(var p in run.Players.Where(p=>p.Character is Frieren))await CommissionCargo.Visit(p,current);}
}
[HarmonyPatch(typeof(Hook),nameof(Hook.BeforeCombatStart))]
internal static class CommissionCombatHook{
    private static void Postfix(ICombatState? combatState,ref Task __result)=>__result=After(__result,combatState);
    private static async Task After(Task original,ICombatState? combat){await original;await CommissionCmd.Empower(combat);if(combat!=null)foreach(var p in combat.Players.Where(p=>p.Character is Frieren)){
        if(combat.RunState.CurrentRoom is CombatRoom room){var state=CommissionCmd.State(p);if(state.BeginCombat(CommissionCmd.BattleKey(combat.RunState,room)))CommissionCmd.Write(p,state);}
        await SealingOreCmd.Refresh(p);
    }}
}
[HarmonyPatch(typeof(Hook),nameof(Hook.AfterDamageReceived))]
internal static class CommissionDamageHook{
    private static void Prefix(Creature target,DamageResult result){
        CommissionCargo.Damaged(target,result);
        if(target.Player is {} p&&p.Character is Frieren&&result.UnblockedDamage>0&&target.CombatState?.RunState.CurrentRoom is CombatRoom room){
            var state=CommissionCmd.State(p);if(state.LoseHp(CommissionCmd.BattleKey(p.RunState,room)))CommissionCmd.Write(p,state);
        }
    }
}
[HarmonyPatch(typeof(Hook),nameof(Hook.AfterCombatVictory))]
internal static class CommissionVictoryHook{
    private static void Postfix(IRunState runState,CombatRoom room,ref Task __result)=>__result=After(__result,runState,room);
    private static async Task After(Task original,IRunState run,CombatRoom room){await original;CommissionCmd.Victory(run,room);}
}
[HarmonyPatch(typeof(Hook),nameof(Hook.AfterRoomEntered))]
internal static class CommissionRoomHook{
    private static void Postfix(IRunState runState,AbstractRoom room,ref Task __result)=>__result=After(__result,runState,room);
    private static async Task After(Task original,IRunState run,AbstractRoom room){await original;await CommissionCmd.Entered(run,room);foreach(var p in run.Players.Where(p=>p.Character is Frieren))await SealingOreCmd.Visit(p,room);}
}
[HarmonyPatch(typeof(Hook),nameof(Hook.AfterActEntered))]
internal static class CommissionActHook{
    private static void Postfix(IRunState runState,ref Task __result)=>__result=After(__result,runState);
    private static async Task After(Task original,IRunState run){await original;await CommissionCmd.Entered(run);}
}

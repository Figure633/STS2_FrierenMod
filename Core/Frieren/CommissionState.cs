using System.Text.Json;

namespace FrierenMod.Core;

// 旧枚举值保留给0.11.0存档。
public enum CommissionGoal { Monsters, Elites, Spell, FirstTurnBattle, FlawlessElite, FlawlessBattle, Battles, EmpoweredElite, Deck, Cargo, UnknownRooms }
public enum CommissionRewardKind { Gold, Prop, Potion, Relic, Card, RareChoice, Freight }
public enum CommissionStatus { Available, Accepted, Claimed, Abandoned }
public enum CommissionOperation { Accept, Claim, Abandon }
public sealed record CommissionReward(CommissionRewardKind Kind, string Item, int Amount, int BonusGold = 0,
    string[]? Choices = null, string FallbackProp = "", string ExtraProp = "", string UniqueKey = "", int DuplicateGold = -1);
public sealed class Commission
{
    public int Id { get; init; }
    public int Act { get; init; }
    public string Template { get; init; } = "";
    public int OpensAt { get; init; } = 1;
    public int Stars { get; init; } = 1;
    public CommissionGoal Goal { get; init; }
    public int Target { get; init; } = 1;
    public string Spell { get; init; } = "";
    public CommissionReward Reward { get; set; } = new(CommissionRewardKind.Gold, "", 80);
    public int Affix { get; init; }
    public string EliteBattle { get; set; } = "";
    public bool Legacy { get; set; }
    public bool Completed { get; set; }
    public int Progress { get; set; }
    public int PaidFreight { get; set; }
    public int BaseGold { get; init; }
    public bool RewardReceived { get; set; }
    public CommissionStatus Status { get; set; }
}
public sealed record VillageCommission(string Id, int Stars, string Requirement, bool Cargo = false,
    int Weight = 2, int MinAct = 0, CommissionGoal Goal = CommissionGoal.Deck, int Target = 1);

public sealed class CommissionState
{
    public const int ActiveLimit = 1;
    public int Version { get; set; } = 4;
    public int CurrentAct { get; set; } = -1;
    public string CombatKey { get; set; } = "";
    public bool LostHp { get; set; }
    public int Revision { get; set; }
    public List<int> GeneratedActs { get; set; } = [];
    public List<Commission> Quests { get; set; } = [];
    public HashSet<string> CountedEvents { get; set; } = [];
    public Dictionary<int,int> UnknownRoomsVisited { get; set; } = [];
    public HashSet<string> ClaimedRewards { get; set; } = [];
    public int ActiveCount => Quests.Count(q => !q.Legacy && q.Act == CurrentAct && q.Status == CommissionStatus.Accepted);
    public Commission? Active => Quests.SingleOrDefault(q => !q.Legacy && q.Act == CurrentAct && q.Status == CommissionStatus.Accepted);
    public static int QuestId(int act, int slot) => 10000 + act * 100 + slot;
    public static readonly VillageCommission[] Villagers = [
        new("GRAPES",1,"GrapeSpell",Weight:1), new("STATUE",1,"MoldSpell",Weight:1), new("WEDDING",1,"FlowerFieldSpell"),
        new("FESTIVAL",1,"TongueTwisterSpell",Weight:1), new("SUMMER",1,"ShavedIceSpell"), new("LOST",2,"AccessoriesSpell",Weight:1),
        new("TUTOR",2,"tutor",Weight:3), new("CARGO",2,"",true,Weight:3), new("EXHIBITION",2,"rare-pair"),
        new("LIBRARY",2,"legendary-three",Weight:1), new("LESSON",2,"upgraded-cards"), new("VARIETY",1,"distinct-twelve"),
        new("GRANNY",1,"",Weight:3,Goal:CommissionGoal.Elites),
        new("NAMUSAN",1,"",MinAct:1,Goal:CommissionGoal.FirstTurnBattle),
        new("STARK",2,"",Goal:CommissionGoal.FlawlessBattle,Target:3),
        new("GUIDE",2,"",Goal:CommissionGoal.UnknownRooms,Target:4)
    ];
    public bool EnsureAct(int act, Func<int,int> next, IReadOnlyList<string> spells, IReadOnlyList<string> props,
        IReadOnlyList<string> potions, IReadOnlyList<string> relics, IReadOnlyList<string>? rareCards = null,
        IReadOnlyList<string>? bossRelics = null, int affix = 0, Func<string,bool>? meets = null, Func<string,bool>? hasRelic = null)
    {
        if (act < 0) return false;
        bool changed = CurrentAct != act;
        foreach(var q in Quests.Where(q=>q.Act!=act && q.Status is CommissionStatus.Available or CommissionStatus.Accepted)) {
            if (q.Status == CommissionStatus.Accepted && Ready(q, meets ?? (_ => false))) q.Completed = true;
            else q.Status=CommissionStatus.Abandoned;
            changed=true;
        }
        CurrentAct = act;
        if(GeneratedActs.Contains(act)){if(changed)Revision++;return changed;}
        string Pick(IReadOnlyList<string> pool) => pool.Count>0 ? pool[next(pool.Count)] : throw new ArgumentException("委托报酬池不能为空");
        bool Received(string key) => ClaimedRewards.Contains(key) || Quests.Any(q=>q.Reward.Item==key&&(q.Status==CommissionStatus.Claimed&&q.RewardReceived||q.Status is CommissionStatus.Available or CommissionStatus.Accepted));
        T Weighted<T>(IReadOnlyList<(int Weight,T Value)> options) {int roll=next(options.Sum(o=>o.Weight));foreach(var o in options){if(roll<o.Weight)return o.Value;roll-=o.Weight;}throw new InvalidOperationException("无可用委托候选");}
        CommissionReward Prize(int stars){
            var pool=new List<(int Weight,Func<CommissionReward> Value)>();
            void Add(int weight,Func<CommissionReward> prize)=>pool.Add((weight,prize));
            if(stars==1){
                Add(2,()=>new(CommissionRewardKind.Gold,"",50));
                if(!Received("FolkGrimoire"))Add(1,()=>new(CommissionRewardKind.Card,"FolkGrimoire",1,UniqueKey:"FolkGrimoire"));
                Add(3,()=>new(CommissionRewardKind.Prop,"UnbelievableTool",1,ExtraProp:Pick(props)));
                Add(2,()=>new(CommissionRewardKind.Potion,Pick(potions),1));
            }else if(stars==2){
                Add(2,()=>new(CommissionRewardKind.Relic,Pick(relics),120));
                Add(2,()=>new(CommissionRewardKind.Gold,"",75));
                if(act<2&&!(hasRelic?.Invoke("MagicShopMembership")??false))Add(1,()=>new(CommissionRewardKind.Relic,"MagicShopMembership",120));
                if(act is 1 or 2&&!Received("RoyalGrimoire"))Add(2,()=>new(CommissionRewardKind.Prop,"RoyalGrimoire",1,UniqueKey:"RoyalGrimoire"));
            }else{
                Add(2,()=>new(CommissionRewardKind.RareChoice,"",1,Choices:ThreeRares()));
                if(act==0)Add(2,()=>new(CommissionRewardKind.Relic,"FakeFlammeGrimoire",200,DuplicateGold:200));
                Add(2,()=>new(CommissionRewardKind.Prop,Pick(MagicPropRules.CommissionOnly.Where(p=>p.Id!="RoyalGrimoire").Select(p=>p.Id).ToArray()),1));
                if(act==2&&bossRelics is {Count:>0})Add(1,()=>new(CommissionRewardKind.Relic,Pick(bossRelics),1,
                    FallbackProp:Pick(MagicPropRules.CommissionOnly.Where(p=>p.Id!="RoyalGrimoire").Select(p=>p.Id).ToArray())));
            }
            return Weighted(pool)();
        }
        string[] ThreeRares(){var pool=(rareCards??spells).Distinct().ToList();var result=new List<string>();while(result.Count<3&&pool.Count>0){int i=next(pool.Count);result.Add(pool[i]);pool.RemoveAt(i);}return result.ToArray();}
        if(!Quests.Any(q=>q.Id==QuestId(act,1)))Quests.Add(new(){Id=QuestId(act,1),Act=act,Template="HUNT",Stars=3,Goal=CommissionGoal.EmpoweredElite,Affix=affix,Reward=Prize(3),BaseGold=100});
        var villagers=Villagers.Where(v=>act>=v.MinAct&&!Quests.Any(q=>q.Template==v.Id&&(q.Completed||q.Status==CommissionStatus.Claimed||q.Act==act))).ToList();
        for(int slot=2;slot<=3;slot++){
            if(Quests.Any(q=>q.Id==QuestId(act,slot)))continue;
            var template=Weighted(villagers.Select(v=>(v.Weight,v)).ToArray());villagers.Remove(template);
            Quests.Add(new(){Id=QuestId(act,slot),Act=act,Template=template.Id,Stars=template.Stars,
                Goal=template.Cargo?CommissionGoal.Cargo:template.Goal,Spell=template.Requirement,Target=template.Target,
                Progress=template.Goal==CommissionGoal.UnknownRooms?Math.Min(template.Target,UnknownRoomsVisited.GetValueOrDefault(act)):0,
                Reward=template.Cargo?new(CommissionRewardKind.Freight,"",1):Prize(template.Stars),BaseGold=template.Stars==1?30:50});
        }
        GeneratedActs.Add(act);Revision++;return true;
    }
    public bool Accept(int id,int act,int floor){var q=Quests.SingleOrDefault(q=>q.Id==id);if(q==null||q.Legacy||q.Status!=CommissionStatus.Available||q.Act!=act||act!=CurrentAct||q.OpensAt>floor||ActiveCount>=ActiveLimit)return false;q.Status=CommissionStatus.Accepted;q.Progress=q.Goal==CommissionGoal.UnknownRooms?Math.Min(q.Target,UnknownRoomsVisited.GetValueOrDefault(act)):0;Revision++;return true;}
    public bool Abandon(int id){var q=Quests.SingleOrDefault(q=>q.Id==id);if(q?.Status!=CommissionStatus.Accepted)return false;q.Status=CommissionStatus.Abandoned;Revision++;return true;}
    public bool Record(string eventId,CommissionGoal goal,int count=1){
        if(goal is CommissionGoal.Spell or CommissionGoal.Deck or CommissionGoal.Cargo||count<=0||!CountedEvents.Add(eventId))return false;
        foreach(var q in Quests.Where(q=>!q.Legacy&&q.Act==CurrentAct&&q.Status==CommissionStatus.Accepted&&q.Goal==goal&&q.Progress<q.Target))q.Progress=Math.Min(q.Target,q.Progress+count);
        Revision++;return true;
    }
    public bool SetUnknownRooms(int act,int count){
        if(count<=UnknownRoomsVisited.GetValueOrDefault(act))return false;
        UnknownRoomsVisited[act]=count;
        foreach(var q in Quests.Where(q=>!q.Legacy&&q.Act==act&&q.Goal==CommissionGoal.UnknownRooms&&q.Status is CommissionStatus.Available or CommissionStatus.Accepted))
            q.Progress=Math.Min(q.Target,UnknownRoomsVisited[act]);
        Revision++;return true;
    }
    public static int ProgressOf(Commission q,Func<string,bool> meets) => q.Completed ? q.Target : q.Goal is CommissionGoal.Spell or CommissionGoal.Deck ? (meets(q.Spell)?1:0):q.Progress;
    public static bool Ready(Commission q,Func<string,bool> meets)=>!q.Legacy&&q.Status==CommissionStatus.Accepted&&q.Goal!=CommissionGoal.Cargo&&ProgressOf(q,meets)>=q.Target;
    public bool Claim(int id,Func<string,bool> meets){var q=Quests.SingleOrDefault(q=>q.Id==id);if(q==null||q.Act!=CurrentAct&&!q.Completed||!Ready(q,meets))return false;q.Status=CommissionStatus.Claimed;Revision++;return true;}
    public bool BindElite(string key){var q=Active;if(q?.Goal!=CommissionGoal.EmpoweredElite||q.EliteBattle.Length>0||q.Progress>0)return false;q.EliteBattle=key;Revision++;return true;}
    public bool WinElite(string key){var q=Active;if(q?.Goal!=CommissionGoal.EmpoweredElite||q.EliteBattle!=key)return false;return Record("empowered/"+key,CommissionGoal.EmpoweredElite);}
    public bool BeginCombat(string key){if(CombatKey==key)return false;CombatKey=key;LostHp=false;Revision++;return true;}
    public bool LoseHp(string key){BeginCombat(key);if(LostHp)return false;LostHp=true;Revision++;return true;}
    public bool MigrateGrimoireRewards()
    {
        var pending = Quests.Where(q => q.Status is CommissionStatus.Available or CommissionStatus.Accepted &&
            q.Reward.Kind == CommissionRewardKind.Relic && (q.Reward.Item == "FlammeGrimoire" ||
                q.Reward.Item == "FakeFlammeGrimoire" && q.Reward.BonusGold != 0)).ToArray();
        foreach (var q in pending) q.Reward = q.Reward with { Item = "FakeFlammeGrimoire", BonusGold = 0 };
        if (pending.Length > 0) Revision++;
        return pending.Length > 0;
    }
    public string ToJson()=>JsonSerializer.Serialize(this);
    public static CommissionState FromJson(string json){
        var s=JsonSerializer.Deserialize<CommissionState>(json)??throw new ArgumentException("委托存档为空");
        if(s.Version is not (1 or 2 or 3 or 4)||s.Revision<0||s.Quests==null||s.GeneratedActs==null||s.CountedEvents==null||s.UnknownRoomsVisited==null||s.ClaimedRewards==null||s.GeneratedActs.Distinct().Count()!=s.GeneratedActs.Count||s.Quests.Select(q=>q.Id).Distinct().Count()!=s.Quests.Count||
            s.Quests.Any(q=>q.Act<0||q.Target<=0||q.Progress<0||q.Progress>q.Target||!Enum.IsDefined(q.Goal)||!Enum.IsDefined(q.Status)||!Enum.IsDefined(q.Reward.Kind)||q.Reward.Amount<=0||q.Reward.BonusGold<0||q.BaseGold<0||q.Stars is <1 or >3||q.Affix is <0 or >3)||s.Version>=3&&s.ActiveCount>ActiveLimit)throw new ArgumentException("委托存档格式无效");
        if(s.Version<3){foreach(var q in s.Quests){q.Legacy=true;if(q.Status!=CommissionStatus.Claimed)q.Status=CommissionStatus.Abandoned;}s.GeneratedActs.Clear();s.CurrentAct=-1;s.Revision++;}
        else if(s.Version==3){
            // 已接受的合同保留原报酬；只重建本幕尚未接取的旧任务，且每个槽位仍只生成一次。
            s.Quests.RemoveAll(q=>q.Act==s.CurrentAct&&q.Status==CommissionStatus.Available);
            s.GeneratedActs.Remove(s.CurrentAct);s.Revision++;
        }
        if(s.Version<4)foreach(var q in s.Quests.Where(q=>q.Status==CommissionStatus.Claimed))q.RewardReceived=true;
        s.Version=4;return s;
    }
}

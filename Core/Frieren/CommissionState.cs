using System.Text.Json;

namespace FrierenMod.Core;

// 旧枚举值保留给0.11.0存档。
public enum CommissionGoal { Monsters, Elites, Spell, FirstTurnBattle, FlawlessElite, FlawlessBattle, Battles, EmpoweredElite, Deck, Cargo }
public enum CommissionRewardKind { Gold, Prop, Potion, Relic, Card, RareChoice, Freight }
public enum CommissionStatus { Available, Accepted, Claimed, Abandoned }
public enum CommissionOperation { Accept, Claim, Abandon }
public sealed record CommissionReward(CommissionRewardKind Kind, string Item, int Amount, int BonusGold = 0,
    string[]? Choices = null, string FallbackProp = "");
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
    public CommissionReward Reward { get; init; } = new(CommissionRewardKind.Gold, "", 80);
    public int Affix { get; init; }
    public string EliteBattle { get; set; } = "";
    public bool Legacy { get; set; }
    public bool Completed { get; set; }
    public int Progress { get; set; }
    public int PaidFreight { get; set; }
    public CommissionStatus Status { get; set; }
}
public sealed record VillageCommission(string Id, int Stars, string Requirement, bool Cargo = false);

public sealed class CommissionState
{
    public const int ActiveLimit = 1;
    public int Version { get; set; } = 3;
    public int CurrentAct { get; set; } = -1;
    public string CombatKey { get; set; } = "";
    public bool LostHp { get; set; }
    public int Revision { get; set; }
    public List<int> GeneratedActs { get; set; } = [];
    public List<Commission> Quests { get; set; } = [];
    public HashSet<string> CountedEvents { get; set; } = [];
    public int ActiveCount => Quests.Count(q => !q.Legacy && q.Act == CurrentAct && q.Status == CommissionStatus.Accepted);
    public Commission? Active => Quests.SingleOrDefault(q => !q.Legacy && q.Act == CurrentAct && q.Status == CommissionStatus.Accepted);
    public static int QuestId(int act, int slot) => 10000 + act * 100 + slot;
    public static readonly VillageCommission[] Villagers = [
        new("GRAPES",1,"GrapeSpell"), new("STATUE",1,"MoldSpell"), new("WEDDING",1,"FlowerFieldSpell"),
        new("TEA",1,"HotTeaSpell"), new("FESTIVAL",1,"TongueTwisterSpell"), new("COPPER",1,"RustCleanMagic"),
        new("SUMMER",1,"ShavedIceSpell"), new("LOST",1,"AccessoriesSpell"), new("SLEEP",1,"BabySleeping"),
        new("TUTOR",2,"tutor"), new("CARGO",2,"",true), new("EXHIBITION",2,"rare-pair"),
        new("LIBRARY",2,"legendary-three"), new("LESSON",2,"upgraded-five"), new("THRIFT",2,"zero-three"),
        new("SHELTER",2,"power-three"), new("VARIETY",2,"distinct-twelve")
    ];
    public bool EnsureAct(int act, Func<int,int> next, IReadOnlyList<string> spells, IReadOnlyList<string> props,
        IReadOnlyList<string> potions, IReadOnlyList<string> relics, IReadOnlyList<string>? rareCards = null,
        IReadOnlyList<string>? bossRelics = null, int affix = 0, Func<string,bool>? meets = null)
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
        CommissionReward Prize(int stars){
            if(stars==1)return next(4) switch {
                0=>new(CommissionRewardKind.Gold,"",80),
                1=>new(CommissionRewardKind.Card,"FolkGrimoire",1),
                2=>new(CommissionRewardKind.Prop,Pick(props),2),
                _=>new(CommissionRewardKind.Potion,Pick(potions),1,30)};
            if(stars==2)return next(3) switch {
                0=>new(CommissionRewardKind.Relic,Pick(relics),160),
                1=>new(CommissionRewardKind.Gold,"",160),
                _=>new(CommissionRewardKind.Card,"FolkGrimoire",2,60)};
            int kinds=act is 1 or 2&&bossRelics is {Count:>0}?3:2;
            return next(kinds) switch {
                0=>new(CommissionRewardKind.Prop,Pick(MagicPropRules.CommissionOnly.Select(p=>p.Id).ToArray()),1),
                1=>new(CommissionRewardKind.RareChoice,"",1,Choices:ThreeRares()),
                _=>new(CommissionRewardKind.Relic,Pick(bossRelics!),1,FallbackProp:Pick(MagicPropRules.CommissionOnly.Select(p=>p.Id).ToArray()))};
        }
        string[] ThreeRares(){var pool=(rareCards??spells).Distinct().ToList();var result=new List<string>();while(result.Count<3&&pool.Count>0){int i=next(pool.Count);result.Add(pool[i]);pool.RemoveAt(i);}return result.ToArray();}
        Quests.Add(new(){Id=QuestId(act,1),Act=act,Template="HUNT",Stars=3,Goal=CommissionGoal.EmpoweredElite,Affix=affix,Reward=Prize(3)});
        var villagers=Villagers.ToList();
        for(int slot=2;slot<=3;slot++){
            int i=next(villagers.Count);var template=villagers[i];villagers.RemoveAt(i);
            Quests.Add(new(){Id=QuestId(act,slot),Act=act,Template=template.Id,Stars=template.Stars,
                Goal=template.Cargo?CommissionGoal.Cargo:CommissionGoal.Deck,Spell=template.Requirement,
                Reward=template.Cargo?new(CommissionRewardKind.Freight,"",1):Prize(template.Stars)});
        }
        GeneratedActs.Add(act);Revision++;return true;
    }
    public bool Accept(int id,int act,int floor){var q=Quests.SingleOrDefault(q=>q.Id==id);if(q==null||q.Legacy||q.Status!=CommissionStatus.Available||q.Act!=act||act!=CurrentAct||q.OpensAt>floor||ActiveCount>=ActiveLimit)return false;q.Status=CommissionStatus.Accepted;q.Progress=0;Revision++;return true;}
    public bool Abandon(int id){var q=Quests.SingleOrDefault(q=>q.Id==id);if(q?.Status!=CommissionStatus.Accepted)return false;q.Status=CommissionStatus.Abandoned;Revision++;return true;}
    public bool Record(string eventId,CommissionGoal goal,int count=1){
        if(goal is CommissionGoal.Spell or CommissionGoal.Deck or CommissionGoal.Cargo||count<=0||!CountedEvents.Add(eventId))return false;
        foreach(var q in Quests.Where(q=>!q.Legacy&&q.Act==CurrentAct&&q.Status==CommissionStatus.Accepted&&q.Goal==goal&&q.Progress<q.Target))q.Progress=Math.Min(q.Target,q.Progress+count);
        Revision++;return true;
    }
    public static int ProgressOf(Commission q,Func<string,bool> meets) => q.Completed ? q.Target : q.Goal is CommissionGoal.Spell or CommissionGoal.Deck ? (meets(q.Spell)?1:0):q.Progress;
    public static bool Ready(Commission q,Func<string,bool> meets)=>!q.Legacy&&q.Status==CommissionStatus.Accepted&&q.Goal!=CommissionGoal.Cargo&&ProgressOf(q,meets)>=q.Target;
    public bool Claim(int id,Func<string,bool> meets){var q=Quests.SingleOrDefault(q=>q.Id==id);if(q==null||q.Act!=CurrentAct&&!q.Completed||!Ready(q,meets))return false;q.Status=CommissionStatus.Claimed;Revision++;return true;}
    public bool BindElite(string key){var q=Active;if(q?.Goal!=CommissionGoal.EmpoweredElite||q.EliteBattle.Length>0||q.Progress>0)return false;q.EliteBattle=key;Revision++;return true;}
    public bool WinElite(string key){var q=Active;if(q?.Goal!=CommissionGoal.EmpoweredElite||q.EliteBattle!=key)return false;return Record("empowered/"+key,CommissionGoal.EmpoweredElite);}
    public bool BeginCombat(string key){if(CombatKey==key)return false;CombatKey=key;LostHp=false;Revision++;return true;}
    public bool LoseHp(string key){BeginCombat(key);if(LostHp)return false;LostHp=true;Revision++;return true;}
    public string ToJson()=>JsonSerializer.Serialize(this);
    public static CommissionState FromJson(string json){
        var s=JsonSerializer.Deserialize<CommissionState>(json)??throw new ArgumentException("委托存档为空");
        if(s.Version is not (1 or 2 or 3)||s.Revision<0||s.Quests==null||s.GeneratedActs==null||s.CountedEvents==null||s.GeneratedActs.Distinct().Count()!=s.GeneratedActs.Count||s.Quests.Select(q=>q.Id).Distinct().Count()!=s.Quests.Count||
            s.Quests.Any(q=>q.Act<0||q.Target<=0||q.Progress<0||q.Progress>q.Target||!Enum.IsDefined(q.Goal)||!Enum.IsDefined(q.Status)||!Enum.IsDefined(q.Reward.Kind)||q.Reward.Amount<=0||q.Reward.BonusGold<0||q.Stars is <1 or >3||q.Affix is <0 or >3)||s.Version==3&&s.ActiveCount>ActiveLimit)throw new ArgumentException("委托存档格式无效");
        if(s.Version<3){foreach(var q in s.Quests){q.Legacy=true;if(q.Status!=CommissionStatus.Claimed)q.Status=CommissionStatus.Abandoned;}s.GeneratedActs.Clear();s.CurrentAct=-1;s.Revision++;}
        s.Version=3;return s;
    }
}

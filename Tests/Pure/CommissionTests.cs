using FrierenMod.Core;
using System.Text.Json;

public static class CommissionTests
{
    public static int Run()
    {
        int count=0;
        void Check(bool ok,string label){if(!ok)throw new Exception(label);count++;Console.WriteLine("PASS 委托："+label);}
        CommissionState New(int seed=0,int act=0){var rng=new Random(seed);var s=new CommissionState();s.EnsureAct(act,rng.Next,["a","b","c"],["MysteryBox"],["potion"],["common"],["a","b","c"],["boss"],2);return s;}
        var s=New();var id=CommissionState.QuestId(0,1);
        Check(s.Quests.Count==3&&s.Quests[0].Stars==3&&s.Quests[0].Goal==CommissionGoal.EmpoweredElite,"每幕3项，一项固定三星强化精英");
        Check(s.Quests.Skip(1).Select(q=>q.Template).Distinct().Count()==2,"两项村民任务不重复");
        Check(!s.EnsureAct(0,_=>throw new Exception("重掷"),[],[],[],[])&&s.ToJson()==New().ToJson(),"打开、读档和再次进入不重掷");
        Check(s.Accept(id,0,1)&&!s.Accept(id+1,0,1)&&s.ActiveCount==1,"最多同时一项");
        Check(!s.WinElite("unbound")&&!CommissionState.Ready(s.Active!,_=>true),"未强化的精英胜利不追溯");
        Check(s.BindElite("next-elite")&&!s.BindElite("later-elite"),"只绑定下一场精英，重复开战不换目标");
        Check(!s.WinElite("other-elite")&&s.WinElite("next-elite")&&!s.WinElite("next-elite"),"只统计绑定的强化精英，胜利去重");
        Check(s.Claim(id,_=>true)&&!s.Claim(id,_=>true),"完成后只能领奖一次");
        Check(s.Accept(id+1,0,1)&&s.Abandon(id+1)&&!s.Accept(id+1,0,1)&&s.Accept(id+2,0,1),"放弃关闭该条，仍可接另外一条");
        var saved=CommissionState.FromJson(s.ToJson());Check(saved.ToJson()==s.ToJson(),"存读档保留星级、奖励和目标战斗");
        s.EnsureAct(1,_=>0,["a","b","c"],["MysteryBox"],["potion"],["common"],["a","b","c"],["boss"]);
        Check(s.ActiveCount==0&&!s.Claim(id+2,_=>true)&&s.Quests.Single(q=>q.Id==id+2).Status==CommissionStatus.Abandoned,"过幕自动放弃并拒绝旧幕提交");
        var carry=New();var hunt=carry.Quests[0];carry.Accept(hunt.Id,0,1);carry.BindElite("done");carry.WinElite("done");
        carry.EnsureAct(1,_=>0,["a","b","c"],["MysteryBox"],["potion"],["common"]);
        Check(carry.ActiveCount==0&&hunt.Completed&&carry.Accept(CommissionState.QuestId(1,1),1,1),"已完成旧幕委托保留且不占新幕接取名额");
        carry=CommissionState.FromJson(carry.ToJson());Check(carry.Claim(hunt.Id,_=>false)&&!carry.Claim(hunt.Id,_=>true),"旧幕完成资格存档后仍可领取且只能领取一次");
        var deck=new CommissionState{CurrentAct=0,GeneratedActs=[0],Quests=[new(){Id=10001,Act=0,Goal=CommissionGoal.Deck,Spell="owned",Status=CommissionStatus.Accepted}]};
        deck.EnsureAct(1,_=>0,["a","b","c"],["MysteryBox"],["potion"],["common"],meets:x=>x=="owned");
        Check(deck.Claim(10001,_=>false),"跨幕锁定已满足的持有条件，之后丢失卡牌仍可领奖");
        var allTemplates=new HashSet<string>();var grapeRewards=new HashSet<string>();bool bossSeen=false,rareSeen=false;
        for(int seed=0;seed<1500;seed++){
            var a=New(seed);var b=New(seed,1);var c=New(seed,2);
            if(a.Quests[0].Reward.Kind==CommissionRewardKind.Relic)throw new Exception("第一幕出现Boss遗物");
            bossSeen|=b.Quests[0].Reward.Item=="boss"||c.Quests[0].Reward.Item=="boss";
            foreach(var q in a.Quests){allTemplates.Add(q.Template);if(q.Template=="GRAPES")grapeRewards.Add(q.Reward.Kind.ToString());if(q.Reward.Kind==CommissionRewardKind.RareChoice){rareSeen=true;if(q.Reward.Choices?.Distinct().Count()!=3)throw new Exception("金卡选项不完整");}}
            if(a.Quests.Count!=3||b.Quests.Count!=3)throw new Exception("超出三项");
        }
        Check(bossSeen,"二、三幕可以抽到一代模组Boss遗物");
        Check(rareSeen,"三星奖励池包含固定金卡三选一");
        Check(grapeRewards.Count>=3,"同一村民任务可配不同同星级报酬，不绑定模板");
        Check(allTemplates.Count==CommissionState.Villagers.Length+1&&CommissionState.Villagers.Length>=15,"村民池内容丰富并可实际抽到全部模板");
        Check(New(2).Quests.All(q=>q.Goal!=CommissionGoal.Cargo||q.Reward.Kind==CommissionRewardKind.Freight),"护送专门按运费结算");
        var old=new CommissionState{Version=2,CurrentAct=0,GeneratedActs=[0],Quests=[new(){Id=1,Act=0,Template="PATROL",Goal=CommissionGoal.Monsters,Target=3,Status=CommissionStatus.Accepted}]};
        var migrated=CommissionState.FromJson(old.ToJson());migrated.EnsureAct(0,_=>0,["a","b","c"],["MysteryBox"],["potion"],["common"]);
        Check(migrated.Quests.Count(q=>!q.Legacy)==3&&migrated.Quests.Single(q=>q.Legacy).Status==CommissionStatus.Abandoned,"旧六项板仅迁移一次，保留历史并生成新三项板");
        Check(CommissionState.FromJson(migrated.ToJson()).ToJson()==migrated.ToJson(),"迁移后重新载入不再次生成");
        try{CommissionState.FromJson("{\"Version\":4}");throw new Exception("未来版本未拒绝");}catch(ArgumentException){Check(true,"未来存档版本拒绝");}
        return count;
    }
}

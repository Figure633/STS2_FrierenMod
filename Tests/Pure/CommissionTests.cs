using FrierenMod.Core;

public static class CommissionTests
{
    public static int Run()
    {
        int count = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception(name); count++; Console.WriteLine("PASS 委托：" + name); }
        CommissionState New(int variant=0) {
            var s = new CommissionState(); int choice=0;
            s.EnsureAct(0, max => max==5 && ++choice==1 ? variant : 0, ["a", "b"], ["tool"], ["potion"], ["relic"]); return s;
        }
        var state = New();
        Check(state.Quests.Count == 6 && state.Quests.Select(q => q.OpensAt).SequenceEqual(new[] {1,1,3,5,3,7}), "每幕最多六项，挑战任务提前开放");
        Check(!state.EnsureAct(0, _ => throw new Exception("重复推进随机数"), [], [], [], []), "重复进入本幕不生成或重掷");
        Check(state.ToJson() == New().ToJson(), "同一随机源生成相同任务和固定奖励");
        Check(state.Quests[0].Reward.Amount==90 && state.Quests[4].Reward.BonusGold==200, "基础报酬提升，高难挑战额外奖励更多金币");
        Check(!state.Accept(3,0,1) && !state.Accept(1,1,9) && !state.Accept(999,0,9), "拒绝未开放、过期与不存在的委托");
        state.Record("before",CommissionGoal.Monsters);
        Check(state.Accept(1,0,1) && state.Quests[0].Progress == 0 && !state.Record("before",CommissionGoal.Monsters), "接取前击杀不追溯，复活也不补计");
        state.Record("after",CommissionGoal.Monsters); state.Record("after",CommissionGoal.Monsters);
        Check(state.Quests[0].Progress == 1, "同一敌人重复死亡只计一次");
        state.Record("more",CommissionGoal.Monsters,99);
        Check(state.Quests[0].Progress == 3 && state.Claim(1,_=>false) && !state.Claim(1,_=>false), "进度封顶，完成后只领取一次");
        Check(state.Accept(2,0,9) && !state.Claim(2,_=>false) && CommissionState.Ready(state.Quests[1],_=>true), "指定魔法实时核对持有状态");
        Check(state.Claim(2,_=>true), "持有指定魔法可完成，规则层不删除卡牌");
        Check(state.Accept(3,0,9) && state.Accept(4,0,9) && state.Accept(5,0,9) && !state.Accept(6,0,9), "最多同时接三项");
        Check(state.Abandon(5) && !state.Accept(5,0,9) && state.Accept(6,0,9), "放弃释放名额，不能重接");
        state.Record("swift",CommissionGoal.FirstTurnBattle);
        Check(CommissionState.Ready(state.Quests.Single(q=>q.Id==3),_=>false), "挑战完成后可在当幕领奖");
        state.EnsureAct(1,_=>0,["a","b"],["tool"],["potion"],["relic"]);
        Check(state.ActiveCount==0 && state.Quests.Where(q=>q.Act==0&&q.Status!=CommissionStatus.Claimed).All(q=>q.Status==CommissionStatus.Abandoned), "新幕自动放弃旧幕全部未领奖任务，包括已达标任务");
        Check(!state.Claim(3,_=>true) && !state.Accept(6,0,9), "旧幕不可领奖或重新接取");
        state.Record("next-act",CommissionGoal.Monsters);
        Check(state.Quests.Single(q=>q.Id==6).Progress==0, "新幕战斗不推进旧任务");
        Check(state.Quests.Single(q=>q.Id==1).Status==CommissionStatus.Claimed, "过幕保留已领奖历史");
        state.BeginCombat("fight");state.LoseHp("fight");state.BeginCombat("fight");
        Check(state.LostHp, "同场重复初始化不清除扣血记录");
        var copy = CommissionState.FromJson(state.ToJson());
        Check(copy.ToJson()==state.ToJson() && copy.LostHp && !copy.Record("next-act",CommissionGoal.Monsters), "读档保留战斗扣血、固定奖励、进度及事件去重");
        copy.BeginCombat("new-fight");Check(!copy.LostHp,"新一场战斗清除上场扣血记录");
        var legacy=CommissionState.FromJson(state.ToJson().Replace("\"Version\":2","\"Version\":1"));
        Check(legacy.Version==2&&legacy.Quests.Count==state.Quests.Count,"兼容旧委托存档，不重掷旧板");
        var templates=new HashSet<string>();
        for(int i=0;i<5;i++){var s=New(i);Check(s.Quests.Count==6,"可变委托池仍仅六项："+i);templates.Add(s.Quests[5].Template);}
        Check(templates.Count==5,"第六项从五种不同委托中抽取");
        try { CommissionState.FromJson("{\"Version\":3}"); throw new Exception("未拒绝未来存档"); } catch(ArgumentException) { Check(true,"拒绝未来格式"); }
        return count;
    }
}

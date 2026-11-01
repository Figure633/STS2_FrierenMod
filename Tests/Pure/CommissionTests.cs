using FrierenMod.Core;

public static class CommissionTests
{
    public static int Run()
    {
        int count = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception(name); count++; Console.WriteLine("PASS 委托：" + name); }
        CommissionState New() { var s = new CommissionState(); s.EnsureAct(0, _ => 0, ["a", "b"], ["tool"], ["potion"], ["relic"]); return s; }
        var state = New(); Check(state.Quests.Count == 6 && state.Quests.Select(q => q.OpensAt).SequenceEqual(new[] {1,1,3,5,7,9}), "六个委托按楼层开放");
        Check(!state.EnsureAct(0, _ => throw new Exception("重复推进随机数"), [], [], [], []), "重复进入本幕不生成或重掷");
        Check(state.ToJson() == New().ToJson(), "同一随机源生成相同任务和固定奖励");
        Check(!state.Accept(3,0,1) && !state.Accept(1,1,9) && !state.Accept(999,0,9), "拒绝未开放、过期与不存在的委托");
        state.Record("before",CommissionGoal.Monsters);
        Check(state.Accept(1,0,1) && state.Quests[0].Progress == 0 && !state.Record("before",CommissionGoal.Monsters), "接取前击杀不追溯，复活也不补计");
        state.Record("after",CommissionGoal.Monsters); state.Record("after",CommissionGoal.Monsters);
        Check(state.Quests[0].Progress == 1, "同一敌人重复死亡只计一次");
        state.Record("more",CommissionGoal.Monsters,99);
        Check(state.Quests[0].Progress == 3 && state.Claim(1,_=>false) && !state.Claim(1,_=>false), "进度封顶，完成后只领取一次");
        Check(state.Accept(2,0,9) && !state.Claim(2,_=>false) && CommissionState.Ready(state.Quests[1],_=>true), "指定魔法实时核对持有状态");
        Check(state.Claim(2,_=>true), "持有指定魔法可完成，规则层不删除卡牌");
        Check(state.Accept(3,0,9) && state.Accept(4,0,9) && state.Accept(5,0,9) && !state.Accept(6,0,9), "最多同时接三张");
        Check(state.Abandon(5) && !state.Accept(5,0,9) && state.Accept(6,0,9), "放弃释放名额，不能反复接取刷进度");
        state.EnsureAct(1,_=>0,["a","b"],["tool"],["potion"],["relic"]);
        state.Record("next-act",CommissionGoal.Monsters);
        Check(state.Quests.Single(q=>q.Id==3).Progress == 1 && !state.Accept(1,1,9), "已接委托跨幕继续，旧幕未接委托关闭");
        var copy = CommissionState.FromJson(state.ToJson());
        Check(copy.ToJson()==state.ToJson() && !copy.Record("next-act",CommissionGoal.Monsters), "存读档保留固定奖励、进度、领取状态及死亡去重");
        try { CommissionState.FromJson("{\"Version\":2}"); throw new Exception("未拒绝未来存档"); } catch(ArgumentException) { Check(true,"拒绝未来格式"); }
        return count;
    }
}

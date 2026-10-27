using MegaCrit.Sts2.Core.Combat;
using HarmonyLib;

public static class CombatTestState
{
    public static void SetInProgress(bool active)
    {
        var state = AccessTools.Field(typeof(CombatManager), "_turnState").GetValue(CombatManager.Instance);
        if (state == null) { if (active) throw new InvalidOperationException("先建立测试战斗"); return; }
        AccessTools.Property(state.GetType(), "IsInProgress").SetValue(state, active);
    }
}

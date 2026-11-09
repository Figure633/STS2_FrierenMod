using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace FrierenMod.Scripts.Mechanics;

// 二代没有玩家烟雾弹；复用原生逃跑、结束战斗和无奖励出口，不判为击杀。
public static class MagicExamEscape
{
    private static readonly SavedSpireField<Player,string> EscapedRoom=new(()=>"","FrierenMagicExamEscape");
    public static bool IsEscaped(ICombatState? combat) => combat?.RunState.CurrentRoom is CombatRoom room &&
        combat.Players.Any(p=>EscapedRoom.Get(p)==CommissionCmd.BattleKey(combat.RunState,room));
    public static async Task Flee(Player player)
    {
        if(player.RunState.CurrentRoom is not CombatRoom {RoomType:not RoomType.Boss} room || player.Creature.CombatState is not {} combat)return;
        string key=CommissionCmd.BattleKey(player.RunState,room);
        foreach(var member in combat.Players)EscapedRoom.Set(member,key);
        foreach(var enemy in combat.Enemies.ToArray())await CreatureCmd.Escape(enemy);
    }
    [HarmonyPatch(typeof(EncounterModel),nameof(EncounterModel.ShouldGiveRewards),MethodType.Getter)]
    private static class NoRewards
    {
        private static void Postfix(EncounterModel __instance,ref bool __result)
        {
            if(RunManager.Instance.DebugOnlyGetState()?.CurrentRoom is CombatRoom room && room.Encounter==__instance && IsEscaped(room.CombatState))__result=false;
        }
    }
    [HarmonyPatch(typeof(Hook),nameof(Hook.AfterCombatVictory))]
    private static class NoVictoryEffects
    {
        private static bool Prefix(ICombatState combatState,ref Task __result)
        {if(!IsEscaped(combatState))return true;__result=Task.CompletedTask;return false;}
    }
}

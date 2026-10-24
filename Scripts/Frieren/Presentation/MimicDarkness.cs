using FrierenMod.Scripts.Powers;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace FrierenMod.Scripts.Presentation;

// 一代黑暗按适应阶段逐步恢复 HUD；只控制本场已有节点，离场或能力移除立即恢复。
public partial class MimicDarkness : Node
{
    private readonly Dictionary<CanvasItem, bool> _hidden = [];
    private NCombatRoom Room => (NCombatRoom)GetParent();
    public override void _Process(double delta)
    {
        var player = LocalContext.GetMe(RunManager.Instance.DebugOnlyGetState());
        var dark = CombatManager.Instance.IsInProgress ? player?.Creature.GetPower<DarkPower>() : null;
        var wanted = new HashSet<CanvasItem>();
        void Hide(CanvasItem? item, bool hide) { if (hide && item != null) wanted.Add(item); }
        if (dark != null && NRun.Instance?.GlobalUi is { } global)
        {
            int stage = dark.Adaptation;
            Hide(Room.GetNodeOrNull<Control>("%BgContainer"), true);
            foreach (var creature in Room.CreatureNodes)
            {
                Hide(creature.IntentContainer, stage < 2);
                Hide(creature.GetNodeOrNull<Control>("%HealthBar"), stage < 2);
            }
            var top = global.TopBar;
            foreach (var node in new CanvasItem[] { top.Hp, top.Gold, top.Map, top.Deck, top.PotionContainer, global.RelicInventory }) Hide(node, stage < 2);
            foreach (var node in new CanvasItem[] { top.Portrait, top.FloorIcon, top.RoomIcon, top.BossIcon }) Hide(node, stage < 3);
            Hide(Room.Ui.DrawPile, stage < 3);
            Hide(Room.Ui.DiscardPile, stage < 4); Hide(Room.Ui.ExhaustPile, stage < 4);
            Hide(Room.Ui.EnergyCounterContainer, stage < 4);
        }
        foreach (var node in _hidden.Keys.Except(wanted).ToArray()) Restore(node);
        foreach (var node in wanted)
        {
            if (!_hidden.ContainsKey(node)) _hidden[node] = node.Visible;
            node.Hide();
        }
    }
    private void Restore(CanvasItem node)
    {
        if (GodotObject.IsInstanceValid(node)) node.Visible = _hidden[node];
        _hidden.Remove(node);
    }
    public override void _ExitTree() { foreach (var node in _hidden.Keys.ToArray()) Restore(node); }
}

[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom._Ready))]
public static class MimicDarknessSetup
{
    [HarmonyPostfix]
    private static void Attach(NCombatRoom __instance) => __instance.AddChild(new MimicDarkness());
}

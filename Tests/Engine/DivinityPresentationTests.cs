using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Powers;
using FrierenMod.Scripts.Presentation.Divinity;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.Rooms;

public static class DivinityPresentationTests
{
    public static async Task<int> Run(Node root)
    {
        int checks = 0;
        void Check(bool ok, string text) { if (!ok) throw new Exception(text); checks++; GD.Print("PASS 神格：" + text); }
        async Task Frames(int n = 3) { for (int i = 0; i < n; i++) await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame); }
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "DIVINITY-PRESENTATION");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = 1;
        var combat = new CombatState(runState: run); combat.AddPlayer(player);
        combat.AddCreature(combat.CreateCreature(ModelDb.Monster<Axebot>().ToMutable(), CombatSide.Enemy, null));
        CombatManager.Instance.SetUpCombat(combat); AccessTools.Property(typeof(CombatManager), nameof(CombatManager.IsInProgress)).SetValue(CombatManager.Instance, true);
        var character = (Frieren)player.Character; character.RegisterSceneConversions();
        var host = new Node2D(); root.AddChild(host);
        var background = new ColorRect { Size = new Vector2(1920, 1080), Color = new Color("17202c"), MouseFilter = Control.MouseFilterEnum.Ignore }; host.AddChild(background);
        var normal = character.CreateVisuals(); normal.Position = new Vector2(600, 760); host.AddChild(normal);
        var divine = character.CreateVisuals(); divine.Position = new Vector2(1250, 760); host.AddChild(divine);
        var body = divine.GetNode<Node2D>("Visuals/Body"); var tint = body.Modulate;
        var session = DivinityPresentation.Attach(player.Creature, divine, body);
        Check(!session.IsActive && DivinityPresentation.Attach(player.Creature, divine, body) == session, "无神格不显示，重复接入不增加光环");
        var context = new ThrowingPlayerChoiceContext();
        await PowerCmd.Apply<MantraPower>(context, player.Creature, 10, player.Creature, null); await Frames();
        var aura = session.GetChild<Node2D>(0);
        Check(session.IsActive && aura.GetNode("FloatingEyes") is FrierenDivinityEyeSpawner && aura.GetNode("AuraBlobs") is FrierenAuraBlobEmitter && aura.GetNode("ActivationBurst") is FrierenDivinityActivationBurst,
            "十层真言进入神格时加载原有眼睛、光环与进入特效");
        Check(body.Modulate != tint && normal.GetNode<Node2D>("Visuals/Body").Modulate == tint, "仅对应角色呈现紫色，原色按角色保存");
        Check(aura.GetNode<AudioStreamPlayer>("DivinityAmbience").Playing && ((AudioStreamOggVorbis)aura.GetNode<AudioStreamPlayer>("DivinityAmbience").Stream).Loop,
            "原有神格环境音循环播放");
        var icon = ModelDb.Power<DivinityPower>().Icon;
        Check(icon.GetWidth() > 0 && icon.ResourcePath.Contains("DivinityPower"), "专用神格能力图标可加载");
        await root.ToSignal(root.GetTree().CreateTimer(0.45), SceneTreeTimer.SignalName.Timeout);
        if (OS.GetCmdlineUserArgs().Contains("--ui-snapshot")) root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/divinity-updated.png"));
        var power = player.Creature.GetPower<DivinityPower>()!;
        power.AmountOnTurnStart = 1;
        await power.AfterPlayerTurnStart(context, player); await Frames();
        Check(!session.IsActive && body.Modulate == tint && !GodotObject.IsInstanceValid(aura), "移除神格时清除光环、音效并恢复角色原色");
        await PowerCmd.Apply<DivinityPower>(context, player.Creature, 1, player.Creature, null);
        session.QueueFree(); await Frames();
        Check(body.Modulate == tint, "战斗视觉离场时恢复原色并释放事件订阅");
        session = DivinityPresentation.Attach(player.Creature, divine, body); await Frames();
        Check(session.IsActive && session.GetChild(0).GetNodeOrNull("ActivationBurst") == null,
            "战斗已有神格时恢复光环，不重复播放进入爆发");
        ((Action<CombatRoom>?)AccessTools.Field(typeof(CombatManager), "CombatEnded").GetValue(CombatManager.Instance))?.Invoke(null!);
        await Frames();
        Check(!session.IsActive && body.Modulate == tint && session.GetChildCount() == 0, "战斗结束事件清除全部神格光环与循环声音");
        await PowerCmd.Remove(player.Creature.GetPower<DivinityPower>()!); await Frames();
        Check(!session.IsActive && body.Modulate == tint, "恢复后的神格同样正常退出");
        host.QueueFree(); await Frames(); RunManager.Instance.CleanUp(); return checks;
    }
}

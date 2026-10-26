using FrierenMod.Scripts.Characters;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.sts2.Core.Nodes.TopBar;

public static class PortraitLayoutTests
{
    public static async Task<int> Run(Node root)
    {
        int count = 0;
        void Check(bool value, string message) { if (!value) throw new Exception(message); count++; GD.Print("PASS 人物头像：" + message); }
        async Task Tick() { for (int i = 0; i < 4; i++) await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame); }
        SaveManager.Instance.InitPrefsDataForTest();
        var character = ModelDb.Character<Frieren>();
        Check(character.IconTexture.GetSize() == new Vector2(85, 85), "历史记录贴图采用原版85×85规格");
        var panel = new Control { Size = new(1920, 1080) }; root.AddChild(panel);
        panel.AddChild(new ColorRect { Size = panel.Size, Color = new("24333e") });
        panel.AddChild(new TextureRect { Position = new(91, 93), Size = new(72, 68),
            Texture = GD.Load<Texture2D>("res://images/atlases/ui_atlas.sprites/top_bar/top_bar_char_backdrop.tres"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize });
        var host = new NTopBarPortrait { Size = new(54, 54), Position = new(100, 100) }; panel.AddChild(host);
        host.Initialize(Player.CreateForNewRun<Frieren>(UnlockState.all, 1)); await Tick();
        var icon = host.GetChild<TextureRect>(0);
        Check(icon.Position.IsEqualApprox(Vector2.Zero) && icon.Size.IsEqualApprox(host.Size), "实际顶栏54×54容器内居中，不再使用64像素固定节点");
        foreach (float size in new[] { 40f, 54f, 72f }) {
            host.Size = Vector2.One * size; await Tick();
            Check(icon.GetGlobalRect().IsEqualApprox(host.GetGlobalRect()), "头像随父框大小适配：" + size);
        }
        host.Size = new(54, 54);
        var historyIcon = GD.Load<PackedScene>(NRunHistoryPlayerIcon.scenePath).Instantiate<NRunHistoryPlayerIcon>();
        historyIcon.Position = new(400, 100); panel.AddChild(historyIcon);
        var player = new RunHistoryPlayer { Id = 1, Character = character.Id };
        historyIcon.LoadRun(player, new RunHistory { Players = [player], Ascension = 10 }); await Tick();
        var portrait = historyIcon.GetNode<TextureRect>("%Icon");
        Check(portrait.Size.IsEqualApprox(historyIcon.Size) && portrait.Size.X <= 85, $"实际历史记录头像服从容器尺寸：头像{portrait.Size}，容器{historyIcon.Size}");
        historyIcon.Select();
        await root.ToSignal(root.GetTree().CreateTimer(.15), SceneTreeTimer.SignalName.Timeout);
        // 原生选中补间放大1.1倍；透明安全区保证实际人物仍在框内。
        var alphaBounds = new Rect2(new(7, 8), new(71, 69));
        var drawn = new Rect2(portrait.GetGlobalTransform() * (alphaBounds.Position / 85 * portrait.Size),
            alphaBounds.Size / 85 * portrait.Size * portrait.GetGlobalTransform().Scale);
        Check(historyIcon.GetGlobalRect().Grow(.1f).Encloses(drawn), "历史记录选中放大后人物仍在框内");
        if (OS.GetCmdlineUserArgs().Contains("--ui-snapshot")) root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/portrait-layout-current.png"));
        var other = new RunHistoryPlayer { Id = 1, Character = ModelDb.Character<Ironclad>().Id };
        historyIcon.LoadRun(other, new RunHistory { Players = [other] }); await Tick();
        Check(portrait.ExpandMode == TextureRect.ExpandModeEnum.KeepSize, "同一控件改显示原版角色时恢复原生布局");
        panel.QueueFree(); await Tick(); return count;
    }
}

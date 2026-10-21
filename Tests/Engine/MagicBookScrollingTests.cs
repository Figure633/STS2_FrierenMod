using System.Text.Json;
using FrierenMod.Core;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.UI;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;

public static class MagicBookScrollingTests
{
    private static readonly NControllerManager Controller = new();
    private static readonly NInputManager Input = new();
    private static bool GetController(ref NControllerManager __result) { __result = Controller; return false; }
    private static bool GetInput(ref NInputManager __result) { __result = Input; return false; }
    public static async Task<int> Run(Node root)
    {
        var inputFixture = new Harmony("frieren.tests.book_scroll_input");
        inputFixture.Patch(AccessTools.PropertyGetter(typeof(NControllerManager), nameof(NControllerManager.Instance)),
            prefix: new HarmonyMethod(typeof(MagicBookScrollingTests), nameof(GetController)));
        AccessTools.Property(typeof(NInputManager), nameof(NInputManager.ControllerManager)).SetValue(Input, Controller);
        inputFixture.Patch(AccessTools.PropertyGetter(typeof(NInputManager), nameof(NInputManager.Instance)),
            prefix: new HarmonyMethod(typeof(MagicBookScrollingTests), nameof(GetInput)));
        int count = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception(name); GD.Print("PASS 魔法书滚动：" + name); count++; }
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "FRIEREN-BOOK-SCROLL");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = 1;
        LocManager.Instance.SetLanguage("zhs");
        var deck = MagicDeckState.FromJson(JsonSerializer.Serialize(new MagicDeckSave(3,
            MagicDeckState.CreatePrototypeLoadout().Items.ToArray(), true, 3, StartingPropsGranted: true)));
        for (int i = 0; i < 20; i++) deck.Add(SpellCatalog.All[i % SpellCatalog.All.Count].Id);
        int crystal = deck.AddProp("CrystalBall").InstanceId;
        deck.AddProp("TransmutationCircle"); deck.AddProp("UnbelievableTool");
        MagicDeckStore.Write(player, deck);
        string before = deck.ToJson();
        var ui = new Control { Size = new(1920, 1080) }; root.AddChild(ui);
        ui.AddChild(new ColorRect { Size = ui.Size, Color = new("17202c"), MouseFilter = Control.MouseFilterEnum.Ignore });
        var screen = new MagicBookScreen(ui, player, () => null); screen.Open();
        var scroll = screen.View.GetNode<ScrollContainer>("BookCanvas/InventoryScroll");
        var inventory = (GridContainer)screen.View.FindChild("Inventory", true, false);
        var status = (Control)screen.View.FindChild("BookStatus", true, false);
        var sorter = (NCardViewSortButton)screen.View.FindChild("TypeSorter", true, false);
        NGridCardHolder Holder(string name) => (NGridCardHolder)screen.View.FindChild(name, true, false);
        var equipped = Holder("Factor_" + deck.Items[0].InstanceId);
        async Task Tick() { for (int i = 0; i < 8; i++) await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame); }
        Vector2 Point(NGridCardHolder holder) => holder.Hitbox.GetGlobalTransformWithCanvas() * (holder.Hitbox.Size / 2);
        async Task Click(Vector2 point, MouseButton button = MouseButton.Left) {
            root.GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = button, Pressed = true }, true);
            await Tick();
            root.GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = button, Pressed = false }, true);
            await Tick();
        }
        async Task Snapshot(string name) {
            await Tick();
            if (OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))
                root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/" + name + ".png"));
        }
        int Selected() => ((List<int>)AccessTools.Field(typeof(MagicBookScreen), "_selected").GetValue(screen)!).Count;
        await Tick();
        var equippedStart = Point(equipped); var sorterStart = sorter.GlobalPosition; var statusStart = status.GlobalPosition;
        await Snapshot("magic-book-scroll-top");
        var wheelPoint = scroll.GetGlobalTransformWithCanvas() * (scroll.Size / 2);
        await Click(wheelPoint, MouseButton.WheelDown);
        int delta = scroll.ScrollVertical;
        Check(delta > 0 && Math.Abs(Point(equipped).Y - equippedStart.Y + delta) < 1 &&
            Math.Abs(sorter.GlobalPosition.Y - sorterStart.Y + delta) < 1, "真实滚轮带动槽位、排序栏与库存一起移动");
        Check(status.GlobalPosition == statusStart, "底部操作提示保持固定");
        root.GetViewport().PushInput(new InputEventMouseMotion { Position = new(30, 100), GlobalPosition = new(30, 100) }, true);
        scroll.ScrollVertical = 450; await Tick();
        Check(inventory.Columns == 5 && inventory.GetChildren().OfType<Control>().Take(15)
            .All(cell => scroll.GetGlobalRect().Encloses(cell.GetGlobalRect())), "一屏完整显示五列三行库存格");
        await Snapshot("magic-book-scroll-three-rows");
        var first = inventory.GetChild<Control>(0).FindChildren("*", "", true, false).OfType<NGridCardHolder>().Single();
        first.Hitbox.EmitSignal(NClickableControl.SignalName.Focused, first.Hitbox); await Tick();
        Check(scroll.GetGlobalRect().Encloses(first.Hitbox.GetGlobalRect()), "边缘卡牌悬停放大后自动滚入可见范围");
        await Snapshot("magic-book-scroll-hover");
        first.Hitbox.EmitSignal(NClickableControl.SignalName.Unfocused, first.Hitbox);
        foreach (var size in new[] { new Vector2(1280, 720), new Vector2(2560, 1440) }) {
            ui.Size = size; await Tick(); scroll.ScrollVertical = 450; await Tick();
            Check(inventory.GetChildren().OfType<Control>().Take(15).All(cell => scroll.GetGlobalRect().Encloses(cell.GetGlobalRect())) &&
                new Rect2(Vector2.Zero, size).Encloses(scroll.GetGlobalRect()), "统一缩放后三行仍可见：" + size);
        }
        ui.Size = new(1920, 1080); await Tick(); sorter.ForceClick(); await Tick();
        scroll.ScrollVertical = 300; await Tick();
        var prop = Holder("Prop_" + crystal);
        await Click(Point(prop), MouseButton.Right);
        Check(((int?)AccessTools.Field(typeof(MagicBookScreen), "_armed").GetValue(screen)) == crystal, "滚动后实际右键仍能启用魔具");
        await Click(Point(equipped));
        Check(Selected() == 0, "滚动裁切区以外的槽位不会响应点击");
        scroll.ScrollVertical = 0; await Tick(); await Click(Point(equipped));
        Check(Selected() == 1, "向上滚回槽位后能继续选择原魔具的目标");
        await Click(Point(prop), MouseButton.Right);
        Check(Selected() == 0 && MagicDeckStore.Read(player).ToJson() == before, "取消选择不改动库存和槽位");
        scroll.ScrollVertical = 450; await Tick();
        deck.Add("BuildUpBody"); MagicDeckStore.Write(player, deck); screen.Refresh(); await Tick();
        Check(scroll.ScrollVertical == 450, "刷新库存保留当前滚动位置");
        screen.Close(); screen.Open(); await Tick();
        Check(scroll.ScrollVertical == 0, "重新打开恢复至槽位所在的顶部");
        screen.Close(); ui.QueueFree(); await Tick();
        inputFixture.UnpatchAll(inputFixture.Id); Controller.Free(); Input.Free();
        return count;
    }
}

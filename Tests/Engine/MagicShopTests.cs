using FrierenMod.Core;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Relics;
using FrierenMod.Scripts.RestSite;
using FrierenMod.Scripts.UI;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;
using System.Text.Json;

public static class MagicShopTests
{
    public static async Task<int> Run(Node root)
    {
        int count = 0;
        void Check(bool value, string name) {
            if (!value) throw new Exception(name);
            GD.Print("PASS " + name); count++;
        }
        if (RunManager.Instance.DebugOnlyGetState() != null) RunManager.Instance.CleanUp();
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "FRIERENSHOP");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService());
        LocalContext.NetId = 1;
        run.PushRoom(new RestSiteRoom());
        var selector = new TestCardSelector();
        using var selection = CardSelectCmd.UseSelector(selector);
        MagicDeckState Fresh() => MagicDeckState.FromJson(JsonSerializer.Serialize(new MagicDeckSave(3,
            MagicDeckState.CreatePrototypeLoadout().Items.ToArray(), true, 3, StartingPropsGranted: true)));
        int Prepare(string prop, params string[] extra) {
            var deck = Fresh(); foreach (string id in extra) deck.Add(id);
            int instance = deck.AddProp(prop).InstanceId; MagicDeckStore.Write(player, deck); return instance;
        }
        MagicDeckState Read() => MagicDeckStore.Read(player);
        Task<bool> Use(int propId, params int[] indexes) {
            var deck = Read();
            var eligible = MagicPropRules.Eligible(deck, deck.Props.Single(p => p.InstanceId == propId));
            return MagicPropCmd.UseSelected(player, propId, indexes.Select(i => eligible[i]).ToArray());
        }
        MagicDeckStore.Write(player, Fresh());
        var option = RestSiteOption.Generate(player).OfType<BuyMagicPropsOption>().Single();
        Check(option.IsEnabled && option.Icon.GetWidth() > 0 && !option.Title.GetFormattedText().Contains("OPTION_"),
            "芙莉莲专属逛魔具店按钮、原图图标和标题已注册");
        await RelicCmd.Remove(player.GetRelic<HolyEmblem>()!);
        Check(RestSiteOption.Generate(player).OfType<BuyMagicPropsOption>().Count() == 1,
            "失去圣徽仍保留角色营地按钮");
        var ironclad = Player.CreateForNewRun<Ironclad>(UnlockState.all, 2);
        _ = RunState.CreateForTest([ironclad]);
        Check(!RestSiteOption.Generate(ironclad).OfType<BuyMagicPropsOption>().Any(), "其他角色不获得魔具店按钮");

        var sync = RunManager.Instance.RestSiteSynchronizer;
        sync.BeginRestSite();
        int shopIndex = sync.GetOptionsForPlayer(player).ToList().FindIndex(o => o is BuyMagicPropsOption);
        string before = Read().ToJson(); int gold = player.Gold;
        selector.PrepareToSelect(Array.Empty<int>());
        Check(!await sync.ChooseLocalOption(shopIndex) && sync.GetOptionsForPlayer(player).Count == 3 && Read().ToJson() == before,
            "从真实营地同步器取消魔具选择，库存和营地行动保持不变");
        selector.PrepareToSelect(new[] {1, 6});
        Check(await sync.ChooseLocalOption(shopIndex) && sync.GetOptionsForPlayer(player).Count == 0 &&
            Read().Props.Select(p => p.PropId).SequenceEqual(new[] {"MysteryBox", "UnbelievableTool"}) && player.Gold == gold,
            "营地八选二获得魔具，免费但消费一次营地行动");
        int box = Read().Props.Single(p => p.PropId == "MysteryBox").InstanceId;
        Check(await Use(box) && Read().Items.Count == 5 && Read().Props.Count == 1,
            "盲盒实际进入因子库存且使用后消耗");

        int prop = Prepare("CrystalBall");
        before = Read().ToJson();
        Check(!await Use(prop) && Read().ToJson() == before, "取消魔具目标选择不消耗");
        Check(await Use(prop, 0, 1) && Read().Items[0].Slot == 1 && Read().Items[1].Slot == 0,
            "水晶球交换两个槽位");
        prop = Prepare("UnbelievableTool", "BePrepared"); Check(await Use(prop, 0, 3) && Read().Items.Single(i => i.SpellId == "BePrepared").Slot == 0,
            "妙妙工具完成装载与库存替换");
        prop = Prepare("MysticBox", "BePrepared", "IceArrows", "BluePrint"); Check(await Use(prop, 0, 1, 2) && Read().Items.Take(3).All(i => i.Slot == -1),
            "神秘魔术箱通过内联目标编号重装三个槽位");
        prop = Prepare("TransmutationCircle"); Check(await Use(prop, 0, 2) && Read().Items.Count == 3 && Read().Items.All(i => i.Slot >= 0),
            "炼成阵实际替换两张因子且保留装载槽位");
        prop = Prepare("SeinNote", "BePrepared", "IceArrows");
        player.Creature.SetCurrentHpInternal(20);
        Check(await Use(prop, 0, 1) && player.Creature.CurrentHp == 30 && Read().Items.Count == 3,
            "赞因笔记实际消耗材料并通过原生命令治疗");
        prop = Prepare("UnbelievableToolBox", "BePrepared", "IceArrows"); Check(await Use(prop, 0, 1) && Read().Props.Count == 2 && Read().Props.All(p => p.PropId == "UnbelievableTool"),
            "工具箱实际生成两个独立妙妙工具");
        var saved = JsonSerializer.Serialize(player.ToSerializable(), JsonSerializationUtility.Options);
        var restored = Player.FromSerializable(JsonSerializer.Deserialize<SerializablePlayer>(saved, JsonSerializationUtility.Options)!);
        Check(MagicDeckStore.Read(restored).ToJson() == Read().ToJson(), "玩家真实存档恢复魔具、因子、槽位及编号计数");

        prop = Prepare("ShadowDragonHorn", "BePrepared", "IceArrows");
        Check(!MagicPropCmd.CanUse(player, prop), "龙角在营地禁用");
        var combat = new CombatState(runState: run); combat.AddPlayer(player);
        var enemy = combat.CreateCreature(ModelDb.Monster<MegaCrit.Sts2.Core.Models.Monsters.Axebot>().ToMutable(), CombatSide.Enemy, null);
        combat.AddCreature(enemy);
        CombatManager.Instance.SetUpCombat(combat);
        AccessTools.Property(typeof(CombatManager), nameof(CombatManager.IsInProgress)).SetValue(CombatManager.Instance, true);
        Check(!MagicPropCmd.CanUse(player, prop), "战斗开场阶段禁止插入魔具操作");
        AccessTools.Property(typeof(CombatManager), nameof(CombatManager.IsStarting)).SetValue(CombatManager.Instance, false);
        AccessTools.Property(typeof(CombatManager), nameof(CombatManager.PlayerActionsDisabled)).SetValue(CombatManager.Instance, true);
        Check(!MagicPropCmd.CanUse(player, prop), "结束回合后禁用魔具操作");
        AccessTools.Property(typeof(CombatManager), nameof(CombatManager.PlayerActionsDisabled)).SetValue(CombatManager.Instance, false);
        player.PlayerCombatState!.Energy = 3;
        Check(await Use(prop, 0, 1) && player.PlayerCombatState.Energy == 5 && Read().Items.Count == 3,
            "龙角在战斗中消耗两件材料并实际增加能量");
        AccessTools.Property(typeof(CombatManager), nameof(CombatManager.IsInProgress)).SetValue(CombatManager.Instance, false);

        prop = Prepare("MysteryBox");
        var ui = new Control { Size = new Vector2(1920, 1080) }; root.AddChild(ui);
        MagicBookUi.Attach(ui, player);
        ((FrierenMod.Scripts.UI.MagicFactorButton)ui.FindChild("FrierenMagicBookButton", true, false)).EmitSignal(MegaCrit.Sts2.Core.Nodes.GodotExtensions.NClickableControl.SignalName.Released, ui.FindChild("FrierenMagicBookButton", true, false));
        var book = ui.GetNode<Control>("FrierenMagicBook");
        var use = (NGridCardHolder)book.FindChild("Prop_" + prop, true, false);
        use.EmitSignal(NCardHolder.SignalName.AltPressed, use);
        for (int i = 0; i < 3; i++) await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
        Check(Read().Props.Count == 0 && Read().Items.Count == 5 && book.Visible,
            "魔法书右键盲盒立即生效并刷新原生卡牌库存");
        prop = Prepare("CrystalBall");
        ui.GetNode<Godot.Timer>("FrierenManaRefresh").EmitSignal(Godot.Timer.SignalName.Timeout);
        before = Read().ToJson();
        void Click(string name, bool right = false) {
            var holder = (NGridCardHolder)book.FindChild(name, true, false);
            var hitbox = holder.Hitbox;
            hitbox.EmitSignal(MegaCrit.Sts2.Core.Nodes.GodotExtensions.NClickableControl.SignalName.Focused, hitbox);
            hitbox.EmitSignal(MegaCrit.Sts2.Core.Nodes.GodotExtensions.NClickableControl.SignalName.MousePressed,
                new InputEventMouseButton { ButtonIndex = right ? MouseButton.Right : MouseButton.Left, Pressed = true });
            hitbox.EmitSignal(MegaCrit.Sts2.Core.Nodes.GodotExtensions.NClickableControl.SignalName.MouseReleased,
                new InputEventMouseButton { ButtonIndex = right ? MouseButton.Right : MouseButton.Left, Pressed = false });
        }
        var ids = Read().Items.Take(2).Select(i => i.InstanceId).ToArray();
        Click("Factor_" + ids[0]);
        Check(Read().ToJson() == before, "未启用魔具时点击因子不会免费换槽");
        Click("Prop_" + prop, true);
        Click("Factor_" + ids[0]);
        Click("Prop_" + prop, true);
        Check(Read().ToJson() == before && book.Visible, "右键取消已选一个目标的水晶球，库存与槽位不变");
        Click("Prop_" + prop, true);
        Click("Factor_" + ids[0]);
        root.GetViewport().PushInput(new InputEventKey { Keycode = Key.Escape, Pressed = true });
        await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
        Check(Read().ToJson() == before && book.Visible &&
            ((Label)book.FindChild("BookStatus", true, false)).Text == new MegaCrit.Sts2.Core.Localization.LocString("frieren", "BOOK_HINT").GetFormattedText(),
            "Esc 首先取消魔具选择，保留魔法书");
        Click("Prop_" + prop, true);
        Click("Factor_" + ids[0]);
        Click("Factor_" + ids[1]);
        await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
        Check(Read().Props.Count == 0 && Read().Items[0].Slot == 1 && Read().Items[1].Slot == 0 && book.Visible,
            "同屏选择两个因子完成水晶球交换，只消耗一次且不弹出其他选择界面");
        Check(!await MagicPropCmd.UseSelected(player, prop, ids), "重复提交已经消耗的魔具不会再次生效");
        prop = Prepare("UnbelievableTool", "BePrepared");
        before = Read().ToJson();
        Check(!await MagicPropCmd.UseSelected(player, prop, Read().Items.Take(2).Select(i => i.InstanceId).ToArray()) && Read().ToJson() == before,
            "内联选择两个已装载目标时拒绝错误组合且不消耗");
        ui.GetNode<Godot.Timer>("FrierenManaRefresh").EmitSignal(Godot.Timer.SignalName.Timeout);
        Click("Prop_" + prop, true);
        Click("Factor_" + Read().Items[0].InstanceId);
        book.Hide();
        ((FrierenMod.Scripts.UI.MagicFactorButton)ui.FindChild("FrierenMagicBookButton", true, false)).EmitSignal(MegaCrit.Sts2.Core.Nodes.GodotExtensions.NClickableControl.SignalName.Released, ui.FindChild("FrierenMagicBookButton", true, false));
        Click("Factor_" + Read().Items.Last().InstanceId);
        Check(Read().ToJson() == before, "关闭再打开魔法书不残留待使用魔具或选中目标");
        if (OS.GetCmdlineUserArgs().Contains("--ui-snapshot")) {
            var deck = Read(); foreach (var entry in MagicPropRules.All) deck.AddProp(entry.Id); MagicDeckStore.Write(player, deck);
            ui.GetNode<Godot.Timer>("FrierenManaRefresh").EmitSignal(Godot.Timer.SignalName.Timeout);
            for (int i = 0; i < 5; i++) await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
            root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/magic-shop-book.png"));
        }
        ui.QueueFree();
        for (int i = 0; i < 5; i++) await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
        run.PopCurrentRoom();
        return count;
    }
}

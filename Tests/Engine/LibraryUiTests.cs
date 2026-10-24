using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Presentation;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Pooling;
using MegaCrit.Sts2.Core.Entities.UI;

public static class LibraryUiTests
{
    private static bool SkipStartup() => false;
    // TestMode 原生工厂直接返回 null；使用同一对象池创建可见节点，不关闭存档隔离。
    private static bool CreateCard(CardModel card, ModelVisibility visibility, ref NCard? __result)
    {
        var node = NodePool.Get<NCard>();
        AccessTools.Property(typeof(NCard), nameof(NCard.Model)).SetValue(node, card);
        node.Visibility = visibility; __result = node; return false;
    }
    private static bool CreateHolder(NCard cardNode, ref NGridCardHolder? __result)
    {
        var holder = NodePool.Get<NGridCardHolder>();
        holder.Call(NCardHolder.MethodName.SetCard, cardNode);
        holder.Call(NGridCardHolder.MethodName.UpdateCardModel);
        holder.Scale = holder.SmallScale; __result = holder; return false;
    }
    public static async Task<int> Run(Node root)
    {
        int count = await LibraryTests.Run(root);
        void Check(bool ok, string name) { if (!ok) throw new Exception(name); count++; GD.Print("PASS 百科入口：" + name); }
        async Task Tick() {
            // 原生百科换组包含退场和入场动画；等待结束再切换或销毁场景。
            await root.ToSignal(root.GetTree().CreateTimer(0.6), SceneTreeTimer.SignalName.Timeout);
            for (int i = 0; i < 4; i++) await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        SaveManager.Instance.InitPrefsDataForTest();
        var startup = new Harmony("frieren.tests.library_host");
        startup.Patch(AccessTools.Method(typeof(NCard), nameof(NCard.Create)), prefix: new HarmonyMethod(typeof(LibraryUiTests), nameof(CreateCard)));
        startup.Patch(AccessTools.Method(typeof(NGridCardHolder), nameof(NGridCardHolder.Create)), prefix: new HarmonyMethod(typeof(LibraryUiTests), nameof(CreateHolder)));
        foreach (string method in new[] { nameof(NGame._EnterTree), nameof(NGame._Ready) })
            startup.Patch(AccessTools.Method(typeof(NGame), method), prefix: new HarmonyMethod(typeof(LibraryUiTests), nameof(SkipStartup)));
        var game = new NGame { Size = new(1920, 1080), ProcessMode = Node.ProcessModeEnum.Disabled };
        var input = new NInputManager(); var controller = new NControllerManager();
        AccessTools.Property(typeof(NInputManager), nameof(NInputManager.ControllerManager)).SetValue(input, controller);
        AccessTools.Property(typeof(NGame), nameof(NGame.InputManager)).SetValue(game, input);
        root.AddChild(game); AccessTools.Property(typeof(NGame), nameof(NGame.Instance)).SetValue(null, game);
        foreach (string language in new[] { "zhs", "eng" }) {
            LocManager.Instance.SetLanguage(language);
            var library = GD.Load<PackedScene>("res://scenes/screens/card_library/card_library.tscn").Instantiate<NCardLibrary>();
            library.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            root.AddChild(library);
            var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
            library.Initialize(RunState.CreateForTest([player], seed: "LIBRARY-0710"));
            var misc = library.GetNode<NCardPoolFilter>("%MiscPool");
            var magic = misc.GetParent().GetNode<NCardPoolFilter>(FrierenLibraryFilter.MagicFilterName);
            var filters = (Dictionary<NCardPoolFilter, Func<CardModel, bool>>)AccessTools.Field(typeof(NCardLibrary), "_poolFilters").GetValue(library);
            var grid = library.GetNode<NCardLibraryGrid>("%CardGrid");
            // 只渲染本次相关卡池及两张原生衍生牌，避开游戏包未导出贴图的原型模型。
            var candidates = (List<CardModel>)AccessTools.Field(typeof(NCardLibraryGrid), "_allCards").GetValue(grid);
            candidates.RemoveAll(c => c.Pool is not FrierenCardPool && c is not MegaCrit.Sts2.Core.Models.Cards.Dazed && c is not MegaCrit.Sts2.Core.Models.Cards.Injury);
            Check(magic.Loc!.GetFormattedText() == (language == "zhs" ? "魔法因子和魔具" : "Magic Factors and Tools"), "独立入口名称：" + language);
            Check(magic.GetNode<TextureRect>("Image").Texture.ResourcePath == "res://FrierenMod/images/ui/magic_deck.png" &&
                magic.GetNode<TextureRect>("Image/Shadow").Texture == magic.GetNode<TextureRect>("Image").Texture, "图标及阴影复用魔法牌组图片");
            Check(magic.GetNode<TextureRect>("Image").Material != misc.GetNode<TextureRect>("Image").Material, "按钮材质独立，选择状态不会串色");
            var items = ModelDb.AllCards.Where(FrierenLibraryFilter.IsMagicItem).ToArray();
            Check(items.All(c => filters[magic](c) && !filters[misc](c)) && !filters[magic](ModelDb.Card<Mana>()), "仅因子和魔具进入新分类，魔力保留衍生归属");
            library.OnSubmenuOpened(); await Tick();
            magic.IsSelected = true; magic.EmitSignal(NCardPoolFilter.SignalName.Toggled, magic); await Tick();
            Check(grid.VisibleCards.Count() == items.Length && grid.VisibleCards.All(FrierenLibraryFilter.IsMagicItem) && filters.Keys.Count(c => c.IsSelected) == 1,
                "原生切换信号显示全部因子和八种魔具，并取消其他分类");
            var rows = (List<List<NGridCardHolder>>)AccessTools.Field(typeof(NCardGrid), "_cardRows").GetValue(grid);
            Check(rows.SelectMany(r => r).Any() && rows.SelectMany(r => r).All(h => h.CardNode is { Visibility: ModelVisibility.Visible } card && FrierenLibraryFilter.IsMagicItem(card.Model)),
                "实际可见卡面已切换到因子和魔具，非锁定或未知牌");
            var common = library.GetNode<NCardRarityTickbox>("%CommonRarity"); common.IsTicked = true;
            common.EmitSignal(NCardRarityTickbox.SignalName.Toggled, common); await Tick();
            Check(grid.VisibleCards.Any() && grid.VisibleCards.All(c => c.Rarity == CardRarity.Common), "新分类继续支持原生稀有度筛选");
            common.IsTicked = false; common.EmitSignal(NCardRarityTickbox.SignalName.Toggled, common); await Tick();
            misc.IsSelected = true; misc.EmitSignal(NCardPoolFilter.SignalName.Toggled, misc); await Tick();
            Check(grid.VisibleCards.All(c => !FrierenLibraryFilter.IsMagicItem(c)) && !magic.IsSelected, "返回衍生分类后不会残留因子与魔具");
            magic.IsSelected = true; magic.EmitSignal(NCardPoolFilter.SignalName.Toggled, magic); await Tick();
            if (OS.GetCmdlineUserArgs().Contains("--ui-snapshot")) root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/library-current-" + language + ".png"));
            library.OnSubmenuClosed(); await Tick(); library.QueueFree(); await Tick();
        }
        AccessTools.Property(typeof(NGame), nameof(NGame.Instance)).SetValue(null, null);
        game.QueueFree(); input.Free(); controller.Free(); startup.UnpatchAll(startup.Id);
        LocManager.Instance.SetLanguage("eng"); return count;
    }
}

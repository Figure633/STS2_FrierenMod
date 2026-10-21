using System.Text.RegularExpressions;
using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Presentation;
using FrierenMod.Scripts.Powers;
using FrierenMod.Scripts.UI;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Pooling;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;

public static class ChantSelectionTests
{
    private static bool SkipStartup() => false;
    private static bool TextHoverOnly(NCardHolder __instance) => __instance.CardModel is { } card && !card.HoverTips.Any(t => t is CardHoverTip);

    public static async Task<int> Run(Node root)
    {
        int count = 0;
        void Check(bool valid, string name) { if (!valid) throw new Exception(name); GD.Print("PASS 咏唱选择：" + name); count++; }
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "FRIEREN-CHANT-SELECTION");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = 1;
        var combat = new CombatState(runState: run); combat.AddPlayer(player);
        var enemy = combat.CreateCreature(ModelDb.Monster<MegaCrit.Sts2.Core.Models.Monsters.Axebot>().ToMutable(), CombatSide.Enemy, null);
        combat.AddCreature(enemy); CombatManager.Instance.SetUpCombat(combat);
        AccessTools.Property(typeof(CombatManager), nameof(CombatManager.IsInProgress)).SetValue(CombatManager.Instance, true);
        var context = new BlockingPlayerChoiceContext();
        var blockOption=ChantRules.Preview(ManaSlot.Draw,MagicDeckState.Definition("AdvancedDefensiveMagic"),2);
        var blockFactor=FactorCard.Create(player,"AdvancedDefensiveMagic",ManaSlot.Draw,blockOption);
        LocManager.Instance.SetLanguage("zhs");
        var dex=await PowerCmd.Apply<MegaCrit.Sts2.Core.Models.Powers.DexterityPower>(context,player.Creature,3,player.Creature,null);
        Check(blockFactor.GetDescriptionForPile(PileType.None).Contains("[green]5[/green]"),"咏唱因子按敏捷实时预览两次格挡并变绿");
        await PowerCmd.Remove(dex!);
        await PowerCmd.Apply<MegaCrit.Sts2.Core.Models.Powers.DexterityPower>(context,player.Creature,-3,player.Creature,null);
        Check(blockFactor.GetDescriptionForPile(PileType.None).Contains("[red]0[/red]"),"负敏捷预览降至零并变红");
        await PowerCmd.Remove(player.Creature.GetPower<MegaCrit.Sts2.Core.Models.Powers.DexterityPower>()!);
        Check(blockFactor.GetDescriptionForPile(PileType.None).Contains("2 点格挡两次") && blockFactor.Option==blockOption,
            "预览不改写因子基础奖励或支付费用");
        LocManager.Instance.SetLanguage("eng");
        T Card<T>() where T : CardModel { var c = ModelDb.Card<T>().ToMutable(); c.Owner = player; return (T)c; }
        var deck = MagicDeckState.CreatePrototypeLoadout(); MagicDeckStore.Write(player, deck);
        foreach (var pile in new[] { PileType.Draw, PileType.Discard })
            for (int i = 0; i < 7; i++) await CardPileCmd.Add(Card<Mana>(), pile);

        var choices = ChantCmd.Choices(player, 2);
        Check(choices.Count == 3 && choices.Select(c => c.Option.Slot).SequenceEqual(Enum.GetValues<ManaSlot>()) &&
            choices.Select(c => c.CanSelect).SequenceEqual(new[] { true, false, true }), "普通咏唱保留三槽，手牌无魔力时只禁用中间因子");
        Check(choices[1].Option.Cost == 2 && choices[1].Option.Reward == 2 && ChantCmd.Options(player, 2).Count == 2,
            "不可支付因子仍预览完整费用和效果，实际结算选项仅包含可用槽");
        var precise = ChantCmd.Choices(player, null);
        Check(precise[1].Option.Intensity == 0 && !precise[1].CanSelect && precise[0].Option.Intensity == 7,
            "精准咏唱分别计算每槽最大X，零魔力的槽仍保留");
        foreach (string id in new[] { "BigFire", "Factor010" }) {
            deck.Equip(deck.Add(id).InstanceId, ManaSlot.Draw); MagicDeckStore.Write(player, deck);
            var option = ChantCmd.Choices(player, null)[0];
            Check(option.CanSelect && option.Option.Intensity == (id == "BigFire" ? 3 : 2) && option.Option.Cost == 6,
                "精准咏唱七张魔力的实际费用和X：" + id);
        }
        deck = MagicDeckState.CreatePrototypeLoadout(); MagicDeckStore.Write(player, deck);
        await PowerCmd.Apply<ConcentrationPower>(context, player.Creature, 2, player.Creature, null);
        Check(ChantCmd.Choices(player, 2).All(c => c.CanSelect && c.Option.Cost == 0), "集中减费后零魔力槽恢复可选");
        await PowerCmd.Remove(player.Creature.GetPower<ConcentrationPower>()!);
        await PowerCmd.Apply<ChantWithoutManaTimesPower>(context, player.Creature, 1, player.Creature, null);
        Check(ChantCmd.Choices(player, 2).All(c => c.CanSelect && c.Option.Cost == 0), "免魔力咏唱不会错误禁用因子");
        await PowerCmd.Remove(player.Creature.GetPower<ChantWithoutManaTimesPower>()!);
        var pileCounts = player.PlayerCombatState!.AllPiles.Select(p => p.Cards.Count).ToArray();

        var tokens = ModelDb.CardPool<FrierenCardPool>().AllCards.Where(c => c.Rarity == CardRarity.Token &&
            c is not FactorCard and not MagicPropCard and not ChantChoice and not LegendaryCraftChoice and not PaperPlaneSpell).ToArray();
        Check(tokens.Length >= 12 && tokens.All(c => c.ShouldShowInCardLibrary && !FrierenLibraryFilter.IsCharacterCard(c)),
            "所有启用的衍生牌包含魔力、苍月草和额外防御，均可见且排除芙莉莲分类");
        var filter = new NCardPoolFilter();
        var misc = new NCardPoolFilter();
        var filters = new Dictionary<NCardPoolFilter, Func<CardModel, bool>> {
            [filter] = c => player.Character.CardPool.AllCardIds.Contains(c.Id),
            [misc] = c => (uint)(c.Rarity - CardRarity.Event) <= 4u
        };
        AccessTools.Method(typeof(FrierenLibraryFilter), "Apply").Invoke(null, [filters, new Dictionary<CharacterModel, NCardPoolFilter> { [player.Character] = filter }, misc]);
        Check(tokens.All(c => !filters[filter](c) && (uint)(c.Rarity - CardRarity.Event) <= 4u) && filters[filter](Card<HighDensityMana>()) && filters[filter](Card<RapidChant>()),
            "实际百科过滤器排除衍生牌，原生杂项收录；高密度魔力和普通角色卡保留");
        Check(!ModelDb.Card<PaperPlaneSpell>().ShouldShowInCardLibrary && !ModelDb.Card<TimeConcept>().ShouldShowInCardLibrary &&
            ModelDb.CardPool<FrierenCardPool>().AllCards.OfType<FactorCard>().All(c => c.ShouldShowInCardLibrary && !filters[filter](c) && filters[misc](c)), "兼容旧档的废弃牌继续隐藏，因子进入杂项分类");
        filter.Free(); misc.Free();

        // 使用真实原生卡牌、悬停场景与宿主；只跳过独立测试不需要的游戏启动。
        var harmony = new Harmony("frieren.tests.chant_selection");
        foreach (string method in new[] { nameof(NGame._EnterTree), nameof(NGame._Ready) })
            harmony.Patch(AccessTools.Method(typeof(NGame), method), prefix: new HarmonyMethod(typeof(ChantSelectionTests), nameof(SkipStartup)));
        harmony.Unpatch(AccessTools.Method(typeof(NCardHolder), "CreateHoverTips"), HarmonyPatchType.All, "frieren.tests.hover_host");
        harmony.Patch(AccessTools.Method(typeof(NCardHolder), "CreateHoverTips"), prefix: new HarmonyMethod(typeof(ChantSelectionTests), nameof(TextHoverOnly)));
        var game = new NGame { Size = new(1920, 1080), ProcessMode = Node.ProcessModeEnum.Disabled };
        var input = new NInputManager(); var controller = new NControllerManager();
        AccessTools.Property(typeof(NInputManager), nameof(NInputManager.ControllerManager)).SetValue(input, controller);
        AccessTools.Property(typeof(NGame), nameof(NGame.InputManager)).SetValue(game, input);
        root.AddChild(game); AccessTools.Property(typeof(NGame), nameof(NGame.Instance)).SetValue(null, game);
        var hoverHost = new Control { ZIndex = 100, MouseFilter = Control.MouseFilterEnum.Ignore }; game.AddChild(hoverHost);
        AccessTools.Property(typeof(NGame), nameof(NGame.HoverTipsContainer)).SetValue(game, hoverHost);
        var ui = new Control { Size = game.Size }; game.AddChild(ui);
        ui.AddChild(new ColorRect { Size = ui.Size, Color = new("17202c"), MouseFilter = Control.MouseFilterEnum.Ignore });
        NGridCardHolder Holder(CardModel model, Vector2 position) {
            var node = NodePool.Get<NCard>(); AccessTools.Property(typeof(NCard), nameof(NCard.Model)).SetValue(node, model);
            var holder = NodePool.Get<NGridCardHolder>(); holder.Call(NCardHolder.MethodName.SetCard, node); holder.Call(NGridCardHolder.MethodName.UpdateCardModel);
            holder.Position = position; holder.Scale = holder.SmallScale; ui.AddChild(holder);
            node.UpdateVisuals(PileType.None, CardPreviewMode.Normal); return holder;
        }
        async Task Tick() { for (int i = 0; i < 5; i++) await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame); }
        async Task Snapshot(string name) {
            await Tick(); if (OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))
                root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/" + name + ".png"));
        }
        void Click(NGridCardHolder holder, bool right = false) {
            holder.Hitbox.EmitSignal(NClickableControl.SignalName.Focused, holder.Hitbox);
            holder.Hitbox.EmitSignal(NClickableControl.SignalName.MousePressed, new InputEventMouseButton { ButtonIndex = right ? MouseButton.Right : MouseButton.Left, Pressed = true });
            holder.Hitbox.EmitSignal(NClickableControl.SignalName.MouseReleased, new InputEventMouseButton { ButtonIndex = right ? MouseButton.Right : MouseButton.Left });
        }
        var selection = new NChooseACardSelectionScreen();
        var holders = choices.Select((c, i) => Holder(FactorCard.Create(player, c.Option.SpellId, c.Option.Slot, c.Option, c.CanSelect), new(620 + 340 * i, 600))).ToArray();
        foreach (var holder in holders) {
            ChantSelectionPresentation.Decorate(holder);
            holder.Pressed += h => AccessTools.Method(typeof(NChooseACardSelectionScreen), "SelectHolder").Invoke(selection, [h]);
        }
        var completion = (TaskCompletionSource<IEnumerable<CardModel>>)AccessTools.Field(typeof(NChooseACardSelectionScreen), "_completionSource").GetValue(selection)!;
        await Tick();
        foreach (string lang in new[] { "zhs", "eng" }) {
            LocManager.Instance.SetLanguage(lang);
            foreach (var holder in holders) {
                holder.CardNode!.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
                var f = (FactorCard)holder.CardModel;
                holder.GetNode<Label>("ChantSlotHeader").Text = new LocString("frieren", $"CHANT_SLOT.{f.Slot}").GetFormattedText();
                string text = Regex.Replace(f.GetDescriptionForPile(PileType.None), @"\[[^\]]*\]", "");
                Check(!text.Contains('·') && text.Contains(lang == "zhs" ? "从" + new LocString("frieren", $"SLOT.{f.Slot}").GetFormattedText() + "消耗2张魔力" : "Consume 2 Mana from your"), "槽位与费用形成完整消耗句：" + lang + "/" + f.Slot);
                Check(holder.GetNode<Label>("ChantSlotHeader").Position.Y + 50 < -211 && holder.GetNode<Label>("ChantSlotHeader").Modulate.A == 1,
                    "槽位标题位于卡面上方且不随禁用因子淡化：" + f.Slot);
            }
            Check(holders[1].CardNode!.Modulate.A == 0.6f && holders[0].CardNode!.Modulate.A == 1 && holders[2].CardNode!.Modulate.A == 1,
                "只有魔力不足的因子呈半透明：" + lang);
            await Snapshot("chant-choice-" + lang);
        }
        Click(holders[1]); await Tick();
        holders[1]._GuiInput(new InputEventAction { Action = MegaInput.select, Pressed = true }); await Tick();
        AccessTools.Method(typeof(NChooseACardSelectionScreen), "SelectHolder").Invoke(selection, [holders[1]]);
        Check(!completion.Task.IsCompleted, "禁用因子阻止鼠标、手柄以及绕过输入直接发送选择");
        Check(player.PlayerCombatState.AllPiles.Select(p => p.Cards.Count).SequenceEqual(pileCounts), "查看和点击禁用因子不会消耗魔力");
        Click(holders[0]); await Tick();
        Check(completion.Task.IsCompletedSuccessfully && completion.Task.Result.Single() == holders[0].CardModel,
            "可用因子通过原生选择完成任务，保持索引同步所用模型");
        selection.Free();
        foreach (var holder in holders) holder.QueueFree(); await Tick();

        var probe = Holder(FactorCard.Create(player, choices[1].Option.SpellId, ManaSlot.Hand, choices[1].Option, false), new(960, 600));
        ChantSelectionPresentation.Decorate(probe); ChantSelectionPresentation.Decorate(probe);
        Check(probe.GetChildren().Count(n => n.Name == "ChantSlotHeader") == 1 && probe.CardNode!.Modulate.A == 0.6f,
            "重复刷新只保留一个槽位标题，禁用因子仍有六成不透明度");
        probe.ReassignToCard(Card<RapidChant>(), PileType.None, null, ModelVisibility.Visible);
        Check(!probe.HasNode("ChantSlotHeader") && !probe.HasMeta("FrierenChantChoice") && probe.CardNode!.Modulate.A == 1,
            "未归还对象池而直接换成百科普通卡时同样清理槽位与淡化状态");
        probe.ReassignToCard(FactorCard.Create(player, choices[1].Option.SpellId, ManaSlot.Hand, choices[1].Option, false),
            PileType.None, null, ModelVisibility.Visible);
        ChantSelectionPresentation.Decorate(probe);
        probe.QueueFreeSafely(); await Tick();
        Check(!probe.HasNode("ChantSlotHeader") && !probe.HasMeta("FrierenChantChoice"), "真实归还对象池时清除槽位标题和选择状态");
        var reused = Holder(Card<RapidChant>(), new(960, 600));
        Check(ReferenceEquals(reused, probe) && !reused.HasNode("ChantSlotHeader") && reused.CardNode!.Modulate.A == 1,
            "百科普通卡复用同一控件时没有槽位文字或透明度残留");
        await Snapshot("chant-reused-normal-card");
        reused.QueueFreeSafely(); await Tick();
        var book = new MagicBookScreen(ui, player, () => null);
        for (int i = 0; i < 2; i++) {
            book.Open(); await Tick();
            Check(book.View.FindChildren("ChantSlotHeader", "", true, false).Count == 0,
                "咏唱后第" + (i + 1) + "次打开魔法牌组没有额外标题重影");
            if (i == 0) await Snapshot("chant-book-no-ghosting");
            book.Close(); await Tick();
        }
        book.View.QueueFree(); await Tick();

        var textHolder = Holder(Card<FullAhead>(), new(720, 560));
        foreach (string lang in new[] { "zhs", "eng" }) {
            LocManager.Instance.SetLanguage(lang);
            AccessTools.Property(typeof(NCard), nameof(NCard.Model)).SetValue(textHolder.CardNode, Card<FullAhead>());
            textHolder.Call(NGridCardHolder.MethodName.UpdateCardModel);
            textHolder.CardNode!.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
            var chantTitle = new LocString("frieren", "TIP.CHANT.title").GetFormattedText();
            Check(Card<FullAhead>().HoverTips.OfType<HoverTip>().Any(t => t.Title == chantTitle) &&
                Card<OilSpell>().HoverTips.OfType<HoverTip>().Any(t => t.Title == chantTitle), "正文引用咏唱的能力牌及传说牌都提供咏唱规则：" + lang);
            textHolder.Hitbox.EmitSignal(NClickableControl.SignalName.Focused, textHolder.Hitbox); await Tick();
            Check(hoverHost.GetChildCount() == 1 && hoverHost.FindChildren("*", "Label", true, false).OfType<Label>().Any(l => l.Text == chantTitle),
                "真实鼠标悬停创建原生咏唱说明窗口：" + lang);
            await Snapshot("chant-hover-" + lang);
            textHolder.Hitbox.EmitSignal(NClickableControl.SignalName.Unfocused, textHolder.Hitbox); await Tick();
            Check(hoverHost.GetChildCount() == 0, "移开鼠标清除提示窗口：" + lang);
            AccessTools.Property(typeof(NCard), nameof(NCard.Model)).SetValue(textHolder.CardNode, Card<OilSpell>());
            textHolder.Call(NGridCardHolder.MethodName.UpdateCardModel);
            textHolder.CardNode!.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
            textHolder.Hitbox.EmitSignal(NClickableControl.SignalName.Focused, textHolder.Hitbox); await Tick();
            var titles = hoverHost.FindChildren("*", "Label", true, false).OfType<Label>().Select(l => l.Text).ToArray();
            Check(titles.Contains(chantTitle) && titles.Contains(new LocString("frieren", "TIP.LEGENDARY.title").GetFormattedText()),
                "传说魔法真实悬停同时显示传说魔法及咏唱解锁规则：" + lang);
            await Snapshot("legendary-hover-" + lang);
            textHolder.Hitbox.EmitSignal(NClickableControl.SignalName.Unfocused, textHolder.Hitbox); await Tick();
        }
        Check(ModelDb.Power<FullAheadPower>().HoverTips.OfType<HoverTip>().Any(t => t.Title == new LocString("frieren", "TIP.CHANT.title").GetFormattedText()),
            "能力图标引用咏唱时同样补充规则说明");
        textHolder.QueueFree(); await Tick();
        AccessTools.Property(typeof(NGame), nameof(NGame.Instance)).SetValue(null, null);
        game.QueueFree(); await Tick();
        input.Free(); controller.Free();
        foreach (var method in harmony.GetPatchedMethods().ToArray()) harmony.Unpatch(method, HarmonyPatchType.All, harmony.Id);
        LocManager.Instance.SetLanguage("eng");
        return count;
    }
}

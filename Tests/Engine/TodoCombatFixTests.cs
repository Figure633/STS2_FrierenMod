using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Monsters;
using FrierenMod.Scripts.Powers;
using FrierenMod.Scripts.Presentation;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Pooling;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Unlocks;

public static class TodoCombatFixTests
{
    private static readonly List<string> Spoken = [];
    private static bool CaptureTalk(LocString line) { Spoken.Add(line.GetFormattedText()); return false; }
    private static bool SkipEndTurn() => false;

    public static async Task<int> Run(Node root)
    {
        int checks = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception(name); checks++; GD.Print("PASS 0.7.6：" + name); }
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "TODO-COMBAT-076");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = 1;
        SaveManager.Instance.InitPrefsDataForTest();
        var combat = new CombatState(runState: run); combat.AddPlayer(player);
        var monster = (Mimic)ModelDb.Monster<Mimic>().ToMutable();
        var mimic = combat.CreateCreature(monster, CombatSide.Enemy, null); combat.AddCreature(mimic);
        CombatManager.Instance.SetUpCombat(combat);
        AccessTools.Property(typeof(CombatManager), nameof(CombatManager.IsInProgress)).SetValue(CombatManager.Instance, true);
        var context = new ThrowingPlayerChoiceContext();
        var resources = new ResourceInfo { EnergySpent = 0, EnergyValue = 0, StarsSpent = 0, StarValue = 0 };
        async Task Reset() {
            foreach (var power in player.Creature.Powers.ToArray()) await PowerCmd.Remove(power);
            foreach (var pile in player.PlayerCombatState!.AllPiles) foreach (var card in pile.Cards.ToArray()) pile.RemoveInternal(card);
            player.PlayerCombatState.Energy = 3; CombatManager.Instance.History.Clear();
        }
        async Task Play(CardModel card) {
            await CardPileCmd.Add(card, PileType.Hand);
            await card.OnPlayWrapper(context, null, false, resources, skipCardPileVisuals: true);
        }
        foreach (bool upgraded in new[] { false, true }) {
            await Reset(); await PowerCmd.Apply<SynchroWithoutManaPower>(context, player.Creature, 2, player.Creature, null);
            var mana = combat.CreateCard<Mana>(player); await Play(mana);
            Check(mana.Pile?.Type == PileType.Hand && player.PlayerCombatState!.Energy == 3 &&
                player.Creature.GetPowerAmount<SynchroWithoutManaPower>() == 1, "普通魔力回手不额外获得能量");
            var dense = combat.CreateCard<HighDensityMana>(player);
            if (upgraded) { dense.UpgradeInternal(); dense.FinalizeUpgradeInternal(); }
            int gain = upgraded ? 2 : 1;
            await Play(dense);
            Check(dense.Pile?.Type == PileType.Hand && player.PlayerCombatState!.Energy == 3 + gain &&
                !player.Creature.HasPower<SynchroWithoutManaPower>(), "最后一层回手仍恰好回能一次，升级=" + upgraded);
            await Play(dense);
            Check(dense.Pile?.Type == PileType.Exhaust && player.PlayerCombatState!.Energy == 3 + 2 * gain,
                "省吃俭用用完恢复普通消耗且恰好回能一次，升级=" + upgraded);
            await PowerCmd.Apply<SynchroWithoutManaPower>(context, player.Creature, 1, player.Creature, null);
            var payment = combat.CreateCard<HighDensityMana>(player);
            await CardPileCmd.Add(payment, PileType.Draw); await CardCmd.Exhaust(context, payment);
            Check(player.PlayerCombatState!.Energy == 4 + 2 * gain && player.Creature.GetPowerAmount<SynchroWithoutManaPower>() == 1,
                "咏唱等直接消耗只触发原有能量，不扣回手层数");
        }
        await Reset(); LocManager.Instance.SetLanguage("zhs");
        Spoken.Clear();
        var capture = new Harmony("frieren.tests.todo076.talk");
        var talkMethod = AccessTools.Method(typeof(TalkCmd), nameof(TalkCmd.Play));
        var endMethod = AccessTools.Method(typeof(PlayerCmd), nameof(PlayerCmd.EndTurn));
        capture.Patch(talkMethod, prefix: new HarmonyMethod(typeof(TodoCombatFixTests), nameof(CaptureTalk)));
        capture.Patch(endMethod, prefix: new HarmonyMethod(typeof(TodoCombatFixTests), nameof(SkipEndTurn)));
        try {
            monster.RollMove([player.Creature]); await monster.AfterAddedToRoom();
            var disguise = mimic.GetPower<MimicPower>()!;
            var play = new CardPlay { Card = combat.CreateCard<StrikeFrieren>(player), Target = mimic, ResultPile = PileType.Discard,
                IsAutoPlay = false, PlayIndex = 0, PlayCount = 1, Resources = resources };
            for (int i = 0; i < 4; i++) await disguise.AfterCardPlayedLate(context, play);
            Check(Spoken.Count == 0, "前四张牌不提前播放宝箱怪提示");
            await disguise.AfterCardPlayedLate(context, play);
            Check(Spoken.SequenceEqual(new[] { "[sine]嗝[/sine]" }) && !mimic.HasPower<MimicPower>(), "伪装问号触发时播放原生摇摆嗝气泡");
            await PowerCmd.Apply<SwallowedPower>(context, player.Creature, 6, mimic, null);
            await monster.PerformMove(); monster.RollMove([player.Creature]);
            await monster.PerformMove(); monster.RollMove([player.Creature]);
            Check(monster.NextMove.Id == "FULL", "吞噬后首次吃撑进入休息问号回合");
            await monster.PerformMove(); monster.RollMove([player.Creature]);
            Check(Spoken.Count == 2 && monster.NextMove.Id != "FULL", "一代吃撑回合保留嗝气泡并正常继续行动");
        } finally { capture.UnpatchAll(capture.Id); }
        var host = new Control { Size = new Vector2(1920, 1080) }; root.AddChild(host);
        host.AddChild(new ColorRect { Size = host.Size, Color = new Color("17202c"), MouseFilter = Control.MouseFilterEnum.Ignore });
        monster.RegisterSceneConversions(); var visual = monster.CreateVisuals();
        visual.Position = new Vector2(960, 420); host.AddChild(visual);
        var sprite = visual.GetNode<Sprite2D>("Visuals/Body"); var picture = sprite.Texture.GetImage();
        if (picture.IsCompressed()) picture.Decompress();
        var visible = picture.GetUsedRect();
        float bottom = sprite.Position.Y - picture.GetHeight() / 2f + visible.End.Y;
        Check(Mathf.Abs(bottom - visual.Bounds.GetRect().End.Y) <= 1 && visual.Bounds.Size == new Vector2(198, 234),
            "宝箱怪可见图像底边与原生血条定位的碰撞框底边对齐");
        Check(visual.IntentPosition.Position.Y < -234 && visual.TalkPosition!.Position.Y == -234, "意图与气泡随宝箱怪的可见图像定位");
        var rare = new CardModel[] { ModelDb.Card<Himmel>(), ModelDb.Card<CustomLegendarySpell>(), ModelDb.Card<Fatalism>(), ModelDb.Card<Repent>() };
        var nodes = new List<NCard>();
        var characterFilter = new NCardPoolFilter(); var miscFilter = new NCardPoolFilter();
        var filters = new Dictionary<NCardPoolFilter, Func<CardModel, bool>> { [characterFilter] = _ => true, [miscFilter] = _ => false };
        AccessTools.Method(typeof(FrierenLibraryFilter), "Apply").Invoke(null,
            [filters, new Dictionary<CharacterModel, NCardPoolFilter> { [player.Character] = characterFilter }, miscFilter]);
        var grid = new NCardLibraryGrid(); grid.RefreshVisibility();
        var visibility = AccessTools.Method(typeof(NCardLibraryGrid), "GetCardVisibility");
        foreach (var canonical in rare) {
            var model = canonical.ToMutable(); model.Owner = player;
            var node = NodePool.Get<NCard>(); AccessTools.Property(typeof(NCard), nameof(NCard.Model)).SetValue(node, model);
            node.Position = new Vector2(290 + nodes.Count * 445, 810); node.Scale = Vector2.One * 0.8f;
            host.AddChild(node); node.UpdateVisuals(PileType.None, CardPreviewMode.Normal); nodes.Add(node);
            Check(model.Rarity == CardRarity.Rare && model.BannerMaterial.ResourcePath.EndsWith("card_banner_rare_mat.tres"), canonical.Id.Entry + "金色稀有卡面");
            Check(filters[miscFilter](canonical) && !filters[characterFilter](canonical) &&
                (ModelVisibility)visibility.Invoke(grid, [canonical])! == ModelVisibility.Visible, canonical.Id.Entry + "完整显示在衍生分类");
            Check(!model.CanBeGeneratedInCombat && !model.CanBeGeneratedByModifiers &&
                !player.Character.CardPool.GetUnlockedCards(UnlockState.all, CardMultiplayerConstraint.None).Any(c => c.Id == model.Id),
                canonical.Id.Entry + "保持专属生成限制，不混入奖励及商店");
        }
        var clues = new[] { ModelDb.Power<Factor100Power>().Icon, ModelDb.Power<Factor010Power>().Icon, ModelDb.Power<Factor001Power>().Icon };
        Check(clues.Select(i => i.GetSize()).SequenceEqual(new[] { new Vector2(84, 82), new Vector2(84, 84), new Vector2(84, 84) }) &&
            clues.Select(i => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(i.GetImage().GetData()))).Distinct().Count() == 3,
            "谜题三段线索均有独立的一代原图，不是同一个占位图");
        for (int i = 0; i < 6; i++) await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
        if (OS.GetCmdlineUserArgs().Contains("--ui-snapshot")) root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/todo-combat-076.png"));
        foreach (var node in nodes) { host.RemoveChild(node); node.QueueFreeSafely(); }
        host.QueueFree(); characterFilter.Free(); miscFilter.Free(); grid.Free(); LocManager.Instance.SetLanguage("eng");
        for (int i = 0; i < 3; i++) await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
        RunManager.Instance.CleanUp(); return checks;
    }
}

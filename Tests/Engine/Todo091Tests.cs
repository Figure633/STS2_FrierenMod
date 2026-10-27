using System.Text.Json;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Encounters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using FrierenMod.Scripts.Relics;
using FrierenMod.Scripts.UI;
using FrierenMod.Scripts.Presentation;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Unlocks;

public static class Todo091Tests
{
    public static async Task<int> Run(Node root)
    {
        int count = 0;
        void Check(bool ok, string text) { if (!ok) throw new Exception(text); count++; GD.Print("PASS TODO091: " + text); }
        SaveManager.Instance.InitPrefsDataForTest();
        var positions = new HashSet<bool>();
        for (int seed = 0; seed < 12; seed++)
        {
            var bossPlayer = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
            var bossRun = RunState.CreateForTest([bossPlayer], acts: [ModelDb.Act<Glory>()], ascensionLevel: 10, seed: "BOSS091" + seed);
            RunManager.Instance.SetUpTest(bossRun, new NetSingleplayerGameService());
            RunManager.Instance.GenerateRooms();
            var bossAct = bossRun.Acts[0];
            bool first = bossAct.BossEncounter is SpiegelFrierenEncounter;
            Check(bossAct.SecondBossEncounter != null && first != (bossAct.SecondBossEncounter is SpiegelFrierenEncounter), "双Boss恰有一个复制体，种子" + seed);
            positions.Add(first);
            RunManager.Instance.CleanUp();
        }
        Check(positions.Count == 2, "复制体可随机出现在第一或第二Boss位置");
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "TODO091");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService());
        SaveManager.Instance.InitPrefsDataForTest();
        player.AddRelicInternal(ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.Byrdpip>().ToMutable());
        var auraRoom = new CombatRoom(ModelDb.Encounter<AuraEncounter>().ToMutable(), run);
        var auraRewards = (List<Reward>)AccessTools.Method(typeof(RewardsSet), "GenerateRewardsFor")
            .Invoke(new RewardsSet(player), [player, auraRoom])!;
        Check(auraRewards.OfType<RelicReward>().Count(r => r.Relic is AuraCompanion) == 1, "已有原生宠物仍可获得阿乌拉陪伴奖励");
        var pool = ModelDb.CardPool<FrierenCardPool>();
        var cards = pool.GetUnlockedCards(UnlockState.all, CardMultiplayerConstraint.SingleplayerOnly).ToArray();
        Check(cards.Length > 50 && cards.All(FrierenCardAvailability.IsRandomCandidate), "普通池仅保留可随机获取的正式牌");
        Check(!ModelDb.Card<DiscardChantChoice>().CanBeGeneratedInCombat && !ModelDb.Card<Mana>().CanBeGeneratedByModifiers &&
            !cards.Any(c => c is PaperPlaneSpell), "咏唱选项、魔力衍生牌、弃用纸飞机排除");
        Check(FrierenMusic.TrackTitle(FrierenMusic.Folder + "STS_Merchant_NewMix_v1.ogg") == "Grassy Turtles and Seed Rats", "商店曲名按原音频元数据核对");
        var act = ModelDb.Act<Glory>().ToMutable(); act.ApplyDiscoveryOrderModifications(UnlockState.all);
        Check(act.BossEncounter is SpiegelFrierenEncounter && act.AllBossEncounters.Count() > 1, "三层默认固定复制体，保留双Boss候选");
        LocManager.Instance.SetLanguage("zhs");
        var energyText = ModelDb.Relic<ComfortableBed>().DynamicDescription.GetFormattedText();
        Check(energyText.Contains("res://FrierenMod/images/energy/text_icon.png"), "未持有遗物预览能量图标指向有效资源");
        var apple = ModelDb.Card<RedAppleSpell>().ToMutable(); apple.UpgradeInternal();
        string appleText = apple.GetDescriptionForPile(PileType.None);
        Check(appleText.Contains("青苹果+") && appleText.Contains("[red]力量[/red]") && appleText.Contains("[green]敏捷[/green]"), "升级牌名及力量敏捷颜色");
        var deck = MagicDeckState.CreatePrototypeLoadout(); deck.AddProp("UnbelievableTool");
        var history = new RunHistoryPlayer { Id = 1, Character = player.Character.Id };
        MagicDeckHistory.Data.Set(history, deck.ToJson());
        string json = JsonSerializer.Serialize(history, JsonSerializationUtility.Options);
        var restored = JsonSerializer.Deserialize<RunHistoryPlayer>(json, JsonSerializationUtility.Options)!;
        Check(json.Contains("frieren_magic_deck") && MagicDeckHistory.Data.Get(restored) == deck.ToJson(), "原生历史JSON往返保留因子、装载与魔具");
        var legacy = JsonSerializer.Deserialize<RunHistoryPlayer>("{\"id\":1,\"character\":\"" + player.Character.Id + "\"}", JsonSerializationUtility.Options)!;
        Check(string.IsNullOrEmpty(MagicDeckHistory.Data.Get(legacy)), "旧历史缺失快照仍可读取");
        MagicDeckStore.Data.Set(player, deck.ToJson());
        var snapshotRun = new SerializableRun { Players = [player.ToSerializable()] };
        var snapshotHistory = new RunHistory { Players = [new RunHistoryPlayer { Id = 1, Character = player.Character.Id }] };
        var capture = AccessTools.Inner(typeof(MagicDeckHistory), "Capture");
        AccessTools.Method(capture,"Prefix").Invoke(null,[snapshotRun]);
        AccessTools.Method(AccessTools.Inner(typeof(MagicDeckHistory),"SaveSnapshot"),"Prefix").Invoke(null,[snapshotHistory]);
        AccessTools.Method(capture,"Finalizer").Invoke(null,[]);
        Check(MagicDeckHistory.Data.Get(snapshotHistory.Players[0]) == deck.ToJson(), "历史创建从已序列化玩家快照获取魔法牌组");
        var game = new MegaCrit.Sts2.Core.Nodes.NGame { Size = new Vector2(1920,1080) };
        var sceneContainer = new MegaCrit.Sts2.Core.Nodes.NSceneContainer(); game.AddChild(sceneContainer);
        AccessTools.Property(typeof(MegaCrit.Sts2.Core.Nodes.NGame), "RootSceneContainer").SetValue(game, sceneContainer);
        var input = new NInputManager(); var controller = new NControllerManager();
        AccessTools.Property(typeof(NInputManager), nameof(NInputManager.ControllerManager)).SetValue(input, controller);
        AccessTools.Property(typeof(MegaCrit.Sts2.Core.Nodes.NGame), "InputManager").SetValue(game, input);
        AccessTools.Property(typeof(MegaCrit.Sts2.Core.Nodes.NGame), "Instance").SetValue(null, game);
        var panel = new Control { Size = new Vector2(1920,1080) }; root.AddChild(panel);
        var book = new MagicBookScreen(panel, player, readOnly: true);
        AccessTools.Method(typeof(MagicBookScreen), "Arm").Invoke(book, [deck.Props.Single().InstanceId]);
        Check(MagicDeckStore.Read(player).ToJson() == deck.ToJson(), "历史只读魔法书不能使用魔具");
        book.View.QueueFree();
        var props = MagicPropRules.All.Take(2).Select(p => (CardModel)MagicPropCard.Create(player,p.Id)).ToArray();
        var shop = NSimpleCardSelectScreen.Create(props, new CardSelectorPrefs(new LocString("frieren","SHOP_SELECT"),2) { Cancelable = true });
        // 原生卡牌节点工厂在 TestMode 下返回 null；仅显示选牌界面时启用节点创建。
        MegaCrit.Sts2.Core.TestSupport.TestMode.IsOn = false;
        panel.AddChild(shop);
        for (int i=0; i<5; i++) await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
        MegaCrit.Sts2.Core.TestSupport.TestMode.IsOn = true;
        var back = shop.GetNode<NBackButton>("FrierenShopCancel");
        back.EmitSignal(NButton.SignalName.Released, back);
        var completion = (TaskCompletionSource<IEnumerable<CardModel>>)AccessTools.Field(typeof(NCardGridSelectionScreen),"_completionSource").GetValue(shop)!;
        Check(completion.Task.IsCompletedSuccessfully && !(await completion.Task).Any(), "原生魔具店返回按钮完成空选择");
        panel.QueueFree();
        var combat = new CombatState(runState: run); combat.AddPlayer(player);
        combat.AddCreature(combat.CreateCreature(ModelDb.Monster<MegaCrit.Sts2.Core.Models.Monsters.Axebot>().ToMutable(), CombatSide.Enemy, null));
        CombatManager.Instance.SetUpCombat(combat);
        AccessTools.Property(typeof(CombatManager), nameof(CombatManager.IsInProgress)).SetValue(CombatManager.Instance,true);
        var counterRoot = new Control { Size = new Vector2(1920,1080) }; root.AddChild(counterRoot);
        var selectionOverlay = new NSimpleCardSelectScreen();
        MagicBookUi.Attach(counterRoot, player, () => selectionOverlay, () => false);
        counterRoot.GetNode<Godot.Timer>("FrierenManaRefresh").EmitSignal(Godot.Timer.SignalName.Timeout);
        var counterHud = counterRoot.GetNode<Control>("FrierenManaCounter");
        Check(counterHud.Visible && counterHud.MouseBehaviorRecursive == Control.MouseBehaviorRecursiveEnum.Disabled,
            "原生选牌界面魔力计数可见，不抢占选牌输入");
        counterRoot.QueueFree(); selectionOverlay.Free();
        var context = new ThrowingPlayerChoiceContext();
        CardPlay Play(CardModel card) => new() { Card = card, Target = player.Creature, ResultPile = PileType.Discard,
            Resources = new ResourceInfo { EnergySpent = 0, EnergyValue = 0, StarsSpent = 0, StarValue = 0 }, PlayCount = 1, PlayIndex = 0, IsAutoPlay = false };
        var recycle = combat.CreateCard<Recycle>(player); var original = Play(recycle);
        await AccessTools.Method(typeof(Recycle), "OnPlay").Invoke(recycle, [context, original])!.AsTask();
        var power = player.Creature.GetPower<RecyclePower>()!;
        await power.AfterCardPlayed(context, original);
        Check(player.Creature.Block == 0 && recycle.Rarity == CardRarity.Uncommon, "蓝卡再循环不由自身首次触发");
        var next = Play(combat.CreateCard<Mana>(player)); await power.BeforeCardPlayed(next); await power.AfterCardPlayed(context,next);
        Check(player.Creature.Block == 2, "此后每张牌获得2格挡");
        await power.AfterSideTurnEnd(context, CombatSide.Player, [player.Creature]);
        Check(!player.Creature.HasPower<RecyclePower>(), "再循环回合结束清除");
        var room = new CombatRoom(combat);
        await PowerCmd.Apply<GainRelicPower>(context, player.Creature, 1, player.Creature, null);
        var relicPower = player.Creature.GetPower<GainRelicPower>()!;
        await relicPower.AfterCombatEnd(room); await relicPower.AfterCombatEnd(room);
        Check(room.ExtraRewards[player].OfType<RelicReward>().Count() == 1, "饰品魔法在能力清空前登记遗物，重复钩子不重复发放");
        var pet = AuraCompanionVisual.Create(); root.AddChild(pet);
        Check(pet.SpriteFrames.GetFrameCount("default") == 65 && pet.IsPlaying(), "宠物65帧以原速度一半循环，不使用战斗宠物栏");
        if (OS.GetCmdlineUserArgs().Contains("--ui-snapshot")) {
            var backdrop = new ColorRect { Color = new Color("263144"), Size = new Vector2(1920,1080), MouseFilter = Control.MouseFilterEnum.Ignore };
            root.AddChild(backdrop); root.MoveChild(backdrop,0);
            pet.Position = new Vector2(700,600); pet.Scale = new Vector2(1.4f,1.4f); pet.Pause(); pet.Frame = 0;
            for (int i=0;i<5;i++) await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
            root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/aura-pet.png"));
            backdrop.QueueFree();
        }
        pet.QueueFree();
        for (int i=0; i<5; i++) await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
        AccessTools.Property(typeof(MegaCrit.Sts2.Core.Nodes.NGame), "Instance").SetValue(null, null);
        game.Free(); input.Free(); controller.Free();
        RunManager.Instance.CleanUp(); LocManager.Instance.SetLanguage("eng");
        return count;
    }
    private static Task AsTask(this object value) => (Task)value;
}

using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Enchantments;
using FrierenMod.Scripts.Events;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using FrierenMod.Scripts.Relics;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.ValueProps;
using System.Text.Json;

public static class HollowKnightTests
{
    public static async Task<int> Run(Node root)
    {
        int checks = 0;
        void Check(bool ok, string text) { if (!ok) throw new Exception(text); checks++; GD.Print("PASS 空洞骑士：" + text); }
        async Task Tick(int n = 3) { for (int i = 0; i < n; i++) await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame); }
        SaveManager.Instance.InitPrefsDataForTest();
        var p = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var ally = Player.CreateForNewRun<Frieren>(UnlockState.all, 2);
        var run = RunState.CreateForTest([p, ally], seed: "HOLLOW-KNIGHT");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = 1;
        p.Creature.SetCurrentHpInternal(60);
        async Task<HollowKnightAdventure> Story() {
            var story = (HollowKnightAdventure)ModelDb.Event<HollowKnightAdventure>().ToMutable();
            await CombatTestState.BeginEvent(story, p); return story;
        }
        var canonical = ModelDb.Event<HollowKnightAdventure>();
        Check(canonical.IsAllowed(run) && ModelDb.AllSharedEvents.Contains(canonical), "接入芙莉莲跨幕随机事件池");
        var story = await Story();
        Check(story.CurrentOptions.Count == 3 && story.CurrentOptions[2].HoverTips.OfType<CardHoverTip>().Any(), "仅三个作者分支，赠卡有原生悬停预览");
        await story.CurrentOptions[0].Chosen();
        Check(story.IsFinished && p.GetRelic<KnightSecret>() != null && ally.GetRelic<KnightSecret>() == null, "奥义遗物只给选择该分支的玩家");
        var relic = p.GetRelic<KnightSecret>()!;
        Check(relic.Icon.GetWidth() > 0 && relic.IconOutline.GetWidth() > 0, "原图遗物和轮廓资源可加载");
        story = await Story(); int hp = p.Creature.CurrentHp;
        var selector = new TestCardSelector();
        using (CombatTestState.UseSelector(selector)) { selector.PrepareToSelect([0]); await story.CurrentOptions[1].Chosen(); }
        var enchanted = p.Deck.Cards.Single(c => c.Enchantment is MarkOfPride);
        Check(story.IsFinished && p.Creature.CurrentHp == hp - 9 && enchanted.Type == CardType.Attack && enchanted.TargetType == TargetType.AnyEnemy,
            "苦难之路造成9伤害并通过原生选择器永久附魔一张单体攻击");
        var loadedCard = CardModel.FromSerializable(enchanted.ToSerializable());
        Check(loadedCard.Enchantment is MarkOfPride, "附魔经原生卡牌存档恢复");
        var saved = p.ToSerializable(); var writer = new PacketWriter(); saved.Serialize(writer);
        var reader = new PacketReader(); reader.Reset(writer.Buffer); var packet = new SerializablePlayer(); packet.Deserialize(reader);
        var loaded = Player.FromSerializable(packet);
        Check(loaded.GetRelic<KnightSecret>() != null && loaded.Deck.Cards.Any(c => c.Enchantment is MarkOfPride) && reader.BitPosition == writer.BitPosition,
            "联机玩家数据往返保留遗物与永久附魔");
        int borrowed = p.Deck.Cards.OfType<BorrowedTime>().Count(); story = await Story(); await story.CurrentOptions[2].Chosen();
        Check(story.IsFinished && p.Deck.Cards.OfType<BorrowedTime>().Count() == borrowed + 1, "丝之歌分支获得原版预借时间");

        var combat = new CombatState(runState: run); combat.AddPlayer(p); combat.AddPlayer(ally);
        var enemies = Enumerable.Range(0, 3).Select(_ => combat.CreateCreature(ModelDb.Monster<Axebot>().ToMutable(), CombatSide.Enemy, null)).ToArray();
        foreach (var e in enemies) { combat.AddCreature(e); e.SetMaxHpInternal(9999); e.SetCurrentHpInternal(9999); }
        CombatManager.Instance.SetUpCombat(combat); CombatTestState.SetInProgress(true);
        var context = new ThrowingPlayerChoiceContext(); p.PlayerCombatState!.Energy = 10;
        await GameApiDamage(p, 4, ValueProp.Unblockable | ValueProp.Unpowered);
        Check(p.Creature.GetPower<ShapeOfUnnPower>()?.Amount == 1, "一次未格挡伤害获得一层乌恩之形");
        await CreatureCmd.GainBlock(p.Creature, 20, ValueProp.Unpowered, null);
        await GameApiDamage(p, 2, ValueProp.Move);
        Check(p.Creature.GetPower<ShapeOfUnnPower>()?.Amount == 1, "完全格挡不触发遗物");
        await GameApiDamage(p, 1, ValueProp.Unblockable | ValueProp.Unpowered);
        await GameApiDamage(p, 1, ValueProp.Unblockable | ValueProp.Unpowered);
        await GameApiDamage(ally, 2, ValueProp.Unblockable | ValueProp.Unpowered);
        var power = p.Creature.GetPower<ShapeOfUnnPower>()!;
        Check(power.Amount == 3 && !ally.Creature.HasPower<ShapeOfUnnPower>(), "多次伤害按次数叠层，队友受伤不触发");
        MagicDeckStore.Write(p, MagicDeckState.CreatePrototypeLoadout());
        await ManaCmd.Generate(p, PileType.Draw, 2);
        var chant = combat.CreateCard<RapidChant>(p);
        var play = CombatTestState.NewPlay(chant, null, PileType.Discard, new ResourceInfo { EnergySpent = 0, EnergyValue = 0, StarsSpent = 0, StarValue = 0 });
        hp = p.Creature.CurrentHp;
        await ChantCmd.Resolve(context, play, ChantCmd.Options(p, 1).Single(o => o.Slot == ManaSlot.Draw));
        Check(p.Creature.CurrentHp == hp + 3 && power.Amount == 3, "实际支付魔力并咏唱后按层数治疗，层数不消耗");
        hp = ally.Creature.CurrentHp;
        await power.AfterChant(context, CombatTestState.NewPlay(combat.CreateCard<RapidChant>(ally), null, PileType.Discard, play.Resources));
        Check(ally.Creature.CurrentHp == hp, "队友的咏唱不触发持有者治疗");

        var mark = ModelDb.Enchantment<MarkOfPride>();
        Check(!mark.CanEnchant(combat.CreateCard<DefendFrieren>(p)) && !mark.CanEnchant(combat.CreateCard<EndlessFlow>(p)), "技能和群体攻击不能获得骄傲印记");
        var attack = combat.CreateCard<StrikeFrieren>(p); CardCmd.Enchant<MarkOfPride>(attack, 1);
        Check(!mark.CanEnchant(attack), "保留原生附魔互斥限制");
        async Task Use(CardModel card, MegaCrit.Sts2.Core.Entities.Creatures.Creature target) {
            await CardPileCmd.Add(card, PileType.Hand);
            await card.OnPlayWrapper(context, target, false, new ResourceInfo { EnergySpent = 1, EnergyValue = 1, StarsSpent = 0, StarValue = 0 }, true);
        }
        int[] before = enemies.Select(e => e.CurrentHp).ToArray(); int energy = p.PlayerCombatState.Energy;
        await Use(attack, enemies[0]);
        int[] dealt = enemies.Select((e, i) => before[i] - e.CurrentHp).ToArray();
        Check(dealt[0] == 6 && dealt.Skip(1).Count(n => n == 6) == 1 && dealt.Sum() == 12,
            "原生实际出牌只追加一次，追加目标必须不同且不递归");
        Check(p.PlayerCombatState.Energy == energy && attack.Pile?.Type == PileType.Discard, "追加不支付能量且原卡正常进入弃牌堆");
        enemies[1].SetCurrentHpInternal(0); enemies[2].SetCurrentHpInternal(0);
        hp = enemies[0].CurrentHp; await Use(attack, enemies[0]);
        Check(enemies[0].CurrentHp == hp - 6, "仅剩原目标时不追加攻击");
        Check(mark.Icon.GetWidth() > 0 && power.Icon.GetWidth() > 0, "附魔与原版护符能力图标可加载");

        CombatTestState.SetInProgress(false); RunManager.Instance.CleanUp();
        var uiPlayer = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var uiRun = RunState.CreateForTest([uiPlayer], seed: "HOLLOW-KNIGHT-UI");
        RunManager.Instance.SetUpTest(uiRun, new NetSingleplayerGameService()); LocalContext.NetId = 1;
        var dir = ProjectSettings.GlobalizePath("res://../../artifacts/hollow-knight/native"); Directory.CreateDirectory(dir);
        foreach (var lang in new[] { "zhs", "eng" }) {
            LocManager.Instance.SetLanguage(lang);
            story = (HollowKnightAdventure)canonical.ToMutable(); await CombatTestState.BeginEvent(story, uiPlayer);
            Check(!story.Title.GetFormattedText().Contains("LOC_ERR") && story.CurrentOptions.All(o => !o.Description.GetFormattedText().Contains("LOC_ERR")), lang + "事件文案及三个选项完整");
            var host = new Control { Size = new(1920, 1080) }; root.AddChild(host);
            var layout = story.CreateScene().Instantiate<NEventLayout>(); host.AddChild(layout); story.SetNode(layout);
            layout.SetEvent(story); layout.SetTitle(story.Title.GetFormattedText()); layout.SetDescription(story.Description!.GetFormattedText());
            layout.AddOptions(story.CurrentOptions); layout.OnSetupComplete();
            await Tick();
            foreach (var tween in root.GetTree().GetProcessedTweens()) tween.CustomStep(10); await Tick();
            Check(layout.OptionButtons.Count() == 3 && layout.OptionButtons.All(b => b.Modulate.A > 0.9f && host.GetGlobalRect().Encloses(b.GetGlobalRect())),
                lang + "长文与三个可见选项都在视口内");
            if (OS.GetCmdlineUserArgs().Contains("--ui-snapshot")) root.GetViewport().GetTexture().GetImage().SavePng(Path.Combine(dir, lang + ".png"));
            host.QueueFree(); await Tick();
        }
        CombatTestState.SetInProgress(false); RunManager.Instance.CleanUp(); return checks;

        Task GameApiDamage(Player who, int amount, ValueProp props) => FrierenMod.Scripts.Shared.Compatibility.GameApi.Damage(context, who.Creature, amount, props, enemies[0], null);
    }
}

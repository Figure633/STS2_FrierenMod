using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Events;
using FrierenMod.Scripts.Mechanics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Pooling;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using System.Text.Json;

public static class BirdGrimoireTests
{
    public static async Task<int> Run(Node root)
    {
        int checks = 0;
        void Check(bool ok, string label) { if (!ok) throw new Exception(label); checks++; GD.Print("PASS 捕鸟魔法书：" + label); }
        SaveManager.Instance.InitPrefsDataForTest();
        var p = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var run = RunState.CreateForTest([p], seed: "BIRD-GRIMOIRE");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = 1;
        var deck = MagicDeckState.FromJson(JsonSerializer.Serialize(new MagicDeckSave(3,
            MagicDeckState.CreatePrototypeLoadout().Items.ToArray(), true, 3, StartingPropsGranted: true)));
        MagicDeckStore.Write(p, deck);
        int deckSize = p.Deck.Cards.Count;
        var story = (AnimalWell)ModelDb.Event<AnimalWell>().ToMutable(); await CombatTestState.BeginEvent(story, p);
        Check(story.CurrentOptions[2].HoverTips.OfType<CardHoverTip>().Any(), "事件奖励保留原生卡牌悬停预览");
        await story.CurrentOptions[2].Chosen(); deck = MagicDeckStore.Read(p);
        int id = deck.Props.Single(x => x.PropId == "BirdCapturingGrimoire").InstanceId;
        Check(story.IsFinished && p.Deck.Cards.Count == deckSize && !p.Deck.Cards.OfType<BirdCapturingSpell>().Any(), "事件奖励存入魔法牌组，不添加到主牌组");
        var book = MagicPropCard.Create(p, "BirdCapturingGrimoire");
        var spell = (BirdCapturingSpell)ModelDb.Card<BirdCapturingSpell>().ToMutable(); spell.Owner = p;
        Check(book.PortraitPath == spell.PortraitPath && GD.Load<Texture2D>(book.PortraitPath).GetWidth() > 0, "魔具与捕鸟魔法共用同一图片路径");
        Check(MagicPropRules.EventOnly.Any(r => r.Id == book.PropId) && !MagicPropRules.All.Any(r => r.Id == book.PropId), "魔法书为事件魔具，未加入营地随机池");
        string before = deck.ToJson();
        Check(!MagicPropCmd.CanUse(p, id) && !await MagicPropCmd.UseSelected(p, id, []) && MagicDeckStore.Read(p).ToJson() == before,
            "非战斗不能使用且不消耗魔具");
        var saved = JsonSerializer.Deserialize<SerializablePlayer>(JsonSerializer.Serialize(p.ToSerializable(), JsonSerializationUtility.Options), JsonSerializationUtility.Options)!;
        Check(MagicDeckStore.Read(Player.FromSerializable(saved)).Props.Any(x => x.PropId == book.PropId), "魔具原生玩家存档往返可恢复");
        var combat = new CombatState(runState: run); combat.AddPlayer(p);
        var enemy = combat.CreateCreature(ModelDb.Monster<Axebot>().ToMutable(), CombatSide.Enemy, null); combat.AddCreature(enemy);
        CombatManager.Instance.SetUpCombat(combat); CombatTestState.SetInProgress(true);
        AccessTools.Property(typeof(CombatManager), nameof(CombatManager.IsStarting)).SetValue(CombatManager.Instance, false);
        Check(MagicPropCmd.CanUse(p, id) && await MagicPropCmd.UseSelected(p, id, []), "玩家战斗阶段通过现有魔具入口使用");
        Check(!MagicDeckStore.Read(p).Props.Any(x => x.InstanceId == id) && p.PlayerCombatState!.Hand.Cards.OfType<BirdCapturingSpell>().Count() == 1 && p.Deck.Cards.Count == deckSize,
            "消耗魔具并原生生成一张捕鸟魔法到手牌，不改变主牌组");
        Check(!await MagicPropCmd.UseSelected(p, id, []) && p.PlayerCombatState!.Hand.Cards.OfType<BirdCapturingSpell>().Count() == 1,
            "重复使用已消耗实例不会重复生成");
        deck = MagicDeckStore.Read(p); id = deck.AddProp(book.PropId).InstanceId; MagicDeckStore.Write(p, deck);
        var request = new NetMagicDeckAction { Prop = id, Targets = [], Snapshot = MagicDeckNetwork.Snapshot(p),
            InCombat = true, Turn = p.PlayerCombatState!.TurnNumber };
        var writer = new PacketWriter(); request.Serialize(writer); var reader = new PacketReader(); reader.Reset(writer.Buffer);
        var recovered = new NetMagicDeckAction(); recovered.Deserialize(reader);
        var execute = AccessTools.Method(typeof(MagicPropCmd), "ExecuteSynchronized");
        bool ok = await (Task<bool>)execute.Invoke(null, [p, recovered, new ThrowingPlayerChoiceContext()])!;
        Check(ok && reader.BitPosition == writer.BitPosition && p.PlayerCombatState.Hand.Cards.OfType<BirdCapturingSpell>().Count() == 2,
            "联机请求序列化后经同步入口生成卡牌");
        Check(!await (Task<bool>)execute.Invoke(null, [p, recovered, new ThrowingPlayerChoiceContext()])!, "过期快照请求不重复生成");

        var host = new Control { Size = new(1920, 1080) }; root.AddChild(host);
        host.AddChild(new ColorRect { Size = host.Size, Color = new("17202c") });
        var nodes = new List<NCard>();
        foreach (var (card, x) in new (CardModel, int)[] { (book, 600), (spell, 1320) }) {
            var node = NodePool.Get<NCard>(); AccessTools.Property(typeof(NCard), nameof(NCard.Model)).SetValue(node, card);
            node.Position = new(x, 520); host.AddChild(node); node.UpdateVisuals(PileType.None, CardPreviewMode.Normal); nodes.Add(node);
        }
        foreach (var lang in new[] { "zhs", "eng" }) {
            LocManager.Instance.SetLanguage(lang);
            Check(!book.GetDescriptionForPile(PileType.None).Contains("LOC_ERR") &&
                book.GetDescriptionForPile(PileType.None) == new LocString("frieren", "PROP.BirdCapturingGrimoire").GetFormattedText(), lang + "魔具卡面和魔法书内说明一致");
            foreach (var node in nodes) node.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
            for (int i = 0; i < 5; i++) await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
            if (OS.GetCmdlineUserArgs().Contains("--ui-snapshot")) {
                var dir = ProjectSettings.GlobalizePath("res://../../artifacts/bird-grimoire"); Directory.CreateDirectory(dir);
                root.GetViewport().GetTexture().GetImage().SavePng(Path.Combine(dir, lang + ".png"));
            }
        }
        foreach (var node in nodes) { host.RemoveChild(node); node.QueueFree(); }
        host.QueueFree(); CombatTestState.SetInProgress(false); RunManager.Instance.CleanUp(); return checks;
    }
}

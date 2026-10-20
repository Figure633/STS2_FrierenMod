using System.Text.Json;
using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Unlocks;

public static class CardIdTests
{
    public static async Task<int> Run()
    {
        int count = 0;
        void Check(bool value, string name) { if (!value) throw new Exception(name); count++; GD.Print("PASS ID迁移：" + name); }
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "FRIEREN-CARD-IDS");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = 1;
        var combat = new CombatState(runState: run); combat.AddPlayer(player);
        var enemy = combat.CreateCreature(ModelDb.Monster<MegaCrit.Sts2.Core.Models.Monsters.Axebot>().ToMutable(), CombatSide.Enemy, null);
        combat.AddCreature(enemy);
        CombatManager.Instance.SetUpCombat(combat);
        AccessTools.Property(typeof(CombatManager), nameof(CombatManager.IsInProgress)).SetValue(CombatManager.Instance, true);
        var cases = new (Type Type, string OldName, string OldEntry, string NewEntry)[] {
            (typeof(ViolentExorcism), "Free", "FREE", "VIOLENT_EXORCISM"),
            (typeof(Envy), "HalfTheSky", "HALF_THE_SKY", "ENVY"),
            (typeof(MetalPetalSpell), "MetallicizFlower", "METALLICIZ_FLOWER", "METAL_PETAL_SPELL"),
            (typeof(SpeakFromHeartSpell), "SayThinkingSpell", "SAY_THINKING_SPELL", "SPEAK_FROM_HEART_SPELL"),
            (typeof(ReadyStance), "PreparedPosture", "PREPARED_POSTURE", "READY_STANCE"),
            (typeof(SustainedBarrage), "ContinuousShooting", "CONTINUOUS_SHOOTING", "SUSTAINED_BARRAGE")
        };
        var index = File.ReadLines(ProjectSettings.GlobalizePath("res://../../docs/inventory/card_names.jsonl"))
            .Select(line => JsonDocument.Parse(line).RootElement.Clone()).ToDictionary(row => row.GetProperty("model").GetString()!);
        foreach (var item in cases) {
            var id = ModelDb.GetId(item.Type);
            Check(id.ToString() == "CARD.FRIERENMOD-" + item.NewEntry, "实际注册新ID " + item.Type.Name);
            Check(item.Type.Assembly.GetType("FrierenMod.Scripts.Cards." + item.OldName) == null, "旧类不重复注册 " + item.OldName);
            var canonical = ModelDb.GetById<CardModel>(id);
            Check(ResourceLoader.Load<Texture2D>(canonical.PortraitPath)?.GetWidth() > 0, "资源包内新路径图片 " + item.Type.Name);
            var oldJson = JsonSerializer.Serialize("CARD.FRIERENMOD-" + item.OldEntry);
            var restoredId = JsonSerializer.Deserialize<ModelId>(oldJson, JsonSerializationUtility.Options)!;
            Check(restoredId == id && ModelDb.GetById<CardModel>(restoredId).GetType() == item.Type,
                "原生JSON读取旧ID " + item.OldName);
            for (int upgrade = 0; upgrade <= (canonical.MaxUpgradeLevel == 0 ? 0 : 1); upgrade++) {
                var card = combat.CreateCard(canonical, player);
                if (upgrade == 1) { card.UpgradeInternal(); card.FinalizeUpgradeInternal(); }
                string newJson = JsonSerializer.Serialize(card.ToSerializable(), JsonSerializationUtility.Options);
                string oldCardJson = newJson.Replace(id.ToString(), "CARD.FRIERENMOD-" + item.OldEntry);
                var restored = CardModel.FromSerializable(JsonSerializer.Deserialize<SerializableCard>(oldCardJson, JsonSerializationUtility.Options)!);
                Check(restored.GetType() == item.Type && restored.CurrentUpgradeLevel == upgrade,
                    "真实卡牌存档保留升级 " + item.Type.Name + "/" + upgrade);
                Check(JsonSerializer.Serialize(restored.ToSerializable(), JsonSerializationUtility.Options).Contains(id.ToString()),
                    "再次存档使用新ID " + item.Type.Name + "/" + upgrade);
                foreach (string language in new[] { "zhs", "eng" }) {
                    LocManager.Instance.SetLanguage(language);
                    Check(card.Title == index[item.Type.Name].GetProperty(language == "zhs" ? "chinese" : "english").GetString() + (upgrade == 1 ? "+" : ""),
                        "核定卡名 " + item.Type.Name + "/" + language + "/" + upgrade);
                    string description = card.GetDescriptionForPile(PileType.None);
                    Check(!string.IsNullOrWhiteSpace(description) && !description.Contains("{Effect}") && !description.Contains("FRIERENMOD-"),
                        "实际卡面文本 " + item.Type.Name + "/" + language + "/" + upgrade);
                }
            }
        }
        Check(ModelId.Deserialize("POWER.FRIERENMOD-FREE").Entry == "FRIERENMOD-FREE" &&
            ModelId.Deserialize("CARD.FREE").Entry == "FREE", "不改写其他模型或原版ID");
        var legacy = new MagicDeckSave(3, [new MagicItem(11, "MetallicizFlower", (int)ManaSlot.Draw)],
            Draft: ["MetallicizFlower", "IceArrows", "BePrepared"], NextId: 30);
        var deck = MagicDeckState.FromJson(JsonSerializer.Serialize(legacy));
        Check(deck.Items.Single() == new MagicItem(11, "MetalPetalSpell", (int)ManaSlot.Draw) &&
            deck.Draft[0] == "MetalPetalSpell" && deck.Add("BuildUpBody").InstanceId == 30,
            "旧因子库存和初始选项迁移，实例与槽位不变");
        Check(!deck.ToJson().Contains("MetallicizFlower") && MagicDeckState.FromJson(deck.ToJson()).ToJson() == deck.ToJson(),
            "因子存档往返使用新名");
        var preview = FactorCard.Create(player, "MetalPetalSpell", ManaSlot.Draw,
            ChantRules.Option(ManaSlot.Draw, 2, MagicDeckState.Definition("MetalPetalSpell"), 2)!.Value);
        Check(preview is MetalPetalSpell && preview.FactorId == "MetalPetalSpell", "新因子通过目录与反射创建");
        deck = MagicDeckState.CreatePrototypeLoadout();
        deck.Equip(deck.Add("MetalPetalSpell").InstanceId, ManaSlot.Draw); MagicDeckStore.Write(player, deck);
        for (int i = 0; i < 2; i++) await CardPileCmd.Add(combat.CreateCard<Mana>(player), PileType.Draw);
        var envy = combat.CreateCard<Envy>(player); await CardPileCmd.Add(envy, PileType.Discard);
        var chant = combat.CreateCard<RapidChant>(player);
        var play = new CardPlay { Card = chant, Target = null, IsAutoPlay = false, ResultPile = PileType.Discard, PlayCount = 1, PlayIndex = 0,
            Resources = new ResourceInfo { EnergySpent = 0, EnergyValue = 0, StarsSpent = 0, StarValue = 0 } };
        var option = ChantCmd.Options(player, 2).Single(x => x.SpellId == "MetalPetalSpell");
        await ChantCmd.Resolve(new BlockingPlayerChoiceContext(), play, option);
        Check(player.Creature.GetPowerAmount<MetallicizePower>() == 2 && !ManaCmd.InPile(player, PileType.Draw).Any(),
            "新因子实际咏唱支付与金属化结算");
        Check(envy.Pile?.Type == PileType.Hand, "Envy改名后仍响应咏唱返回手牌");
        LocManager.Instance.SetLanguage("eng");
        return count;
    }
}

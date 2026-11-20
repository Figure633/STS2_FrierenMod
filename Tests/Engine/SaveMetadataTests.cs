using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using FrierenMod.Core;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Events;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Rewards;
using FrierenMod.Scripts.UI;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Unlocks;

public static class SaveMetadataTests
{
    private static readonly ConditionalWeakTable<SerializableRun, JsonObject> BridgeData = new();
    private static JsonSerializerOptions? EarlyOptions;

    public static void CaptureOriginalOptions() => EarlyOptions = JsonSerializationUtility.Options;

    // 重现结城理 PersonaRunJsonBridge.VerifyRegistration：在 BaseLib 完成注册前
    // 创建原生 Run JSON 元数据，同时保留其他模组通过公开 modifier 添加的属性。
    // MakeReadOnly 模拟其他初始化序列化已使配置只读的情况。
    public static void CacheBeforeRegistration()
    {
        new Harmony("frieren.tests.persona_json_bridge").Patch(
            AccessTools.Method(typeof(JsonSerializationUtility), nameof(JsonSerializationUtility.AlphabetizeProperties)),
            postfix: new HarmonyMethod(typeof(SaveMetadataTests), nameof(ConfigureBridge)));
        EarlyOptions = JsonSerializationUtility.Options;
        EarlyOptions.MakeReadOnly();
        _ = JsonSerializationUtility.GetTypeInfo<SerializableRun>();
        if (EarlyOptions.GetTypeInfo(typeof(SerializablePlayer)).Properties.Any(p => p.Name == "save_dict_String"))
            throw new Exception("未成功重现 BaseLib 注册前的存档结构缓存。");
        GD.Print("SAVE_CACHE_REPRODUCED: 玩家字符串扩展字段尚未注册，元数据已缓存");
    }

    private static void ConfigureBridge(JsonTypeInfo info)
    {
        if (info.Type != typeof(SerializableRun)) return;
        var property = info.CreateJsonPropertyInfo(typeof(JsonObject), "_ritsulib");
        property.Get = o => BridgeData.GetValue((SerializableRun)o, _ => new JsonObject { ["persona"] = "test-persona" });
        property.Set = (o, v) => { BridgeData.Remove((SerializableRun)o); BridgeData.Add((SerializableRun)o, (JsonObject)v!); };
        info.Properties.Add(property);
    }

    public static void SimulateAndroidMetadataFailure()
    {
        // 手机日志中 BaseLib 在首次安装 PropInit 补丁时失败，后续玩家和奖励补丁均未安装。
        var context = AccessTools.TypeByName("MegaCrit.Sts2.Core.Saves.MegaCritSerializerContext");
        var harmony = new Harmony("frieren.tests.android_save");
        foreach (string type in new[] { "SerializablePlayer", "SerializableReward" })
        {
            var method = AccessTools.DeclaredMethod(context, type + "PropInit");
            var patches = Harmony.GetPatchInfo(method)!.Postfixes
                .Where(p => p.PatchMethod.DeclaringType?.DeclaringType == typeof(BaseLib.Patches.Saves.ExtendedSavePatches))
                .ToArray();
            if (patches.Length != 1) throw new Exception("未找到 BaseLib 的 " + type + " JSON 补丁");
            harmony.Unpatch(method, patches[0].PatchMethod);
        }

        var compatibility = typeof(MagicDeckHistory).Assembly.GetType("FrierenMod.Scripts.Shared.Compatibility.SaveMetadataCompatibility", true);
        harmony.Patch(AccessTools.DeclaredMethod(compatibility, "IsAndroid"),
            prefix: new HarmonyMethod(typeof(SaveMetadataTests), nameof(UseAndroid)));
        CacheBeforeRegistration();
    }

    private static bool UseAndroid(ref bool __result)
    {
        __result = true;
        return false;
    }

    public static async Task<int> Run(bool cached, bool android = false)
    {
        int checks = 0;
        void Check(bool ok, string text) { if (!ok) throw new Exception(text); checks++; GD.Print("PASS 存档兼容：" + text); }
        var options = JsonSerializationUtility.Options;
        Check(options.GetTypeInfo(typeof(SerializablePlayer)).Properties.Any(p => p.Name == "save_dict_String"), "完成注册后包含玩家字符串扩展存档");
        if (cached) Check(!ReferenceEquals(options, EarlyOptions), "仅在提前缓存导致缺字段时重新建立 JSON 结构");
        else Check(ReferenceEquals(options, EarlyOptions), "正常启动保持原生 JSON 配置");
        if (android)
        {
            var context = AccessTools.TypeByName("MegaCrit.Sts2.Core.Saves.MegaCritSerializerContext");
            var historyFactory = AccessTools.DeclaredMethod(context, "Create_RunHistoryPlayer");
            Check(Harmony.GetPatchInfo(historyFactory)?.Postfixes.Any(p => p.owner == "sts2.arkalin.frierenmod") != true,
                "Android 不改写原生历史 JSON 生成方法");
        }
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "FRIEREN-SAVE-METADATA");
        var deck = new MagicDeckState();
        for (int i = 0; i < 3; i++) { deck.PrepareDraft(_ => 0); deck.SelectInitial(deck.Draft[0]); }
        deck.GrantStartingProps();
        deck.Add("MetalPetalSpell");
        MagicDeckStore.Data.Set(player, deck.ToJson());
        CommissionCmd.Data.Set(player, new CommissionState { ClaimedRewards = ["MagicBread"] }.ToJson());
        DebtStore.Data.Set(player, 37);
        DebtCollector.Settled.Set(player, true);
        ReadingGrimoireEntrance.Stage.Set(player, 3);
        DragonBallStore.Write(player, new DragonBallState { Started = true, ShopBallClaimed = true });
        var save = new SerializableRun { Players = [player.ToSerializable()] };
        string json = JsonSerializationUtility.ToJson(save);
        Check(json.Contains("FrierenMagicDeck") && json.Contains("save_dict_Int32") && json.Contains("save_dict_Boolean"), "真实 Run JSON 写入字符串、整数和布尔扩展字段");
        var copy = JsonSerializationUtility.FromJson<SerializableRun>(json).SaveData!;
        for (int i = 0; i < 3; i++) {
            var restored = Player.FromSerializable(copy.Players[0]);
            Check(MagicDeckStore.Read(restored).ToJson() == deck.ToJson(), "重复SL保留全部因子、槽位、魔具和开局完成标记 " + i);
            Check(DebtStore.Read(restored) == 37 && DebtCollector.Settled.Get(restored) && ReadingGrimoireEntrance.Stage.Get(restored) == 3,
                "重复SL保留债务、事件状态和开局进度 " + i);
            Check(CommissionCmd.Read(restored).ClaimedRewards.Contains("MagicBread") && DragonBallStore.Read(restored).ShopBallClaimed,
                "重复SL保留委托和龙珠进度 " + i);
            if (cached) Check(BridgeData.TryGetValue(copy, out var bridge) && bridge["persona"]!.GetValue<string>() == "test-persona", "保留其他模组的 JSON bridge 数据 " + i);
            copy.Players[0] = restored.ToSerializable();
            copy = JsonSerializationUtility.FromJson<SerializableRun>(await JsonSerializationUtility.SerializeAsync(copy)).SaveData!;
        }
        if (android)
        {
            var history = new RunHistoryPlayer { Id = player.NetId, Character = player.Character.Id };
            MagicDeckHistory.Data.Set(history, deck.ToJson());
            var restoredHistory = JsonSerializer.Deserialize<RunHistoryPlayer>(JsonSerializer.Serialize(history, options), options)!;
            Check(MagicDeckHistory.Data.Get(restoredHistory) == deck.ToJson(), "Android 历史快照沿用原 JSON 字段");
        }
        var factor = new FactorReward(player) { Seed = 17 }; factor.Populate();
        var reward = JsonSerializer.Deserialize<SerializableReward>(JsonSerializer.Serialize(factor.ToSerializable(), options), options)!;
        Check(((FactorReward)Reward.FromSerializable(reward, player)).Choices.SequenceEqual(factor.Choices), "奖励 JSON 保留确切因子选项");
        var writer = new PacketWriter(); player.ToSerializable().Serialize(writer);
        var reader = new PacketReader(); reader.Reset(writer.Buffer);
        var packet = new SerializablePlayer(); packet.Deserialize(reader);
        Check(reader.BitPosition == writer.BitPosition && MagicDeckStore.Read(Player.FromSerializable(packet)).ToJson() == MagicDeckStore.Read(player).ToJson(),
            "原生玩家网络包保留魔法牌组且完整读完");
        return checks;
    }
}

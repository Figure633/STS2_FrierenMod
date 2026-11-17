using System.Runtime.Loader;
using FrierenMod.Scripts.Characters;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Modifiers;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Unlocks;

public static class HextechOpeningTests
{
    public static async Task<int> Run(Node root)
    {
        int checks = 0;
        void Check(bool ok, string text) { if (!ok) throw new Exception(text); checks++; GD.Print("PASS 海克斯开局：" + text); }
        string dll = System.Environment.GetEnvironmentVariable("FRIEREN_HEXTECH_DLL")
            ?? throw new Exception("请设置 FRIEREN_HEXTECH_DLL 为对应游戏版本的 HextechRunes.dll。");
        // 只加载真实第三方模型，不运行其全局初始化或读写玩家配置。
        var context = new AssemblyLoadContext("Frieren.HextechOpeningTests");
        context.Resolving += (_, name) => AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == name.Name);
        var type = context.LoadFromAssemblyPath(Path.GetFullPath(dll))
            .GetType("HextechRunes.HextechMayhemModifier", throwOnError: true)!;
        ModifierModel Hextech() => ((ModifierModel)Activator.CreateInstance(type, nonPublic: true)!).ToMutable();
        var manager = RunManager.Instance;
        SaveManager.Instance.InitPrefsDataForTest();
        foreach (var test in new[] {
            ("恐惧模式芙莉莲单开", true, false, false, GameMode.Dread, 1, true),
            ("恐惧模式海克斯双开", true, true, false, GameMode.Dread, 1, true),
            ("恐惧模式海克斯加自定义规则", true, true, true, GameMode.Dread, 1, false),
            ("芙莉莲单开", true, false, false, GameMode.Standard, 1, true),
            ("芙莉莲与海克斯双开", true, true, false, GameMode.Standard, 1, true),
            ("芙莉莲加原生自定义规则", true, false, true, GameMode.Standard, 1, false),
            ("海克斯加原生自定义规则", true, true, true, GameMode.Standard, 1, false),
            ("海克斯每日挑战", true, true, false, GameMode.Daily, 1, false),
            ("原版角色已有海克斯标记", false, true, false, GameMode.Standard, 1, false),
            ("联机原生开局", true, true, false, GameMode.Standard, 2, false)
        }) {
            var (name, frieren, hextech, extra, mode, players, relics) = test;
            Player owner = frieren ? Player.CreateForNewRun<Frieren>(UnlockState.all, 1)
                : Player.CreateForNewRun<Ironclad>(UnlockState.all, 1);
            var party = new List<Player> { owner };
            if (players > 1) party.Add(Player.CreateForNewRun<Ironclad>(UnlockState.all, 2));
            var run = RunState.CreateForTest(party, gameMode: mode, seed: "FRIEREN-HEXTECH-OPENING");
            manager.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = 1;
            // 与海克斯 StartRun 后加入标记、芙莉莲完成前置事件后才见涅奥的顺序相同。
            if (hextech) run.AddModifierDebug(Hextech());
            if (extra) run.AddModifierDebug(ModelDb.Modifier<Midas>().ToMutable());
            var before = run.Modifiers.ToArray();
            var neow = ModelDb.AncientEvent<Neow>().ToMutable();
            await CombatTestState.BeginEvent(neow, owner);
            Check(neow.CurrentOptions.Any(o => o.Relic != null) == relics, name + "保留应有的遗物选项规则");
            if (relics) Check(neow.CurrentOptions.Count == 3 && !neow.IsFinished &&
                neow.InitialDescription.LocEntryKey != "NEOW.EVENT.description", name + "三个祝福可选且使用普通开场正文");
            Check(run.Modifiers.SequenceEqual(before), name + "不删除、不替换本局模式标记");
            manager.CleanUp();
        }
        checks += await OpeningEventTests.Run(root, run => {
            var modifier = Hextech();
            modifier.OnRunLoaded(run);
            run.AddModifierDebug(modifier);
        });
        return checks;
    }
}

using System.Runtime.Loader;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;

public static class HextechCardPoolTests
{
    public static int Run()
    {
        int checks = 0;
        void Check(bool ok, string text) { if (!ok) throw new Exception(text); checks++; GD.Print("PASS 海克斯变牌：" + text); }
        var dll = System.Environment.GetEnvironmentVariable("FRIEREN_HEXTECH_DLL")!;
        var context = new AssemblyLoadContext("Frieren.HextechCardPoolTests");
        context.Resolving += (_, name) => AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == name.Name);
        var assembly = context.LoadFromAssemblyPath(Path.GetFullPath(dll));
        var type = assembly.GetType("HextechRunes.CardTransformUpgradeHelper", true)!;
        var filter = AccessTools.Method(type, "GetStableTransformationOptions");
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "HEXTECH-CARD-POOL");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService());
        try {
            var original = run.CreateCard(player.Character.StartingDeck.First(c => c.IsBasicStrikeOrDefend), player);
            var rares = player.Character.CardPool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint).Where(c => c.Rarity == CardRarity.Rare).ToArray();
            CardModel[] Options(bool combat) => (CardModel[])filter.Invoke(null, [original, rares, combat])!;
            Check(Options(false).Any(c => c is FactorCard) && Options(false).Any(c => c is IGeneratedOnlyCard), "真实海克斯魔盒候选复现因子与衍生牌混入");
            HextechCardPoolCompatibility.Install(assembly);
            var allowed = rares.Where(FrierenCardAvailability.IsRandomCandidate).Select(c => c.Id).ToHashSet();
            foreach (bool combat in new[] { false, true }) {
                var options = Options(combat);
                Check(options.Length > 0 && options.Select(c => c.Id).ToHashSet().SetEquals(rares.Where(FrierenCardAvailability.IsRandomCandidate).Where(c => !combat || c.CanBeGeneratedInCombat).Select(c => c.Id)), "过滤后仅保留全部正常稀有牌 combat=" + combat);
            }
            var create = AccessTools.Method(type, "CreateStableOptionTransformation");
            for (int i = 0; i < 50; i++) {
                var result = (CardTransformation)create.Invoke(null, [original, rares, run, "jinlian-box-rare-transform", i, Array.Empty<string>()])!;
                // 真实抽样结果由原生变牌结构承载，候选检查涵盖稀有因子与全部衍生牌。
                Check(allowed.Contains(result.Replacement.Id), "真实魔盒变牌随机路径可完成 " + i);
            }
        } finally { RunManager.Instance.CleanUp(); }
        return checks;
    }
}



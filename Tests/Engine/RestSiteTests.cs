using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Relics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;

public static class RestSiteTests
{
    public static async Task<int> Run()
    {
        int count = 0;
        void Check(bool condition, string name)
        {
            if (!condition) throw new Exception(name);
            GD.Print("PASS " + name); count++;
        }
        // 独立内存局：走原生命令与钩子，不进入用户存档或真实营地界面。
        RunManager.Instance.CleanUp();
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "FRIERENREST");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService());
        LocalContext.NetId = 1;
        AccessTools.Property(typeof(CombatManager), nameof(CombatManager.IsInProgress))
            .SetValue(CombatManager.Instance, false);
        run.PushRoom(new RestSiteRoom());
        var deck = MagicDeckState.CreatePrototypeLoadout();
        MagicDeckStore.Write(player, deck);
        string factors = MagicDeckStore.Read(player).ToJson();

        var options = RestSiteOption.Generate(player);
        Check(options.Select(o => o.OptionId).SequenceEqual(new[] { "HEAL", "SMITH", "FRIEREN_MAGIC_SHOP" }),
            "芙莉莲营地保留原生休息和锻造选项");
        player.Creature.SetMaxHpInternal(100);
        player.Creature.SetCurrentHpInternal(10);
        Check(await options.OfType<HealRestSiteOption>().Single().OnSelect() && player.Creature.CurrentHp == 40,
            "原生休息实际恢复最大生命的 30%");

        var pillow = await RelicCmd.Obtain<RegalPillow>(player);
        player.Creature.SetCurrentHpInternal(10);
        await new HealRestSiteOption(player).OnSelect();
        Check(player.Creature.CurrentHp == 55, "皇家枕头通过原生营地钩子追加 15 点治疗");
        player.Creature.SetCurrentHpInternal(10);
        await PlayerCmd.MimicRestSiteHeal(player, playSfx: false);
        Check(player.Creature.CurrentHp == 55, "原生事件复用的休息治疗命令保留皇家枕头加成");
        await RelicCmd.Remove(pillow);

        var selector = new TestCardSelector();
        using (CardSelectCmd.UseSelector(selector))
        {
            var chant = player.Deck.Cards.OfType<RapidChant>().Single();
            int index = player.Deck.Cards.Where(c => c.IsUpgradable).ToList().IndexOf(chant);
            selector.PrepareToSelect(new[] { index });
            Check(await new SmithRestSiteOption(player).OnSelect() && chant.IsUpgraded,
                "原生营地锻造可以选择并升级快速咏唱");
            int upgrades = player.Deck.Cards.Count(c => c.IsUpgraded);
            selector.PrepareToSelect(Array.Empty<int>());
            Check(!await new SmithRestSiteOption(player).OnSelect() &&
                player.Deck.Cards.Count(c => c.IsUpgraded) == upgrades,
                "取消锻造不升级卡牌，也不报告营地行动成功");
        }

        var shovel = await RelicCmd.Obtain<Shovel>(player);
        options = RestSiteOption.Generate(player);
        Check(options.OfType<DigRestSiteOption>().Count() == 1 && options.Count == 4,
            "铲子通过原生钩子为芙莉莲添加挖掘选项");
        await RelicCmd.Remove(shovel);
        Check(RestSiteOption.Generate(player).Count == 3, "移除铲子后不残留挖掘选项");
        await RelicCmd.Remove(player.GetRelic<HolyEmblem>()!);
        Check(MagicDeckStore.Read(player).ToJson() == factors,
            "营地操作与原生遗物移除流程不丢失已保存的因子库存");
        run.PopCurrentRoom();
        return count;
    }
}

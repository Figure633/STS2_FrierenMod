using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using System.Runtime.CompilerServices;

namespace FrierenMod.Scripts.Mechanics;

public static class MagicDeckCmd
{
    private static readonly ConditionalWeakTable<Player, SemaphoreSlim> SetupLocks = new();
    public static async Task ChooseStartingFactors(PlayerChoiceContext context, Player player)
    {
        // 无涅奥开局可能同时进入第一回合；同一玩家只显示一组初始选择。
        var gate = SetupLocks.GetValue(player, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try { await ChooseStartingFactorsCore(context, player); }
        finally { gate.Release(); }
    }

    private static async Task SaveSetup(Player player, MagicDeckState deck)
    {
        MagicDeckStore.Write(player, deck);
        if (RunManager.Instance.ShouldSave && !CombatManager.Instance.IsInProgress && !CombatManager.Instance.IsStarting)
            await SaveManager.Instance.SaveRun(null, saveProgress: false);
    }

    private static async Task ChooseStartingFactorsCore(PlayerChoiceContext context, Player player)
    {
        var deck = MagicDeckStore.Read(player);
        while (!deck.SetupCompleted)
        {
            deck.PrepareDraft(player.PlayerRng.Rewards.NextInt);
            await SaveSetup(player, deck);
            var cards = deck.Draft.Select(id => FactorCard.Create(player, id, (ManaSlot)deck.DraftSlot)).ToArray();
            var selected = await CardSelectCmd.FromChooseACardScreen(context, cards, player);
            if (selected is not FactorCard factor) return;
            deck.SelectInitial(factor.FactorId);
            await SaveSetup(player, deck);
            Log.Info($"Frieren: 初始槽位 {deck.DraftSlot} 装载 {factor.FactorId}");
        }
    }

    public static async Task<bool> ChooseReward(PlayerChoiceContext context, Player player, IReadOnlyList<string> ids)
    {
        var cards = ids.Select(id => FactorCard.Create(player, id)).ToArray();
        var selected = await CardSelectCmd.FromChooseACardScreen(context, cards, player, canSkip: true);
        if (selected is not FactorCard factor) return false;
        var deck = MagicDeckStore.Read(player);
        deck.Add(factor.FactorId);
        MagicDeckStore.Write(player, deck);
        return true;
    }
}

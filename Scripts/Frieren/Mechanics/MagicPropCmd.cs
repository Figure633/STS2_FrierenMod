using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace FrierenMod.Scripts.Mechanics;

public static class MagicPropCmd
{
    private static readonly HashSet<Player> Busy = [];
    public static bool IsBusy(Player player) => Busy.Contains(player);
    // IsComplete 仅表示奖励已领完，界面仍停留在“前进”；该阶段同样可以整理魔法牌组。
    // 选择具体卡牌时顶层会换成独立选牌屏，仍保护其选择任务。
    public static bool AllowsUseOver(IOverlayScreen? screen) => screen is null or NRewardsScreen;
    private static bool CanAct(Player player)
    {
        var combat = CombatManager.Instance;
        return player.Character is Frieren && player.RunState.Players.Count == 1 &&
            AllowsUseOver(NOverlayStack.Instance?.Peek()) &&
            MagicDeckStore.Read(player).SetupCompleted && !player.Creature.IsDead &&
            !combat.IsStarting && !combat.IsEnding && (!combat.IsInProgress ||
                (!combat.PlayerActionsDisabled && !combat.IsEnemyTurnStarted &&
                 !combat.EndingPlayerTurnPhaseOne && !combat.EndingPlayerTurnPhaseTwo &&
                 !RunManager.Instance.ActionExecutor.IsRunning));
    }
    public static bool CanUse(Player player, int propId)
    {
        if (IsBusy(player) || !CanAct(player)) return false;
        var deck = MagicDeckStore.Read(player);
        var prop = deck.Props.SingleOrDefault(p => p.InstanceId == propId);
        return prop != null && MagicPropRules.CanUse(deck, prop, CombatManager.Instance.IsInProgress);
    }

    public static async Task<bool> VisitShop(Player player)
    {
        if (!CanAct(player) || player.RunState.CurrentRoom is not RestSiteRoom || !Busy.Add(player)) return false;
        try
        {
            var cards = MagicPropRules.All.Select(p => (CardModel)MagicPropCard.Create(player, p.Id)).ToArray();
            var prefs = new CardSelectorPrefs(new LocString("frieren", "SHOP_SELECT"), 2)
                { Cancelable = true, RequireManualConfirmation = true };
            var chosen = (await CardSelectCmd.FromSimpleGrid(new BlockingPlayerChoiceContext(), cards, player, prefs)).ToArray();
            if (chosen.Length != 2 || chosen.Distinct().Count() != 2 || chosen.Except(cards).Any()) return false;
            var deck = MagicDeckStore.Read(player);
            foreach (var card in chosen.Cast<MagicPropCard>()) deck.AddProp(card.PropId);
            MagicDeckStore.Write(player, deck);
            // 与原生锻造一致，成功后由营地同步器消费此次行动，跑局统一保存。
            return true;
        }
        finally { Busy.Remove(player); }
    }

    // 魔法书内联选择使用唯一结算入口，重新检查当前库存和战斗阶段。
    public static async Task<bool> UseSelected(Player player, int propId, IReadOnlyList<int> targets)
    {
        if (!CanUse(player, propId) || !Busy.Add(player)) return false;
        try { return await ApplySelected(player, propId, targets); }
        finally { Busy.Remove(player); }
    }

    private static async Task<bool> ApplySelected(Player player, int propId, IReadOnlyList<int> targets)
    {
        if (!CanAct(player)) return false;
        var result = MagicPropRules.Apply(MagicDeckStore.Read(player), propId, targets,
            CombatManager.Instance.IsInProgress, player.PlayerRng.Rewards.NextInt);
        if (result == null) return false;
        string usedProp = MagicDeckStore.Read(player).Props.Single(p => p.InstanceId == propId).PropId;
        MagicDeckStore.Write(player, result.Deck, propId);
        if (usedProp == "UnbelievableTool") Presentation.FrierenAchievements.Unlock(player, "0006");
        if (usedProp == "SeinNote") Presentation.FrierenAchievements.Unlock(player, "4013");
        if (result.Heal > 0) await CreatureCmd.Heal(player.Creature, result.Heal);
        if (result.Energy > 0) await PlayerCmd.GainEnergy(result.Energy, player);
        await UI.MagicDeckAnimations.WhenIdle(player);
        // 与原生休息/锻造保持同一楼层检查点，避免重复领取营地收益。
        return true;
    }

}

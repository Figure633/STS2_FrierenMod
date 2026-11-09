using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Gold;
using FrierenMod.Scripts.Relics;

namespace FrierenMod.Scripts.Mechanics;

public static class MagicPropCmd
{
    private static readonly HashSet<Player> Busy = [];
    private static readonly HashSet<Player> Shopping = [];
    private static readonly HashSet<Player> ShopPending = [];
    private static readonly HashSet<Player> FreeShopChoices = [];
    internal static bool ConsumeFreeShopChoice(Player player) => FreeShopChoices.Remove(player);
    public static bool IsBusy(Player player) => Busy.Contains(player) || Shopping.Contains(player) || ShopPending.Contains(player);
    // IsComplete 仅表示奖励已领完，界面仍停留在“前进”；该阶段同样可以整理魔法牌组。
    // 选择具体卡牌时顶层会换成独立选牌屏，仍保护其选择任务。
    public static bool AllowsUseOver(IOverlayScreen? screen) => screen is null or NRewardsScreen;
    private static bool CanAct(Player player, bool fromInput = true)
    {
        var combat = CombatManager.Instance;
        return player.Character is Frieren && ReferenceEquals(RunManager.Instance.DebugOnlyGetState(), player.RunState) &&
            !RunManager.Instance.IsCleaningUp && (!fromInput || !RunManager.Instance.NetService.IsGameLoading) &&
            (!fromInput || player.RunState.Players.Count == 1 || LocalContext.IsMe(player)) &&
            (!fromInput || AllowsUseOver(NOverlayStack.Instance?.Peek())) &&
            MagicDeckStore.Read(player).SetupCompleted && !player.Creature.IsDead &&
            !combat.IsStarting && !combat.IsEnding && (!combat.IsInProgress ||
                (!combat.PlayerActionsDisabled && !combat.IsEnemyTurnStarted &&
                 !combat.EndingPlayerTurnPhaseOne && !combat.EndingPlayerTurnPhaseTwo &&
                 (!fromInput || !RunManager.Instance.ActionExecutor.IsRunning) &&
                 (player.RunState.Players.Count == 1 || player.PlayerCombatState?.Phase == PlayerTurnPhase.Play)));
    }
    public static bool CanUse(Player player, int propId)
    {
        if (IsBusy(player) || !CanAct(player)) return false;
        var deck = MagicDeckStore.Read(player);
        var prop = deck.Props.SingleOrDefault(p => p.InstanceId == propId);
        return prop != null && CanUseRule(player,deck,prop);
    }
    private static bool CanUseRule(Player player,MagicDeckState deck,MagicProp prop) =>
        prop.PropId!="MysticBox" && player.Gold>=MagicPropRules.Get(prop.PropId).GoldCost &&
        (prop.PropId!="MagicExamVial" || player.RunState.CurrentRoom is CombatRoom {RoomType:not RoomType.Boss}) &&
        MagicPropRules.CanUse(deck,prop,CombatManager.Instance.IsInProgress);

    public static async Task<bool> VisitShop(Player player)
    {
        if(player.RunState.Players.Count==1)return await VisitShopCore(player,new BlockingPlayerChoiceContext());
        ShopPending.Add(player);
        try {
            return LocalContext.IsMe(player)
                ? await MagicDeckNetwork.Send(player,new NetMagicDeckAction {Shop=true})
                : await MagicDeckNetwork.WaitForShop(player);
        }
        finally { ShopPending.Remove(player); }
    }

    internal static async Task<bool> VisitShopCore(Player player,PlayerChoiceContext context)
    {
        // 原生营地同步器在各端调用 OnSelect；远端不读取本机覆盖层和输入状态。
        if (!CanAct(player, fromInput: false) || CombatManager.Instance.IsInProgress ||
            player.RunState.CurrentRoom is not RestSiteRoom || !Shopping.Add(player)) return false;
        try
        {
            var cards = MagicPropRules.All.Select(p => (CardModel)MagicPropCard.Create(player, p.Id)).ToArray();
            var prefs = new CardSelectorPrefs(new LocString("frieren", "SHOP_SELECT"), 2)
                { Cancelable = true, RequireManualConfirmation = true };
            var chosen = (await CardSelectCmd.FromSimpleGrid(context, cards, player, prefs)).ToArray();
            if (chosen.Length != 2 || chosen.Distinct().Count() != 2 || chosen.Except(cards).Any()) return false;
            var deck = MagicDeckStore.Read(player);
            foreach (var card in chosen.Cast<MagicPropCard>()) deck.AddProp(card.PropId);
            MagicDeckStore.Write(player, deck);
            if(player.GetRelic<MagicShopMembership>() is {} membership){membership.Flash();FreeShopChoices.Add(player);}
            // 与原生锻造一致，成功后由营地同步器消费此次行动，跑局统一保存。
            return true;
        }
        finally { Shopping.Remove(player); }
    }

    // 魔法书内联选择使用唯一结算入口，重新检查当前库存和战斗阶段。
    public static async Task<bool> UseSelected(Player player, int propId, IReadOnlyList<int> targets)
    {
        if (targets.Count > 3 || !CanUse(player, propId) || !Busy.Add(player)) return false;
        try {
            bool success;
            if (player.RunState.Players.Count > 1) {
                success = await MagicDeckNetwork.Send(player, new NetMagicDeckAction {
                    Prop = propId, Targets = targets.ToArray(), Snapshot = MagicDeckNetwork.Snapshot(player),
                    InCombat = CombatManager.Instance.IsInProgress, Turn = player.PlayerCombatState?.TurnNumber ?? 0
                });
            }
            else success = await ApplySelected(player, propId, targets, new BlockingPlayerChoiceContext());
            // 动画只在本地等待，不进入同步动作，避免各端动画时长影响结算次序。
            if (success) await UI.MagicDeckAnimations.WhenIdle(player);
            return success;
        }
        finally { Busy.Remove(player); }
    }

    private static async Task<bool> ApplySelected(Player player, int propId, IReadOnlyList<int> targets, PlayerChoiceContext context)
    {
        if (Shopping.Contains(player) || !CanAct(player, fromInput: false)) return false;
        var original = MagicDeckStore.Read(player);
        var prop = original.Props.SingleOrDefault(p => p.InstanceId == propId);
        if (prop == null || !CanUseRule(player,original,prop)) return false;
        string? chosenFactor = null;
        if (prop.PropId == "RoyalGrimoire") {
            var choices = SpellCatalog.Available.OrderBy(f => f.Id, StringComparer.Ordinal)
                .Select(f => (CardModel)FactorCard.Create(player, f.Id)).ToArray();
            var selected = (await CardSelectCmd.FromSimpleGrid(context, choices, player,
                new CardSelectorPrefs(new LocString("frieren", "CARD_SELECT"), 1))).SingleOrDefault();
            if (selected is not FactorCard factor) return false;
            chosenFactor = factor.FactorId;
        }
        var result = MagicPropRules.Apply(MagicDeckStore.Read(player), propId, targets,
            CombatManager.Instance.IsInProgress, player.PlayerRng.Rewards.NextInt);
        if (result == null) return false;
        int goldCost=MagicPropRules.Get(prop.PropId).GoldCost;
        if(goldCost>0)await PlayerCmd.LoseGold(goldCost,player,GoldLossType.Spent);
        if (chosenFactor != null) result.Deck.Add(chosenFactor);
        string usedProp = MagicDeckStore.Read(player).Props.Single(p => p.InstanceId == propId).PropId;
        MagicDeckStore.Write(player, result.Deck, propId);
        if (usedProp == "SealingOre") await SealingOreCmd.Refresh(player);
        if (usedProp == "UnbelievableTool") Presentation.FrierenAchievements.Unlock(player, "0006");
        if (usedProp == "SeinNote") Presentation.FrierenAchievements.Unlock(player, "4013");
        if (result.Heal > 0) await CreatureCmd.Heal(player.Creature, result.Heal);
        if (result.Energy > 0) await PlayerCmd.GainEnergy(result.Energy, player);
        if (result.Draw > 0) await CardPileCmd.Draw(context,result.Draw,player);
        if (result.BirdSpell) await CardPileCmd.AddGeneratedCardToCombat(
            player.Creature.CombatState!.CreateCard<BirdCapturingSpell>(player), PileType.Hand, player);
        if(result.Escape){await CreatureCmd.Heal(player.Creature,decimal.Floor(player.Creature.MaxHp*.30m));await MagicExamEscape.Flee(player);}
        // 与原生休息/锻造保持同一楼层检查点，避免重复领取营地收益。
        return true;
    }

    internal static Task<bool> ExecuteSynchronized(Player player, NetMagicDeckAction request, PlayerChoiceContext? context = null)
    {
        // 旧库存、过期回合、跨战斗和不合法目标均不能消耗库存或随机数。
        if (request.Targets.Length > 3 || request.InCombat != CombatManager.Instance.IsInProgress ||
            (request.InCombat && request.Turn != player.PlayerCombatState?.TurnNumber) ||
            request.Snapshot != MagicDeckNetwork.Snapshot(player)) return Task.FromResult(false);
        return ApplySelected(player, request.Prop, request.Targets, context ?? new ThrowingPlayerChoiceContext());
    }

}

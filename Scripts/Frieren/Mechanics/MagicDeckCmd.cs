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
using System.Threading;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using Godot;
using FrierenMod.Scripts.UI;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rooms;
using FrierenMod.Scripts.Events;

namespace FrierenMod.Scripts.Mechanics;

public static class MagicDeckCmd
{
    private sealed class SetupSession
    {
        public readonly SemaphoreSlim Gate = new(1, 1);
        public readonly CancellationTokenSource Lifetime = new();
        public bool Active;
    }
    private static readonly ConditionalWeakTable<Player, SetupSession> SetupSessions = new();

    public static bool SetupMustWait => RunManager.Instance.NetService.IsGameLoading ||
        NGame.Instance?.Transition?.InTransition == true || CombatManager.Instance.IsStarting;

    public static bool StartingEventActive(Player player) => player.RunState.CurrentRoom is EventRoom { CanonicalEvent: ReadingGrimoireEvent };

    // 已有界面刷新在淡入及原生加载结束后恢复初始选择，不阻塞 LoadRun/房间转场。
    public static void TryResumeStartingFactors(Player player)
    {
        if (SetupMustWait || StartingEventActive(player) || RunManager.Instance.IsCleaningUp ||
            !ReferenceEquals(RunManager.Instance.DebugOnlyGetState(), player.RunState)) return;
        var combat = CombatManager.Instance;
        if (combat.IsInProgress && (combat.IsEnding || combat.PlayerActionsDisabled || combat.IsEnemyTurnStarted)) return;
        var deck = MagicDeckStore.Read(player);
        if (deck.SetupCompleted && deck.StartingPropsGranted && !FrierenTutorial.ShouldAutoShow(player)) return;
        var session = SetupSessions.GetValue(player, _ => new());
        if (session.Active || session.Lifetime.IsCancellationRequested) return;
        TaskHelper.RunSafely(ChooseStartingFactors(new BlockingPlayerChoiceContext(), player));
    }

    public static Task ChooseStartingFactors(PlayerChoiceContext context, Player player) => RunStartingSetup(context, player, false);

    public static Task<bool> ChooseStartingEventFactor(PlayerChoiceContext context, Player player) => RunStartingSetup(context, player, true);

    private static async Task<bool> RunStartingSetup(PlayerChoiceContext context, Player player, bool singleFactor)
    {
        // 开局事件每次按钮只选一本，圣徽和界面兜底不得抢先完成三次选择。
        if (!singleFactor && StartingEventActive(player)) return false;
        // 无涅奥开局可能同时进入第一回合；同一玩家只显示一组初始选择。
        var session = SetupSessions.GetValue(player, _ => new());
        bool entered = false;
        NMapScreen? hiddenMap = null;
        try {
            await session.Gate.WaitAsync(session.Lifetime.Token); entered = true; session.Active = true;
            if (!MagicDeckStore.Read(player).SetupCompleted && LocalContext.IsMe(player) && NMapScreen.Instance is { IsOpen: true } map) {
                hiddenMap = map;
                // 地图在原生屏幕上下文中的优先级高于选牌，必须经原生接口关闭后再弹出选项。
                map.Close(animateOut: false);
            }
            return await ChooseStartingFactorsCore(context, player, session.Lifetime.Token, singleFactor);
        }
        catch (OperationCanceledException) when (session.Lifetime.IsCancellationRequested) { return false; }
        finally {
            try {
                if (hiddenMap != null && GodotObject.IsInstanceValid(hiddenMap) && !session.Lifetime.IsCancellationRequested &&
                    ReferenceEquals(RunManager.Instance.DebugOnlyGetState(), player.RunState)) hiddenMap.Open();
            }
            finally { if (entered) { session.Active = false; session.Gate.Release(); } }
        }
    }

    private static async Task SaveSetup(Player player, MagicDeckState deck)
    {
        MagicDeckStore.Write(player, deck);
        if (RunManager.Instance.ShouldSave && !CombatManager.Instance.IsInProgress && !CombatManager.Instance.IsStarting)
            await SaveManager.Instance.SaveRun(null, saveProgress: false);
    }

    private static async Task<bool> ChooseStartingFactorsCore(PlayerChoiceContext context, Player player, CancellationToken cancellation, bool singleFactor)
    {
        var deck = MagicDeckStore.Read(player);
        if (!await FrierenTutorial.Show(player, cancellation: cancellation)) return false;
        while (!deck.SetupCompleted)
        {
            cancellation.ThrowIfCancellationRequested();
            deck = MagicDeckStore.Read(player);
            if (deck.SetupCompleted) break;
            deck.PrepareDraft(player.PlayerRng.Rewards.NextInt);
            await SaveSetup(player, deck);
            cancellation.ThrowIfCancellationRequested();
            var cards = deck.Draft.Select(id => FactorCard.Create(player, id, (ManaSlot)deck.DraftSlot)).ToArray();
            int slot = deck.DraftSlot;
            var offered = deck.Draft.ToArray();
            var selected = await CardSelectCmd.FromChooseACardScreen(context, cards, player).WaitAsync(cancellation);
            cancellation.ThrowIfCancellationRequested();
            if (selected is not FactorCard factor) return false;
            // 等待选牌期间的其他入库不能被旧快照覆盖；过期选择不能写入新跑局。
            if (!ReferenceEquals(RunManager.Instance.DebugOnlyGetState(), player.RunState)) return false;
            deck = MagicDeckStore.Read(player);
            if (deck.SetupCompleted || deck.DraftSlot != slot || !deck.Draft.SequenceEqual(offered)) return false;
            deck.SelectInitial(factor.FactorId);
            await SaveSetup(player, deck);
            Log.Info($"Frieren: 初始槽位 {deck.DraftSlot} 装载 {factor.FactorId}");
            if (singleFactor) return true;
        }
        cancellation.ThrowIfCancellationRequested();
        if (!singleFactor) await GrantStartingProps(player);
        return true;
    }

    public static async Task GrantStartingProps(Player player)
    {
        var deck = MagicDeckStore.Read(player);
        if (deck.GrantStartingProps()) await SaveSetup(player, deck);
    }

    [HarmonyPatch(typeof(RunManager), nameof(RunManager.CleanUp))]
    private static class CancelInitialSelection
    {
        private static void Prefix(RunManager __instance)
        {
            var state = __instance.DebugOnlyGetState();
            if (state == null) return;
            if (NModalContainer.Instance?.OpenModal is FrierenTutorial tutorial) tutorial.Finish(false);
            foreach (var player in state.Players)
                if (SetupSessions.TryGetValue(player, out var session)) session.Lifetime.Cancel();
        }
    }

    public static async Task<bool> ChooseReward(PlayerChoiceContext context, Player player, IReadOnlyList<string> ids)
    {
        var cards = ids.Select(id => MagicPropRules.All.Any(p => p.Id == id)
            ? (MegaCrit.Sts2.Core.Models.CardModel)MagicPropCard.Create(player, id) : FactorCard.Create(player, id)).ToArray();
        var selected = await CardSelectCmd.FromChooseACardScreen(context, cards, player, canSkip: true);
        // 统一使用已有的魔法物品入库入口，保留所属玩家与原生飞入动画。
        return selected != null && cards.Contains(selected) && MagicItemAcquisition.Receive(selected);
    }
}

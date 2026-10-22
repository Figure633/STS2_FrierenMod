using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Pooling;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;

public static class MagicDeckPlayabilityTests
{
    public static async Task<int> Run(Node root)
    {
        int count = 0, notifications = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception(name); GD.Print("PASS 因子状态刷新：" + name); count++; }
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "MAGIC-DECK-PLAYABILITY");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = 1;
        var combat = new CombatState(runState: run); combat.AddPlayer(player);
        combat.AddCreature(combat.CreateCreature(ModelDb.Monster<MegaCrit.Sts2.Core.Models.Monsters.Axebot>().ToMutable(), CombatSide.Enemy, null));
        CombatManager.Instance.SetUpCombat(combat);
        AccessTools.Property(typeof(CombatManager), nameof(CombatManager.IsInProgress)).SetValue(CombatManager.Instance, true);
        player.PlayerCombatState!.Energy = 10;
        for (int i=0;i<2;i++) await CardPileCmd.Add(combat.CreateCard<Mana>(player), PileType.Draw);
        var deck = new MagicDeckState();
        int costly = deck.Add("SpellBoost").InstanceId;
        int cheap = deck.Add("AdvancedDefensiveMagic").InstanceId;
        deck.Equip(costly, ManaSlot.Draw); MagicDeckStore.Write(player, deck);
        var hand = new NPlayerHand(); // 仅供原生 holder 查询选择模式，不挂入场景或订阅战斗事件。
        var holders = new List<NHandCardHolder>();
        foreach (var model in new CardModel[] { combat.CreateCard<RapidChant>(player),
            combat.CreateCard<PreciseChant>(player), combat.CreateCard<DefensiveMagic>(player) }) {
            await CardPileCmd.Add(model, PileType.Hand);
            var node = NodePool.Get<NCard>(); AccessTools.Property(typeof(NCard), nameof(NCard.Model)).SetValue(node, model);
            var holder = NHandCardHolder.Create(node, hand);
            holder.Position = new(400 + holders.Count * 350, 500); root.AddChild(holder); holders.Add(holder);
        }
        bool Blocked() => holders.All(h => !h.CardModel!.CanPlay() && h.CardNode!.GetNode<Control>("%UnplayableEnergyIcon").Visible);
        bool Ready() => holders.All(h => h.CardModel!.CanPlay() && !h.CardNode!.GetNode<Control>("%UnplayableEnergyIcon").Visible);
        Check(Blocked(), "初始高费因子下三种咏唱牌均显示不可用");
        void OnChanged(CombatState state) {
            if (state != combat) throw new Exception("刷新了其他战斗");
            notifications++;
            // 与原生 NPlayerHand.OnCombatStateChanged 相同的手牌刷新入口，执行真实视觉逻辑。
            foreach (var holder in holders) holder.UpdateCard();
        }
        CombatManager.Instance.StateTracker.CombatStateChanged += OnChanged;
        async Task Commit(MagicDeckState changed) {
            // 仅此短窗口启用真实 StateTracker 通知；存档仍由先前初始化的内存 IO 承担。
            // 原生 TestMode 禁止任何 UI 订阅该事件，故不能在该窗口保留其抑制逻辑。
            TestMode.IsOn = false;
            try {
                MagicDeckStore.Write(player, changed);
                for (int i=0;i<3;i++) await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            finally { TestMode.IsOn = true; }
        }
        try {
            deck.Equip(cheap, ManaSlot.Draw); await Commit(deck);
            Check(notifications==1 && Ready(), "替换低费因子后原生状态事件立即清除不可用图标");
            Check(ManaCmd.Total(player)==2 && player.PlayerCombatState.Energy==10 && player.PlayerCombatState.Hand.Cards.Count==3,
                "刷新不支付魔力或能量，也不移动手牌");
            deck.Equip(costly, ManaSlot.Draw); await Commit(deck);
            Check(notifications==2 && Blocked(), "反向替换高费因子后立即恢复不可用图标");
            await Commit(deck);
            deck.Add("BuildUpBody"); deck.RewardPity++; await Commit(deck);
            Check(notifications==2, "相同装载、库存新增及掉落计数变化不重复刷新手牌");
            deck.Equip(cheap, ManaSlot.Hand);
            int crystal = deck.AddProp("CrystalBall").InstanceId;
            await Commit(deck);
            int previous = notifications;
            var swapped = MagicPropRules.Apply(deck, crystal, [costly, cheap], true, _ => 0)!;
            deck = swapped.Deck; await Commit(deck);
            Check(notifications==previous+1 && Ready() && deck.Props.All(p=>p.InstanceId!=crystal),
                "水晶球互换槽位同样刷新并只消耗一次魔具");
            deck.Remove(cheap); await Commit(deck);
            Check(Blocked(), "移除已装载因子后立即更新咏唱状态");
            int blueprint = deck.Add("BluePrint").InstanceId;
            deck.Equip(blueprint, ManaSlot.Draw); await Commit(deck);
            Check(Blocked(), "蓝图引用高费手牌槽因子时仍不可用");
            int replacement = deck.Add("AdvancedDefensiveMagic").InstanceId;
            deck.Equip(replacement, ManaSlot.Hand); await Commit(deck);
            Check(Ready(), "修改蓝图引用的其他槽位后也立即更新");
            AccessTools.Property(typeof(CombatManager), nameof(CombatManager.IsInProgress)).SetValue(CombatManager.Instance, false);
            previous=notifications; deck.Equip(costly, ManaSlot.Hand); await Commit(deck);
            Check(notifications==previous && MagicDeckStore.Read(player).InSlot(ManaSlot.Hand)!.Id=="SpellBoost",
                "战斗外照常保存装载且不发送战斗刷新");
        }
        finally {
            TestMode.IsOn = true;
            CombatManager.Instance.StateTracker.CombatStateChanged -= OnChanged;
            foreach (var holder in holders) holder.QueueFree();
            await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
            hand.Free();
        }
        return count;
    }
}

using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace FrierenMod.Scripts.Enchantments;

public sealed class HamonOverdrive : CustomEnchantmentModel
{
    protected override string CustomIconPath => "res://FrierenMod/images/enchantments/HamonOverdrive.svg";
    public override bool HasExtraCardText => true;
    // 与普通条件卡共用原生金色发光；每次查询都按当前手牌顺序计算。
    public override bool ShouldGlowGold => HasCard && Status == EnchantmentStatus.Normal && IsCentral(Card);
    public static bool IsCentral(CardModel card)
    {
        if (card.CombatState == null || card.Pile is not { Type: PileType.Hand } hand) return false;
        int index = -1;
        for (int i = 0; i < hand.Cards.Count; i++) if (hand.Cards[i] == card) { index = i; break; }
        return index >= 0 && (index == (hand.Cards.Count - 1) / 2 || index == hand.Cards.Count / 2);
    }
    // 牌在原生BeforeCardPlayed之前就会离开手牌；在OnPlayWrapper入口记录位置。
    // 每层独立记录，嵌套自动打出不能覆盖外层手动打出的条件。
    internal sealed class Receipt(bool eligible) { public bool Eligible { get; } = eligible; public bool Refunded; }
    private List<Receipt> _receipts = [];
    internal Receipt Begin(bool autoPlay)
    {
        var receipt = new Receipt(!autoPlay && ShouldGlowGold); _receipts.Add(receipt); return receipt;
    }
    internal void End(Receipt receipt) => _receipts.Remove(receipt);
    public override async Task OnPlay(PlayerChoiceContext context, CardPlay? play)
    {
        if (play == null || play.Card != Card || !play.IsFirstInSeries || _receipts.Count == 0) return;
        var receipt = _receipts[^1];
        if (!receipt.Eligible || receipt.Refunded || Status == EnchantmentStatus.Disabled) return;
        receipt.Refunded = true;
        if (play.Resources.EnergySpent > 0) await PlayerCmd.GainEnergy(play.Resources.EnergySpent, Card.Owner);
    }
    protected override void DeepCloneFields() { base.DeepCloneFields(); _receipts = []; }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
    private static class CapturePosition
    {
        private static void Prefix(CardModel __instance, bool isAutoPlay, out Receipt? __state)
            => __state = (__instance.Enchantment as HamonOverdrive)?.Begin(isAutoPlay);
        private static void Postfix(CardModel __instance, Receipt? __state, ref Task __result)
        {
            if (__state != null && __instance.Enchantment is HamonOverdrive enchantment)
                __result = Complete(__result, enchantment, __state);
        }
        private static async Task Complete(Task original, HamonOverdrive enchantment, Receipt receipt)
        {
            try { await original; }
            finally { enchantment.End(receipt); }
        }
    }
}

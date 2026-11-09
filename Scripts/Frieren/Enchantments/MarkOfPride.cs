using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace FrierenMod.Scripts.Enchantments;

public sealed class MarkOfPride : CustomEnchantmentModel
{
    private bool _replaying;
    protected override string CustomIconPath => "res://FrierenMod/images/enchantments/MarkOfPride.png";
    public override bool HasExtraCardText => true;
    public override bool CanEnchantCardType(CardType type) => type == CardType.Attack;
    public override bool CanEnchant(CardModel card) => base.CanEnchant(card) && card.TargetType == TargetType.AnyEnemy;
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (_replaying || play.Card != Card || play.PlayIndex != play.PlayCount - 1 || CombatManager.Instance.IsOverOrEnding) return;
        var targets = Card.CombatState!.HittableEnemies.Where(enemy => enemy != play.Target).ToArray();
        if (targets.Length == 0) return;
        var target = Card.Owner.RunState.Rng.CombatTargets.NextItem(targets);
        _replaying = true;
        try { await CardCmd.AutoPlay(context, Card, target, skipXCapture: true); }
        finally { _replaying = false; }
    }
    protected override void DeepCloneFields() { base.DeepCloneFields(); _replaying = false; }
}

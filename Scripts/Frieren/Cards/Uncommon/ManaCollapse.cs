using FrierenMod.Core;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Cards;

public sealed class ManaCollapse() : FrierenCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(9, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        bool generateMana = CombatManager.Instance.History.CardPlaysStarted.Count(e => e.Actor == Owner.Creature && e.HappenedThisTurn(CombatState)) <= 10;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target!).Execute(context);
        if (generateMana) await ManaCmd.Generate(Owner, PileType.Hand, 2);
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(3); }
    public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(CardModel card, bool isAutoPlay, ResourceInfo resources, PileType pileType, CardPilePosition position) =>
        card == this && CombatManager.Instance.History.CardPlaysStarted.Count(e => e.Actor == Owner.Creature && e.HappenedThisTurn(CombatState)) < 10 && pileType == PileType.Discard ? (PileType.Draw, CardPilePosition.Random) : (pileType, position);
}

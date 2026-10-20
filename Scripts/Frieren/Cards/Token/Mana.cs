using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Presentation;
using MegaCrit.Sts2.Core.Localization;

namespace FrierenMod.Scripts.Cards;

// 普通魔力与高密度魔力共用同调结算。
public sealed class Mana() : FrierenCard(0, CardType.Status, CardRarity.Token, TargetType.Self), IManaCard
{
    protected override string ArtName => SynchroPresentation.ArtName(this);
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("Synchro", new LocString("frieren", $"TIP.{SynchroPresentation.Key(this)}.title").GetFormattedText());
    }
    public override int MaxUpgradeLevel => 0;
    // 同调本身包含消耗，卡面不再重复列出“消耗”；仍走原生出牌去向与节俭钩子。
    protected override PileType GetResultPileTypeForCardPlay()
    {
        var pile = base.GetResultPileTypeForCardPlay();
        return pile == PileType.Discard ? PileType.Exhaust : pile;
    }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) => SynchroCmd.Play(context, this);
}

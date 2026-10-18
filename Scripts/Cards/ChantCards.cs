using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace FrierenMod.Scripts.Cards;

public sealed class PreciseChant() : FrierenCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self), IChantCard
{
    protected override bool IsPlayable => Owner != null && ChantCmd.Options(Owner, null).Count > 0;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await ChantCmd.Choose(context, play, null);
    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Exhaust);
}

public sealed class FinalChant() : FrierenCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self), IChantCard
{
    protected override bool IsPlayable => Owner != null && ChantCmd.Options(Owner, null).Count > 0;
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await ChantCmd.Final(context, play);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class ManaExpansion() : FrierenCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        // 先快照，避免满手牌溢出到弃牌堆后又被重复翻倍。
        var piles = new[] { PileType.Draw, PileType.Hand, PileType.Discard };
        var counts = piles.Select(p => ManaCmd.InPile(Owner, p).Count()).ToArray();
        for (int i = 0; i < piles.Length; i++) await ManaCmd.Generate(Owner, piles[i], counts[i]);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

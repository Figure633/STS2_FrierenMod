using FrierenMod.Core;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Cards;

public abstract class NormalChantCard(int cost, CardRarity rarity, int chant)
    : FrierenCard(cost, CardType.Skill, rarity, TargetType.Self), IChantCard
{
    protected virtual int Intensity => chant;
    protected override void AddExtraArgsToDescription(LocString description)
    {
        description.Add("Intensity", Intensity);
        description.Add("Upgraded", IsUpgraded);
    }
    protected override bool IsPlayable => Owner != null && ChantCmd.Options(Owner, Intensity).Count > 0;
}

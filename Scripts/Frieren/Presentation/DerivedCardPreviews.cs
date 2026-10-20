using FrierenMod.Scripts.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Regret = MegaCrit.Sts2.Core.Models.Cards.Regret;
using System.Text.RegularExpressions;

namespace FrierenMod.Scripts.Presentation;

public static class DerivedCardPreviews
{
    private static readonly Type[] Tokens = [typeof(Mana), typeof(HighDensityMana), typeof(SecondDefensiveMagic),
        typeof(BlueMoonWeed), typeof(LittleFire), typeof(Laziness), typeof(GreenApple),
        typeof(Ice), typeof(Fire), typeof(Thunder), typeof(Dark), typeof(Light), typeof(Regret)];

    public static IEnumerable<IHoverTip> For(CardModel source)
    {
        var readable = source.IsMutable ? source : source.ToMutable();
        string description = Regex.Replace(readable.GetDescriptionForPile(PileType.None), @"\[[^\]]*\]", "");
        var explicitTypes = source switch {
            DefensiveMagic => new[] { typeof(SecondDefensiveMagic) },
            FlowerFieldSpell => [typeof(BlueMoonWeed)],
            FireSpell => [typeof(LittleFire)],
            RedAppleSpell => [typeof(GreenApple)],
            FiveMinutesMore => [typeof(Laziness)],
            ElementsBombing => [typeof(Ice), typeof(Fire), typeof(Thunder), typeof(Dark), typeof(Light)],
            _ => Array.Empty<Type>()
        };
        foreach (var type in Tokens) {
            if (source.GetType() == type) continue;
            var canonical = ModelDb.GetById<CardModel>(ModelDb.GetId(type));
            if (!explicitTypes.Contains(type) && !description.Contains(canonical.Title, StringComparison.OrdinalIgnoreCase)) continue;
            var preview = canonical.ToMutable();
            if (readable.Owner is { } owner) preview.Owner = owner;
            bool upgraded = readable.IsUpgraded && explicitTypes.Contains(type) && source is not FiveMinutesMore;
            if (upgraded && preview.MaxUpgradeLevel > 0) { preview.UpgradeInternal(); preview.FinalizeUpgradeInternal(); }
            yield return new CardHoverTip(preview);
        }
    }
}

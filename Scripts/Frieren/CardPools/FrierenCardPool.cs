using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Relics;

namespace FrierenMod.Scripts.Characters;

public sealed class FrierenCardPool : CustomCardPoolModel
{
    public override string Title => "Frieren";
    public override Color DeckEntryCardColor => new("e5e0f0");
    public override bool IsColorless => false;
    public override string EnergyColorName => "ironclad";
}

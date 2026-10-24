using BaseLib.Abstracts;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.HoverTips;
using FrierenMod.Scripts.Presentation;
using MegaCrit.Sts2.Core.Helpers;

namespace FrierenMod.Scripts.Powers;

public abstract class FrierenPower : CustomPowerModel
{
    public override LocString Description { get { var text = base.Description; text.Add("energyPrefix", EnergyIconHelper.GetPrefix(this)); return text; } }
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => $"res://FrierenMod/images/powers/{GetType().Name}.png";
    public override string CustomBigIconPath => CustomPackedIconPath;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => FrierenHoverTips.ForText(Description.GetRawText());
}

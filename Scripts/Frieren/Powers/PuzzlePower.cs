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

namespace FrierenMod.Scripts.Powers;

public sealed class PuzzlePower : TurnOnlyPower
{
    // 复用一代各阶段的名称和碎片提示，实际累积及结算仍由 Amount 驱动。
    public override LocString Title => Amount is >= 1 and <= 3 ? new("powers", $"{Id.Entry}.{Amount}.title") : base.Title;
    public override LocString Description => Amount is >= 1 and <= 3 ? new("powers", $"{Id.Entry}.{Amount}.description") : base.Description;

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (Amount == 3 && participants.Contains(Owner))
            await CreatureCmd.Kill(CombatState.Enemies.Where(x => x.IsAlive).ToArray());
    }
}

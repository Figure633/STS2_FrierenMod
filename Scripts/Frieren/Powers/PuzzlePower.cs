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
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Helpers;

namespace FrierenMod.Scripts.Powers;

public sealed class PuzzlePower : TurnOnlyPower
{
    // 保留原模型 ID，作为本回合的隐藏顺序状态；三个线索使用各自的可见能力。
    public const int BrokenSequence = 4;
    protected override bool IsVisibleInternal => false;

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power, decimal amount, Creature? applier, CardModel? source)
    {
        if (power != this || Amount != 3) return;
        var enemies = CombatState.Enemies.Where(x => x.IsAlive).ToArray();
        if (enemies.Length == 0) return;
        // 复用原版 Hyperbeam 的蓄力、光束、音效与震屏，结束后再统一击杀。
        var beam = NHyperbeamVfx.Create(Owner, enemies[^1]);
        if (beam != null) {
            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(beam);
            await Cmd.Wait(NHyperbeamVfx.hyperbeamAnticipationDuration + NHyperbeamVfx.hyperbeamLaserDuration);
            foreach (var enemy in enemies.Where(x => x.IsAlive)) {
                var impact = NHyperbeamImpactVfx.Create(Owner, enemy);
                if (impact != null) NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(impact);
            }
        }
        await CreatureCmd.Kill(enemies.Where(x => x.IsAlive).ToArray());
    }
}

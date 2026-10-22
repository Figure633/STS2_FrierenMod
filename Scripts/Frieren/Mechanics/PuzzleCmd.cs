using FrierenMod.Core;
using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace FrierenMod.Scripts.Mechanics;

public static class PuzzleCmd
{
    public static async Task Chant(PlayerChoiceContext context, CardPlay play, ChantOption option)
    {
        var self = play.Card.Owner.Creature;
        int stage = option.SpellId == "Factor100" ? 1 : option.SpellId == "Factor010" ? 2 : 3;
        void Hint() => TalkCmd.Play(new LocString("frieren", "PUZZLE_WRONG"), self, VfxColor.White);
        if ((int)option.Slot != stage - 1) { Hint(); return; }

        int progress = self.GetPowerAmount<PuzzlePower>();
        bool correct = progress == stage - 1;
        // 即使错序也留下各自的文字碎片，让玩家从实际获得的先后判断；本回合不允许重试。
        async Task Clue<T>() where T : PowerModel => await PowerCmd.Apply<T>(context, self, 1, self, play.Card);
        if (stage == 1) await Clue<Factor100Power>();
        else if (stage == 2) await Clue<Factor010Power>();
        else await Clue<Factor001Power>();
        if (correct) await PowerCmd.Apply<PuzzlePower>(context, self, 1, self, play.Card);
        else {
            if (progress != PuzzlePower.BrokenSequence)
                await PowerCmd.Apply<PuzzlePower>(context, self, PuzzlePower.BrokenSequence - progress, self, play.Card);
            Hint();
        }
    }
}

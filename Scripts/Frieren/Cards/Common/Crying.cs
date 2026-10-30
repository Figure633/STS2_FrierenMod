using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Cards;

public sealed class Crying() : FrierenCard(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    public override bool GainsBlock => true;
    protected override IEnumerable<MegaCrit.Sts2.Core.HoverTips.IHoverTip> ExtraHoverTips =>
        base.ExtraHoverTips.Append(MegaCrit.Sts2.Core.HoverTips.HoverTipFactory.FromPower<WeakPower>());
    [SavedProperty] public int CurrentBlock { get => DynamicVars.Block.IntValue; set { AssertMutable(); DynamicVars.Block.BaseValue = value; } }
    [SavedProperty] public int CurrentWeak { get => DynamicVars["Weak"].IntValue; set { AssertMutable(); DynamicVars["Weak"].BaseValue = value; } }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(6, ValueProp.Move), new DynamicVar("Weak", 1)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        decimal weak = DynamicVars["Weak"].BaseValue;
        decimal block = await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        await PowerCmd.Apply<WeakPower>(context, play.Target!, weak, Owner.Creature, this);
        // 存储已经结算过敏捷、脆弱的显示值；下次打出再走正常格挡修正。
        CurrentBlock = (int)weak;
        CurrentWeak = (int)block;
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(2);
}

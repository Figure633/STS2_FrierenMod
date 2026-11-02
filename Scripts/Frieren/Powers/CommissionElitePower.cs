using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Powers;

public sealed class CommissionElitePower : FrierenPower
{
    private int _affix;
    [SavedProperty] public int Affix {get=>_affix;set{AssertMutable();_affix=value;}}
    public override string CustomPackedIconPath=>"res://FrierenMod/images/powers/EnemyManaPower.png";
    public override string CustomBigIconPath=>"res://FrierenMod/images/powers/big/EnemyManaPower.png";
    public override LocString Description {get{var text=base.Description;text.Add("Effect",Mechanics.CommissionCmd.AffixText(Affix,(int)Amount-1,Owner?.CombatState?.Players.Count??1));return text;}}
    public override async Task AfterSideTurnEnd(PlayerChoiceContext context,CombatSide side,IEnumerable<Creature> participants){
        if(!participants.Contains(Owner)||Owner.IsDead)return;
        if(Affix==1){Flash();await CreatureCmd.GainBlock(Owner,8+Amount*2,ValueProp.Unpowered,null);}
        if(Affix==2){Flash();await CreatureCmd.Heal(Owner,4+Amount*2);}
    }
    public async Task Empower(PlayerChoiceContext context){
        // 与一代带火精英类似的四种强化；复用原生生命、力量、人工制品及格挡命令。
        int extra=(int)Math.Ceiling(Owner.MaxHp*(Affix == 3 ? .5 : .25));
        await CreatureCmd.GainMaxHp(Owner,extra);
        if(Affix==0)await PowerCmd.Apply<StrengthPower>(context,Owner,2+Amount,Owner,null);
        if(Affix==1)await CreatureCmd.GainBlock(Owner,8+Amount*2,ValueProp.Unpowered,null);
        if(Affix==3)await PowerCmd.Apply<ArtifactPower>(context,Owner,2,Owner,null);
        Flash();
    }
}

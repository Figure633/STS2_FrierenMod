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
        if(Affix==1){Flash();await CreatureCmd.GainBlock(Owner,Amount*2+2,ValueProp.Unpowered,null);}
        if(Affix==2){Flash();await CreatureCmd.Heal(Owner,Amount*2+1);}
    }
    public async Task Empower(PlayerChoiceContext context){
        // 一代带火精英四选一；生命强化不与其他词缀叠加。
        if(Affix==0)await PowerCmd.Apply<StrengthPower>(context,Owner,Amount+1,Owner,null);
        if(Affix==3)await CreatureCmd.GainMaxHp(Owner,(int)Math.Ceiling(Owner.MaxHp*.25));
        Flash();
    }
}

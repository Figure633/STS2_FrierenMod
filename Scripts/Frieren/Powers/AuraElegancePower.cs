using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Powers;

public sealed class AuraElegancePower : FrierenPower
{
    private int _absorbed;
    public int StoredMana => Math.Max(0, Amount - 1);
    public override int DisplayAmount => StoredMana;
    public override LocString Description { get { var text = base.Description; text.Add("Mana", StoredMana); return text; } }
    public override decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner) return amount;
        _absorbed = Math.Min(StoredMana, Math.Max(0, (int)amount));
        return amount - _absorbed;
    }
    // 只在原生伤害结算确认调用时支付；伤害预览不扣魔力。每段攻击单独结算。
    public override async Task AfterModifyingHpLostAfterOsty()
    {
        int spent = _absorbed;
        _absorbed = 0;
        if (spent > 0) await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), this, -spent, Owner, null);
    }
}

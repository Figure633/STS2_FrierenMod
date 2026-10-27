using MegaCrit.Sts2.Core.Commands;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Powers;

public sealed class AuraElegancePower : FrierenPower
{
    private int _absorbed;
    public int LastAbsorbed { get; private set; }
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
        LastAbsorbed = spent;
        _absorbed = 0;
        if (spent > 0) await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), this, -spent, Owner, null);
    }
    public override Task BeforeDamageReceived(PlayerChoiceContext context, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner) LastAbsorbed = 0;
        return Task.CompletedTask;
    }
    public override async Task AfterDamageReceived(PlayerChoiceContext context, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || result.UnblockedDamage + result.OverkillDamage != 0 || LastAbsorbed <= 0 || TestMode.IsOn) return;
        if (NDamageNumVfx.Create(target, LastAbsorbed) is { } effect) target.GetVfxContainer()?.AddChild(effect);
        if (NHitSparkVfx.Create(target) is { } spark) target.GetVfxContainer()?.AddChild(spark);
        SfxCmd.PlayDamage(target.Monster, LastAbsorbed);
        NGame.Instance?.ScreenShake(LastAbsorbed < 6 ? ShakeStrength.Weak : LastAbsorbed < 11 ? ShakeStrength.Medium : ShakeStrength.Strong, ShakeDuration.Short);
        if (!props.HasFlag(ValueProp.SkipHurtAnim)) await CreatureCmd.TriggerAnim(target, "Hit", 0f);
    }

    // 只调整飘字，保留原生生命伤害结果，避免影响吸血与伤害统计。
    [HarmonyPatch(typeof(NDamageNumVfx), nameof(NDamageNumVfx.Create), typeof(Creature), typeof(DamageResult))]
    private static class AbsorbedDamageNumber
    {
        private static bool Prefix(Creature target, DamageResult result, ref NDamageNumVfx? __result)
        {
            int absorbed = target.GetPower<AuraElegancePower>()?.LastAbsorbed ?? 0;
            if (absorbed <= 0) return true;
            __result = NDamageNumVfx.Create(target, result.UnblockedDamage + result.OverkillDamage + absorbed);
            return false;
        }
    }
}

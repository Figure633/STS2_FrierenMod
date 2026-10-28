using FrierenMod.Scripts.Shared.Compatibility;
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
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Powers;

public sealed class PajamasFormPower : FrierenPower
{
    // 旧模型Amount仍代表能力张数；新增升级加成默认0，保留已有战斗状态语义。
    [MegaCrit.Sts2.Core.Saves.Runs.SavedProperty]
    public int UpgradeBonus { get; set; }
    public int DamagePerMana => 4*Amount+UpgradeBonus;
    public void AddUpgradeBonus(){UpgradeBonus++;InvokeDisplayAmountChanged();}
    public override MegaCrit.Sts2.Core.Localization.LocString Description { get {var text=base.Description;text.Add("DamagePerMana",DamagePerMana);return text;} }
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner)) return;
        var cards = new[] { PileType.Draw, PileType.Hand, PileType.Discard }
            .SelectMany(p => ManaCmd.InPile(Owner.Player!, p)).ToArray();
        if (cards.Length >= 30) Presentation.FrierenAchievements.Unlock(Owner.Player!, "4009");
        foreach (var card in cards) await GameApi.Exhaust(context, card);
        if (cards.Length > 0)
            await CreatureCmd.Damage(context, Owner.CombatState!.Enemies.Where(c => c.IsAlive), DamagePerMana * cards.Length, ValueProp.Unpowered, Owner);
    }
}

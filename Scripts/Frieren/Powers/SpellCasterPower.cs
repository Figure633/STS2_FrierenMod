using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Monsters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Powers;

public sealed class SpellCasterPower : FrierenPower
{
    public static readonly int[] Costs = [10, 20, 20, 30, 20, 30, 20, 50];
    public int SpellsCast { get; private set; }
    public override int DisplayAmount => SpellsCast;
    public int CostForSpell(int index)
    {
        int hp = IsMutable && Owner?.Monster is SpiegelFrieren ? Owner.MaxHp : 500;
        int before = index / 8 * Costs.Sum() + Costs.Take(index % 8).Sum();
        // 对累计阈值取整，避免逐项取整让两轮施法的血量进度发生漂移。
        return (int)Math.Ceiling((before + Costs[index % 8]) * (decimal)hp / 500) - (int)Math.Ceiling(before * (decimal)hp / 500);
    }
    private bool Enhanced => Owner?.Monster is SpiegelFrieren { Enhanced: true };
    public override LocString Description
    {
        get {
            var text = base.Description;
            text.Add("Spells", string.Join("\n", Enumerable.Range(0, 8).Select(i => {
                var line = new LocString("powers", Id.Entry + ".spells." + i + (i == 5 && Enhanced ? ".enhanced" : ""));
                line.Add("Cost", CostForSpell(Math.Min(SpellsCast / 8, 1) * 8 + i)); line.Add("Block", Enhanced ? 15 : 10); line.Add("Ritual", Enhanced ? 5 : 3);
                line.Add("Burns", Enhanced ? 3 : 2); line.Add("Buffer", Enhanced ? 2 : 1);
                return (SpellsCast % 8 > i ? "[green]" : "[gold]") + line.GetFormattedText() + (SpellsCast % 8 > i ? "[/green]" : "[/gold]");
            })));
            return text;
        }
    }
    public async Task Resolve(PlayerChoiceContext context)
    {
        while (SpellsCast < 16 && Owner.GetPower<EnemyManaPower>() is { } mana)
        {
            int spell = SpellsCast % 8;
            int cost = CostForSpell(SpellsCast);
            if(mana.StoredMana<cost)break;
            await PowerCmd.ModifyAmount(context, mana, -cost, Owner, null);
            SpellsCast++; Flash(); InvokeDisplayAmountChanged();
            switch (spell)
            {
                case 0:
                    await PowerCmd.Apply<BarricadePower>(context, Owner, 1, Owner, null);
                    await CreatureCmd.GainBlock(Owner, Enhanced ? 15 : 10, ValueProp.Unpowered, null); break;
                case 1: await PowerCmd.Apply<RitualPower>(context, Owner, Enhanced ? 5 : 3, Owner, null); break;
                case 2: await PowerCmd.Apply<ArtifactPower>(context, Owner, 1, Owner, null); break;
                case 3:
                    foreach (var player in Owner.CombatState!.Players)
                        for (int i = 0; i < (Enhanced ? 3 : 2); i++) await CardFlowCmd.Generate<Burn>(player, PileType.Draw);
                    break;
                case 4:
                    await PowerCmd.Apply<VulnerablePower>(context, Owner.CombatState!.PlayerCreatures, 2, Owner, null);
                    await PowerCmd.Apply<WeakPower>(context, Owner.CombatState.PlayerCreatures, 2, Owner, null); break;
                case 5:
                    await PowerCmd.Apply<BufferPower>(context, Owner, Enhanced ? 2 : 1, Owner, null);
                    break;
                case 6:
                    // 召唤使用二代现有邪教徒，模型、动画、仪式和AI均由原版管理。
                    await CreatureCmd.Add<DampCultist>(Owner.CombatState!, SpellsCast < 8 ? "Bird1" : "Bird2"); break;
                case 7:
                    await PowerCmd.Apply<IntangiblePower>(context, Owner, 1, Owner, null);
                    break;
            }
        }
        if (SpellsCast >= 16) await PowerCmd.Remove(this);
    }
}

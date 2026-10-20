using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Mechanics;

public static class FactorEffects
{
    public static async Task Apply(PlayerChoiceContext context, CardPlay play, ChantOption option)
    {
        var player = play.Card.Owner;
        var self = player.Creature;
        var enemies = self.CombatState!.Enemies.Where(x => x.IsAlive).ToArray();
        int reward = option.Reward;
        async Task Buff<T>(int amount) where T : PowerModel => await PowerCmd.Apply<T>(context, self, amount, self, play.Card);
        async Task Debuff<T>(int amount) where T : PowerModel => await PowerCmd.Apply<T>(context, enemies, amount, self, play.Card);
        switch (option.SpellId)
        {
            case "AdvancedDefensiveMagic":
                for (int i = 0; i < 2; i++) await CreatureCmd.GainBlock(self, reward, ValueProp.Move, play);
                break;
            case "BarrierMagic":
                await CreatureCmd.GainBlock(self, reward, ValueProp.Move, play);
                await Buff<BlurPower>(reward); break;
            case "BuildUpBody": await Buff<StrengthPower>(reward); break;
            case "HighSpeedMovement": await Buff<DexterityPower>(reward); break;
            case "BePrepared": await Buff<VigorPower>(reward); break;
            case "IceArrows": await CreatureCmd.Damage(context, enemies, reward, ValueProp.Unblockable | ValueProp.Unpowered, self, play.Card); break;
            case "TransferToAlchemist": await Debuff<PoisonPower>(reward); break;
            case "MeleeMage": await Buff<StrikeUpPower>(reward); break;
            case "MetalPetalSpell": await Buff<MetallicizePower>(reward); break;
            case "Sleeeep": await Buff<DrawCardsNextTurnPower>(reward); break;
            case "GrossedOutLook":
                foreach (var enemy in enemies)
                {
                    int before = enemy.GetPower<StrengthPower>()?.Amount ?? 0;
                    await PowerCmd.Apply<StrengthPower>(context, enemy, -reward, self, play.Card);
                    int lost = before - (enemy.GetPower<StrengthPower>()?.Amount ?? 0);
                    if (lost > 0) await PowerCmd.Apply<RestoreStrengthPower>(context, enemy, lost, self, play.Card);
                }
                break;
            case "Hypnosis":
                await Debuff<WeakPower>(reward); await Debuff<VulnerablePower>(reward); break;
            case "NatureBeauty": await Buff<EnergyNextTurnPower>(reward); break;
            case "BloodSpell": await CreatureCmd.Damage(context, self, reward, ValueProp.Unblockable | ValueProp.Unpowered, self, play.Card); break;
            case "PassOnTheTorch": await ManaCmd.Generate(player, ManaCmd.Pile((ManaSlot)(((int)option.Slot + 1) % 3)), reward); break;
            case "PinchAndScrape": await Buff<SynchroWithoutManaPower>(reward); break;
            case "SpellBoost": await Buff<PhantasmalPower>(reward); break;
            case "GreatMageComposure": await Buff<ChantWithoutManaTimesPower>(reward); break;
            case "ImpeccableIntangible": await Buff<IntangiblePower>(reward); break;
            case "PeacefulDays": await CardPileCmd.Draw(context, reward, player); break;
            case "Strategizing": await PlayerCmd.GainEnergy(reward, player); break;
            case "Transcendence": await Buff<MantraPower>(reward); break;
            case "BigFire":
                for (int i = 0; i < reward; i++)
                    await CardFlowCmd.AddGenerated(self.CombatState.CreateCard<LittleFire>(player), ManaCmd.Pile(option.Slot), player);
                break;
            case "TheLivingLegend":
                for (int i = 0; i < reward; i++)
                    await CardFlowCmd.AddGenerated(LegendaryCards.CreateRandom(player), ManaCmd.Pile(option.Slot), player);
                break;
            case "Factor100": case "Factor010": case "Factor001":
                int stage = option.SpellId == "Factor100" ? 1 : option.SpellId == "Factor010" ? 2 : 3;
                if ((int)option.Slot != stage - 1) break;
                var puzzle = self.GetPower<PuzzlePower>();
                if ((puzzle?.Amount ?? 0) != stage - 1) await PowerCmd.Remove(puzzle);
                else await Buff<PuzzlePower>(1);
                break;
            default: throw new InvalidOperationException($"因子未结算：{option.SpellId}");
        }
    }
}

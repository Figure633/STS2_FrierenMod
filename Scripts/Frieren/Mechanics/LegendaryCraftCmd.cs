using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Localization;

namespace FrierenMod.Scripts.Mechanics;

public static class LegendaryCraftCmd
{
    public static async Task Create(PlayerChoiceContext context, CardModel source)
    {
        async Task<LegendaryEffect> Choose(IEnumerable<LegendaryEffect> effects)
        {
            var choices = effects.Select(effect => {
                var choice = (LegendaryCraftChoice)ModelDb.Card<LegendaryCraftChoice>().ToMutable();
                choice.Owner = source.Owner;
                choice.Effect = effect;
                if (effect.Kind == "Cost") choice.EnergyCost.SetCustomBaseCost(effect.Amount);
                return choice;
            }).ToArray();
            // 原生三选一界面最多容纳三张；费用的四个选项使用单选网格。
            var selected = choices.Length > 3
                ? (await CardSelectCmd.FromSimpleGrid(context, choices, source.Owner,
                    new CardSelectorPrefs(new LocString("frieren", "CARD_SELECT"), 1))).Single()
                : (await CardSelectCmd.FromChooseACardScreen(context, choices, source.Owner))!;
            return ((LegendaryCraftChoice)selected).Effect;
        }
        int cost = (await Choose(Enumerable.Range(0, 4).Select(i => new LegendaryEffect("Cost", i)))).Amount;
        var selected = new List<LegendaryEffect>();
        for (int layer = 0; layer < 3; layer++)
        {
            var pool = LegendaryCraft.Options(cost, layer).ToList();
            var choices = new List<LegendaryEffect>();
            for (int i = 0; i < (source.IsUpgraded ? 3 : 2); i++)
            {
                int index = source.Owner.RunState.Rng.CombatCardGeneration.NextInt(pool.Count);
                choices.Add(pool[index]);
                pool.RemoveAt(index);
            }
            selected.Add(await Choose(choices));
        }
        var spell = source.Owner.Creature.CombatState!.CreateCard<CustomLegendarySpell>(source.Owner);
        spell.Configure(cost, selected);
        Presentation.FrierenAchievements.Unlock(source.Owner, "4014");
        await CardPileCmd.AddGeneratedCardToCombat(spell, PileType.Hand, source.Owner);
    }

    public static async Task Play(PlayerChoiceContext context, CardPlay play, CustomLegendarySpell card)
    {
        var owner = card.Owner;
        // 一代伤害修饰器优先于其余三层效果。
        foreach (var effect in card.Effects.OrderBy(e => e.Kind.StartsWith("Damage") ? -1 : 0))
        {
            int n = effect.Amount;
            switch (effect.Kind)
            {
                case "Chant": await ChantCmd.Choose(context, play, n); break;
                case "ManaDraw": await ManaCmd.Generate(owner, PileType.Draw, n); break;
                case "ManaHand": await ManaCmd.Generate(owner, PileType.Hand, n); break;
                case "ManaDiscard": await ManaCmd.Generate(owner, PileType.Discard, n); break;
                case "Scry": await BaseLib.Commands.ScryCmd.Execute(context, owner, n); break;
                case "Retain":
                    foreach (var selected in await CardFlowCmd.Choose(context, card, PileType.Hand, n)) selected.GiveSingleTurnRetain();
                    break;
                case "ZeroCost":
                    for (int i = 0; i < n; i++)
                    {
                        var hand = owner.PlayerCombatState!.Hand.Cards.Where(c => c != card && c.EnergyCost.GetWithModifiers(CostModifiers.All) > 0 && !c.EnergyCost.CostsX).ToArray();
                        if (hand.Length > 0) hand[owner.RunState.Rng.CombatCardGeneration.NextInt(hand.Length)].EnergyCost.SetThisTurn(0);
                    }
                    break;
                case "RandomCard":
                    var pool = ModelDb.CardPool<FrierenMod.Scripts.Characters.FrierenCardPool>().AllCards.Where(c => c.Rarity == CardRarity.Common && c.CanBeGeneratedInCombat).OrderBy(c => c.Id.Entry).ToArray();
                    for (int i = 0; i < n; i++)
                    {
                        var generated = owner.Creature.CombatState!.CreateCard(pool[owner.RunState.Rng.CombatCardGeneration.NextInt(pool.Length)], owner);
                        generated.EnergyCost.SetThisTurn(0);
                        await CardPileCmd.AddGeneratedCardToCombat(generated, PileType.Hand, owner);
                    }
                    break;
                case "Block": await CreatureCmd.GainBlock(owner.Creature, card.DynamicVars.Block, play); break;
                case "Damage": case "DamageTriple":
                    await DamageCmd.Attack(n).FromCard(card).Targeting(play.Target!).WithHitCount(effect.Kind == "DamageTriple" ? 3 : 1).Execute(context); break;
                case "DamageAll": await DamageCmd.Attack(n).FromCard(card).TargetingAllOpponents(owner.Creature.CombatState!).Execute(context); break;
                case "Energy": await PlayerCmd.GainEnergy(n, owner); break;
                case "Execute": if (play.Target!.CurrentHp <= n) await CreatureCmd.Kill(play.Target); break;
                case "Draw": await CardPileCmd.Draw(context, n, owner); break;
                case "Dexterity": await PowerCmd.Apply<DexterityPower>(context, owner.Creature, n, owner.Creature, card); break;
                case "Vulnerable": await PowerCmd.Apply<VulnerablePower>(context, owner.Creature.CombatState!.Enemies.Where(e => e.IsAlive), n, owner.Creature, card); break;
                case "Intangible": await PowerCmd.Apply<IntangiblePower>(context, owner.Creature, n, owner.Creature, card); break;
                case "DoubleStrength": await PowerCmd.Apply<StrengthPower>(context, owner.Creature, owner.Creature.GetPowerAmount<StrengthPower>(), owner.Creature, card); break;
                case "RemoveDebuffs":
                    foreach (var power in owner.Creature.Powers.Where(p => p.TypeForCurrentAmount == PowerType.Debuff).ToArray()) await PowerCmd.Remove(power);
                    break;
                case "UpgradeHand": case "UpgradeAll":
                    var cards = effect.Kind == "UpgradeHand" ? owner.PlayerCombatState!.Hand.Cards : owner.PlayerCombatState!.AllCards;
                    foreach (var upgrade in cards.Where(c => c.IsUpgradable && c.Pile?.Type != PileType.Exhaust).ToArray()) CardCmd.Upgrade(upgrade);
                    break;
                default: throw new InvalidOperationException($"Unknown legendary effect: {effect.Kind}");
            }
        }
    }
}

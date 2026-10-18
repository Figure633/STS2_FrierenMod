using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Cards;

public interface IChantCard;
public interface ILegendaryCard;

public static class LegendaryCards
{
    // 饰品魔法与自定义传奇魔法在一代被标记为不可随机生成。
    public static IReadOnlyList<Type> Pool { get; } = new[] { typeof(ContinualChant), typeof(Famehameha),
        typeof(FairyTaleEnd), typeof(HotTeaSpell), typeof(MoldSpell), typeof(OilSpell),
        typeof(RustCleanMagic), typeof(LightningMagic) };
    public static CardModel CreateRandom(Player player)
    {
        var type = Pool[player.RunState.Rng.CombatCardGeneration.NextInt(Pool.Count)];
        return player.Creature.CombatState!.CreateCard(ModelDb.GetById<CardModel>(ModelDb.GetId(type)), player);
    }
    public static int ChantsThisTurn(Player player) => CombatManager.Instance.History.CardPlaysStarted
        .Count(x => x.Actor == player.Creature && x.HappenedThisTurn(player.Creature.CombatState) && x.CardPlay.Card is IChantCard);
}

public abstract class LegendaryCard(int cost, CardType type, CardRarity rarity, TargetType target, bool showInLibrary = true)
    : FrierenCard(cost, type, rarity, target, showInLibrary), ILegendaryCard
{
    protected override bool IsPlayable => Owner != null && LegendaryCards.ChantsThisTurn(Owner) > 0;
}

public sealed class Famehameha() : LegendaryCard(4, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy), IChantListener
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(31, ValueProp.Move)];
    public Task AfterChant(PlayerChoiceContext context, CardPlay play) { EnergyCost.AddThisTurn(-1); return Task.CompletedTask; }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target!).Execute(context);
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(10);
}

public sealed class FairyTaleEnd() : LegendaryCard(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new FairyTaleDamageVar()];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await DamageCmd.Attack(((FairyTaleDamageVar)DynamicVars.Damage).Calculate(this)).FromCard(this).Targeting(play.Target!).Execute(context);
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2);

    // 预览与出牌共用牌组倍率；基础值不在预览时改写，避免反复查看造成重复翻倍。
    private sealed class FairyTaleDamageVar() : DamageVar(6, ValueProp.Move)
    {
        public decimal Calculate(CardModel card)
        {
            int count = card.IsMutable ? card.Owner?.Deck.Cards.OfType<ILegendaryCard>()
                .Select(x => x.GetType()).Distinct().Count() ?? 0 : 0;
            return Math.Min(99999m, BaseValue * (decimal)Math.Pow(2, Math.Min(count, 16)));
        }

        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode,
            MegaCrit.Sts2.Core.Entities.Creatures.Creature? target, bool runGlobalHooks)
        {
            // 使用原生伤害预览处理力量、虚弱、易伤和附魔，保持目标预览与实际命中一致。
            var damage = new DamageVar(Calculate(card), Props);
            damage.UpdateCardPreview(card, previewMode, target, runGlobalHooks);
            EnchantedValue = damage.EnchantedValue;
            PreviewValue = damage.PreviewValue;
        }
    }
}

public sealed class HotTeaSpell() : LegendaryCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(12, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue + ManaCmd.Total(Owner), ValueProp.Move, play);
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3);
}

public sealed class MoldSpell() : LegendaryCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(4, ValueProp.Move), new DynamicVar("Hits", 5)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target!).WithHitCount(DynamicVars["Hits"].IntValue).Execute(context);
    protected override void OnUpgrade() => DynamicVars["Hits"].UpgradeValueBy(1);
}

public sealed class OilSpell() : LegendaryCard(2, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(12, ValueProp.Move), new DynamicVar("SecondDamage", 8)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var targets = Owner.Creature.CombatState!.Enemies.Where(x => x.IsAlive).ToArray();
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).TargetingAllOpponents(CombatState!).Execute(context);
        if (targets.All(x => x.IsAlive))
            await DamageCmd.Attack(DynamicVars["SecondDamage"].BaseValue).FromCard(this).TargetingAllOpponents(CombatState!).Execute(context);
    }
    protected override void OnUpgrade() => DynamicVars["SecondDamage"].UpgradeValueBy(4);
}

// 一代标记 AutoAdd.Ignore；保留测试模型，但不加入正常奖励或生成池。
public sealed class PaperPlaneSpell() : LegendaryCard(2, CardType.Power, CardRarity.Token, TargetType.Self, false)
{
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await PowerCmd.Apply<PaperPlanePower>(context, Owner.Creature, 1, Owner.Creature, this);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class RustCleanMagic() : LegendaryCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (Owner.Creature.CombatState!.Encounter?.RoomType != MegaCrit.Sts2.Core.Rooms.RoomType.Boss)
            await PowerCmd.Remove(play.Target!.Powers.FirstOrDefault(x => x.Type == PowerType.Buff));
        await PowerCmd.Remove(Owner.Creature.Powers.FirstOrDefault(x => x.Type == PowerType.Debuff));
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class LightningMagic() : LegendaryCard(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        foreach (var pile in new[] { PileType.Draw, PileType.Discard })
            foreach (var mana in ManaCmd.InPile(Owner, pile).ToArray()) await CardPileCmd.Add(mana, PileType.Hand);
        await PowerCmd.Apply<ThunderPower>(context, Owner.Creature, 1, Owner.Creature, this);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class LittleFire() : FrierenCard(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target!).Execute(context);
        foreach (var fire in Owner.PlayerCombatState!.AllCards.OfType<LittleFire>().Where(x => x.Pile?.Type != PileType.Exhaust))
            fire.DynamicVars.Damage.BaseValue += 2;
        EnergyCost.AddThisTurn(1);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2);
}

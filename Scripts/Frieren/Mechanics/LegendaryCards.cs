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

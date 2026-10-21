using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Logging;

namespace FrierenMod.Scripts.Mechanics;

public static class ChantCmd
{
    public static bool IsChant(CardModel card) => card is IChantCard || card is CustomLegendarySpell spell && spell.Effects.Any(e => e.Kind == "Chant");

    public static IReadOnlyList<ChantOption> Options(Player player, int? intensity)
        => Choices(player, intensity).Where(choice => choice.CanSelect).Select(choice => choice.Option).ToArray();

    public static IReadOnlyList<(ChantOption Option, bool CanSelect)> Choices(Player player, int? intensity)
    {
        if (player.Creature.CombatState == null) return [];
        var deck = MagicDeckStore.Read(player);
        var result = new List<(ChantOption Option, bool CanSelect)>();
        foreach (var slot in Enum.GetValues<ManaSlot>())
        {
            var spell = deck.ResolveSlot(slot);
            if (spell == null) continue;
            int mana = ManaCmd.InPile(player, ManaCmd.Pile(slot)).Count();
            var option = ChantRules.Preview(slot, spell, intensity ?? ChantRules.PreciseIntensity(mana, spell),
                player.Creature.GetPower<ConcentrationPower>()?.Amount ?? 0,
                player.Creature.GetPower<WeakenedChantPower>()?.Amount ?? 0,
                player.Creature.GetPower<ChantWithoutManaPower>() != null || player.Creature.GetPower<ChantWithoutManaTimesPower>() != null);
            result.Add((option, option.Intensity > 0 && mana >= option.Cost));
        }
        return result;
    }

    public static async Task<ChantOption?> Choose(PlayerChoiceContext context, CardPlay play, int? intensity)
    {
        var player = play.Card.Owner;
        await MagicDeckCmd.ChooseStartingFactors(context, player);
        var options = Choices(player, intensity);
        if (!options.Any(option => option.CanSelect)) { Log.Info("Frieren: 没有可用咏唱槽位；检查装载与各牌堆魔力。"); return null; }
        var free = player.Creature.GetPower<ChantWithoutManaPower>() == null ? player.Creature.GetPower<ChantWithoutManaTimesPower>() : null;
        var cards = options.Select(choice => FactorCard.Create(player, choice.Option.SpellId,
            choice.Option.Slot, choice.Option, choice.CanSelect)).ToArray();
        // 原生选择命令同步选项索引；不使用全局当前玩家或当前卡牌。
        var selected = await CardSelectCmd.FromChooseACardScreen(context, cards, player);
        int index = Array.IndexOf(cards, selected);
        if (index < 0 || !options[index].CanSelect) return null;
        await Resolve(context, play, options[index].Option);
        if (free != null) await PowerCmd.Decrement(free);
        return options[index].Option;
    }

    public static async Task Final(PlayerChoiceContext context, CardPlay play)
    {
        // 一代 FINAL 在结算前生成三个槽的快照。
        await MagicDeckCmd.ChooseStartingFactors(context, play.Card.Owner);
        var self = play.Card.Owner.Creature;
        var free = self.GetPower<ChantWithoutManaPower>() == null ? self.GetPower<ChantWithoutManaTimesPower>() : null;
        var options = Options(play.Card.Owner, null);
        bool fastAnimation = options.Sum(option => option.Cost) > 5;
        foreach (var option in options) await Resolve(context, play, option, fastAnimation);
        if (free != null && options.Count > 0) await PowerCmd.Decrement(free);
    }

    public static async Task Resolve(PlayerChoiceContext context, CardPlay play, ChantOption option, bool fastAnimation = false)
    {
        var player = play.Card.Owner;
        if (!await ManaCmd.Spend(context, player, option.Slot, option.Cost, fastAnimation || option.Cost > 5)) return;
        await FactorEffects.Apply(context, play, option);
        var listeners = player.Creature.Powers.Cast<object>().Concat(player.PlayerCombatState!.AllPiles
            .Where(p => p.Type is PileType.Draw or PileType.Hand or PileType.Discard).SelectMany(p => p.Cards)).OfType<IChantListener>().ToArray();
        foreach (var listener in listeners) await listener.AfterChant(context, play);
        Log.Info($"Frieren: {option.Slot} {option.SpellId} X={option.Intensity} 支付={option.Cost}");
    }
}

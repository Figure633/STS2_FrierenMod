using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace FrierenMod.Scripts.Mechanics;

// 仅记录施法者；一代同样保留卡牌对象，回溯位置而不撤销期间升级。
// 不回滚 RNG、敌人或共享战斗历史，避免二代命令和联机日志失配。
public sealed class PlayerTimeSnapshot
{
    private int _hp, _maxHp, _block, _potionSlots;
    private decimal _energy;
    private Dictionary<PileType, CardModel[]> _piles = [];
    private PowerModel[] _powers = [];
    private RelicModel[] _relics = [];
    private List<SerializablePotion> _potions = [];

    public static PlayerTimeSnapshot Capture(Player player, CardModel exclude) => new()
    {
        _hp = player.Creature.CurrentHp,
        _maxHp = player.Creature.MaxHp,
        _block = player.Creature.Block,
        _energy = player.PlayerCombatState!.Energy,
        _potionSlots = player.MaxPotionCount,
        _piles = new[] { PileType.Draw, PileType.Hand, PileType.Discard, PileType.Exhaust }
            .ToDictionary(p => p, p => CardPile.Get(p, player)!.Cards.Where(c => c != exclude).ToArray()),
        _powers = player.Creature.Powers.Where(p => p is not TimeTravelPower)
            .Select(p => (PowerModel)p.ClonePreservingMutability()).ToArray(),
        _relics = player.Relics.Select(r => (RelicModel)r.ClonePreservingMutability()).ToArray(),
        _potions = player.Potions.Select(p => p.ToSerializable(player.GetPotionSlotIndex(p))).ToList()
    };

    public async Task Restore(PlayerChoiceContext context, Player player, bool upgraded)
    {
        // 清除后重新放入原对象，不触发抽牌、消耗、获取遗物等额外收益。
        foreach (var pile in player.PlayerCombatState!.AllPiles)
            foreach (var card in pile.Cards.ToArray()) pile.RemoveInternal(card);
        foreach (var (pile, cards) in _piles)
            foreach (var card in cards) await CardPileCmd.Add(card, pile, CardPilePosition.Bottom);
        foreach (var power in player.Creature.Powers.ToArray()) await PowerCmd.Remove(power);
        foreach (var power in _powers)
        {
            power.ApplyInternal(player.Creature, power.Amount);
        }
        foreach (var relic in player.Relics.ToArray()) player.RemoveRelicInternal(relic);
        foreach (var relic in _relics) player.AddRelicInternal(relic);
        foreach (var potion in player.Potions.ToArray()) player.DiscardPotionInternal(potion);
        if (player.MaxPotionCount < _potionSlots) player.AddToMaxPotionCount(_potionSlots - player.MaxPotionCount);
        else if (player.MaxPotionCount > _potionSlots) player.SubtractFromMaxPotionCount(player.MaxPotionCount - _potionSlots);
        foreach (var saved in _potions) player.AddPotionInternal(PotionModel.FromSerializable(saved), saved.SlotIndex);
        player.Creature.SetMaxHpInternal(_maxHp);
        player.Creature.SetCurrentHpInternal(_hp);
        player.Creature.LoseBlockInternal(player.Creature.Block);
        player.Creature.GainBlockInternal(_block);
        await PlayerCmd.SetEnergy(_energy, player);
        if (!upgraded) await CardFlowCmd.Generate<MegaCrit.Sts2.Core.Models.Cards.Regret>(player, PileType.Hand);
    }
}

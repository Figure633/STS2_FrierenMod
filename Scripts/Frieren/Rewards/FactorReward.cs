using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using BaseLib.Utils;
using System.Text.Json;
using FrierenMod.Core;
using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace FrierenMod.Scripts.Rewards;

public sealed class FactorReward(Player player) : CustomReward(player)
{
    [CustomEnum] public static RewardType FrierenFactor;
    public static readonly SavedSpireField<FactorReward, string> SavedChoices = new(() => "", "FrierenMagicItemChoices");
    public int Seed { get; init; }
    public string[] Choices => string.IsNullOrEmpty(SavedChoices.Get(this)) ? [] : JsonSerializer.Deserialize<string[]>(SavedChoices.Get(this)!)!;
    protected override RewardType RewardType => FrierenFactor;
    protected override string IconPath => "res://FrierenMod/images/ui/magic_deck.png";
    public override LocString Description => new("frieren", "FACTOR_REWARD");
    public override bool IsPopulated => Choices.Length == 3;
    public override CreateRewardFromSave<CustomReward> DeserializeMethod => Restore;
    public override void Populate()
    {
        if (IsPopulated) return;
        var deck = MagicDeckStore.Read(Player);
        var rng = new Rng((uint)Seed);
        var offer = MagicRewardRules.Offer(deck.RewardRarityOffset, rng.NextInt);
        SavedChoices.Set(this, JsonSerializer.Serialize(offer.Choices));
        deck.RewardRarityOffset = offer.RarityOffset;
        MagicDeckStore.Write(Player, deck);
    }

    // 0.6.4 及以前只保存种子：按旧算法恢复已出现的因子选项，不能重掷成魔具。
    private void PopulateLegacy()
    {
        var rng = new Rng((uint)Seed);
        var available = SpellCatalog.All.ToList();
        var result = new List<string>();
        for (int i = 0; i < 3; i++)
        {
            int roll = rng.NextInt(50);
            var rarity = roll < 3 ? FactorRarity.Rare : roll < 20 ? FactorRarity.Uncommon : FactorRarity.Common;
            var pool = available.Where(x => x.Rarity == rarity).ToArray();
            var chosen = pool[rng.NextInt(pool.Length)];
            result.Add(chosen.Id); available.Remove(chosen);
        }
        SavedChoices.Set(this, JsonSerializer.Serialize(result));
    }
    public override void MarkContentAsSeen() { }
    protected override Task<bool> OnSelect() => MagicDeckCmd.ChooseReward(new BlockingPlayerChoiceContext(), Player, Choices);
    // BaseLib SavedSpireField 保存三项确切 ID；整数种子保留给旧版奖励的读档兼容。
    public override SerializableReward ToSerializable()
    {
        var save = base.ToSerializable(); // BaseLib 在原生入口附加 SavedSpireField。
        save.GoldAmount = Seed;
        return save;
    }
    public static FactorReward Restore(SerializableReward save, Player player)
    {
        var reward = new FactorReward(player) { Seed = save.GoldAmount };
        reward.PopulateLegacy();
        return reward;
    }
}

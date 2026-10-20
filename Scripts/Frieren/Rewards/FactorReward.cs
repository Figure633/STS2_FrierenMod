using BaseLib.Abstracts;
using BaseLib.Patches.Content;
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
    public int Seed { get; init; }
    public string[] Choices { get; private set; } = [];
    protected override RewardType RewardType => FrierenFactor;
    protected override string IconPath => "res://FrierenMod/images/ui/magic_deck.png";
    public override LocString Description => new("frieren", "FACTOR_REWARD");
    public override bool IsPopulated => Choices.Length == 3;
    public override CreateRewardFromSave<CustomReward> DeserializeMethod => Restore;
    public override void Populate()
    {
        if (IsPopulated) return;
        var rng = new Rng((uint)Seed);
        var available = SpellCatalog.All.ToList();
        var result = new List<string>();
        for (int i = 0; i < 3; i++)
        {
            // 当前是独立因子奖励；魔法道具拥有另一套使用流程，不混入因子选项。
            int roll = rng.NextInt(50);
            var rarity = roll < 3 ? FactorRarity.Rare : roll < 20 ? FactorRarity.Uncommon : FactorRarity.Common;
            var pool = available.Where(x => x.Rarity == rarity).ToArray();
            var chosen = pool[rng.NextInt(pool.Length)];
            result.Add(chosen.Id); available.Remove(chosen);
        }
        Choices = [.. result];
    }
    public override void MarkContentAsSeen() { }
    protected override Task<bool> OnSelect() => MagicDeckCmd.ChooseReward(new BlockingPlayerChoiceContext(), Player, Choices);
    // 自定义奖励沿用原生整数载荷保存种子；退出与继续不会重掷选项。
    public override SerializableReward ToSerializable() => new() { RewardType = FrierenFactor, GoldAmount = Seed };
    public static FactorReward Restore(SerializableReward save, Player player)
    {
        var reward = new FactorReward(player) { Seed = save.GoldAmount };
        reward.Populate();
        return reward;
    }
}

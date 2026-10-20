using BaseLib.Abstracts;
using BaseLib.Utils;
using FrierenMod.Core;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Rewards;
using FrierenMod.Scripts.Rewards;

namespace FrierenMod.Scripts.Relics;

[Pool(typeof(FrierenRelicPool))]
public sealed class HolyEmblem : CustomRelicModel
{
    private string _magicDeckJson = new MagicDeckState().ToJson();
    public override RelicRarity Rarity => RelicRarity.Starter;
    public override string PackedIconPath => "res://FrierenMod/images/relics/HolyEmblem.tres";
    protected override string PackedIconOutlinePath => "res://FrierenMod/images/relics/HolyEmblemOutline.tres";
    protected override string BigIconPath => PackedIconPath;

    // 原生 string SavedProperty 同时覆盖 JSON 与网络包，不保存战斗对象引用。
    [SavedProperty]
    public string MagicDeckJson
    {
        get => _magicDeckJson;
        set
        {
            AssertMutable();
            _ = MagicDeckState.FromJson(value);
            _magicDeckJson = value;
        }
    }

    public MagicDeckState ReadMagicDeck() => MagicDeckState.FromJson(_magicDeckJson);
    public void SaveMagicDeck(MagicDeckState deck) => MagicDeckJson = deck.ToJson();

    public override async Task AfterActEntered()
    {
        // EnterRoom 的钩子仍处在转场黑幕内；章节钩子在淡入完成后才运行。
        await MagicDeckCmd.ChooseStartingFactors(new BlockingPlayerChoiceContext(), Owner);
    }

    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner || room is not CombatRoom || room.RoomType == RoomType.Boss || rewards.OfType<FactorReward>().Any()) return false;
        var deck = MagicDeckStore.Read(player);
        bool drop = player.PlayerRng.Rewards.NextInt(100) < (room.RoomType == RoomType.Elite ? 70 : 40) + deck.RewardPity;
        deck.RewardPity += drop ? -10 : 10;
        MagicDeckStore.Write(player, deck);
        if (!drop) return false;
        var reward = new FactorReward(player) { Seed = player.PlayerRng.Rewards.NextInt() };
        reward.Populate(); rewards.Add(reward);
        return true;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player != Owner) return;
        await MagicDeckCmd.ChooseStartingFactors(context, Owner);
        Flash();
        await ManaCmd.Generate(Owner, PileType.Draw, 1);
        await ManaCmd.Generate(Owner, PileType.Discard, 1);
    }
}

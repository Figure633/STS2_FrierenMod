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
        // 外层新跑局/读档仍可能处于加载中；届时由界面在加载完成后继续。
        if (MagicDeckCmd.SetupMustWait) return;
        await MagicDeckCmd.ChooseStartingFactors(new BlockingPlayerChoiceContext(), Owner);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player != Owner) return;
        if (!MagicDeckCmd.SetupMustWait) await MagicDeckCmd.ChooseStartingFactors(context, Owner);
        Flash();
        await ManaCmd.Generate(Owner, PileType.Draw, 1, previewTime: 0.35f);
        await ManaCmd.Generate(Owner, PileType.Discard, 1, previewTime: 0.35f);
    }
}

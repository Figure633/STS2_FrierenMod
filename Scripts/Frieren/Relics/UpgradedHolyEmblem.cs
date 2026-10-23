using BaseLib.Abstracts;
using BaseLib.Utils;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace FrierenMod.Scripts.Relics;

[Pool(typeof(FrierenRelicPool))]
public sealed class UpgradedHolyEmblem : CustomRelicModel
{
    // 与原版升级初始遗物一致，不进入普通随机遗物奖励池。
    public override RelicRarity Rarity => RelicRarity.Starter;
    public override string PackedIconPath => "res://FrierenMod/images/relics/HolyEmblem.tres";
    protected override string PackedIconOutlinePath => "res://FrierenMod/images/relics/HolyEmblemOutline.tres";
    protected override string BigIconPath => PackedIconPath;

    public override async Task AfterActEntered()
    {
        if (!MagicDeckCmd.SetupMustWait)
            await MagicDeckCmd.ChooseStartingFactors(new BlockingPlayerChoiceContext(), Owner);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player != Owner) return;
        if (!MagicDeckCmd.SetupMustWait) await MagicDeckCmd.ChooseStartingFactors(context, Owner);
        Flash();
        await ManaCmd.Generate(Owner, PileType.Draw, 1, previewTime: 0.35f);
        await ManaCmd.Generate(Owner, PileType.Discard, 1, previewTime: 0.35f);
        await ManaCmd.Generate(Owner, PileType.Hand, 1, previewTime: 0.35f);
    }
}

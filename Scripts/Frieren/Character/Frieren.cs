using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Relics;

namespace FrierenMod.Scripts.Characters;

public sealed class Frieren : PlaceholderCharacterModel
{
    private const string Images = "res://FrierenMod/images/character/";
    private const string Scenes = "res://FrierenMod/scenes/";
    private const string Audio = "res://FrierenMod/audio/";
    public override string CustomVisualPath => Scenes + "frieren_combat.tscn";
    public override string CustomCharacterSelectBg => Scenes + "frieren_select.tscn";
    public override string CustomIconPath => Scenes + "frieren_icon.tscn";
    public override string CustomIconTexturePath => Images + "icon.png";
    public override string CustomIconOutlineTexturePath => Images + "icon.png";
    public override string CustomCharacterSelectIconPath => Images + "select_icon.png";
    public override string CustomCharacterSelectLockedIconPath => Images + "select_icon.png";
    public override string CustomMapMarkerPath => Images + "map_marker_sts2.png";
    public override string CustomRestSiteAnimPath => Scenes + "frieren_rest.tscn";
    public override string CustomMerchantAnimPath => Scenes + "frieren_merchant.tscn";
    public override string CustomEnergyCounterPath => Scenes + "frieren_energy.tscn";
    public override string CharacterSelectSfx => Audio + "I_am_frieren.mp3";
    public override string CustomAttackSfx => "";
    public override string CustomCastSfx => "";
    public override string CustomDeathSfx => Presentation.MusicCatalog.Defeat.Path;
    // 复用二代可在建筑师身上播放的魔法命中特效，伤害分配仍由原生结算计算。
    public override List<string> GetArchitectAttackVfx() =>
        ["vfx/vfx_attack_lightning", "vfx/vfx_starry_impact", "vfx/vfx_attack_lightning"];
    public override Color NameColor => new("e5e0f0");
    public override CharacterGender Gender => CharacterGender.Feminine;
    public override int StartingHp => 75;
    public override CardPoolModel CardPool => ModelDb.CardPool<FrierenCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<FrierenRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<FrierenPotionPool>();
    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<StrikeFrieren>(), ModelDb.Card<StrikeFrieren>(),
        ModelDb.Card<StrikeFrieren>(), ModelDb.Card<StrikeFrieren>(),
        ModelDb.Card<DefendFrieren>(), ModelDb.Card<DefendFrieren>(), ModelDb.Card<DefendFrieren>(),
        ModelDb.Card<Flow>(), ModelDb.Card<DefensiveMagic>(), ModelDb.Card<RapidChant>()
    ];
    public override IReadOnlyList<RelicModel> StartingRelics => [ModelDb.Relic<HolyEmblem>()];
}

using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace FrierenMod.Scripts.Relics;

// 二代长蛇升级遗物只在前三回合生效，不能替代一代每回合额外抽一张的长蛇戒指。
public sealed class KraftSerpentRing : FrierenRelic
{
    public override RelicRarity Rarity => RelicRarity.Event;
    public override string PackedIconPath => "res://images/atlases/relic_atlas.sprites/ring_of_the_snake.tres";
    protected override string PackedIconOutlinePath => "res://images/atlases/relic_outline_atlas.sprites/ring_of_the_snake.tres";
    protected override string BigIconPath => "res://images/relics/ring_of_the_snake.png";
    public override decimal ModifyHandDraw(Player player, decimal count) => player == Owner ? count + 1 : count;
}

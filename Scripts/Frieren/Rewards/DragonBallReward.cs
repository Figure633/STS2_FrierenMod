using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using BaseLib.Utils;
using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace FrierenMod.Scripts.Rewards;

public sealed class DragonBallReward(Player player) : CustomReward(player)
{
    [CustomEnum] public static RewardType FrierenDragonBall;
    public static readonly SavedSpireField<DragonBallReward,int> Number = new(() => 0,"FrierenDragonBallNumber");
    protected override RewardType RewardType => FrierenDragonBall;
    protected override string IconPath => "res://FrierenMod/images/cards/DragonBall1.png";
    public override LocString Description { get { var text=new LocString("frieren","DRAGON_BALL_REWARD"); text.Add("Number",DragonBallStore.Next(Player)); return text; } }
    public override bool IsPopulated => Number.Get(this) > 0;
    public override CreateRewardFromSave<CustomReward> DeserializeMethod => Restore;
    public static DragonBallReward Restore(SerializableReward save, Player owner) => new(owner);
    public override void Populate() { if (!IsPopulated) Number.Set(this, DragonBallStore.Next(Player)); }
    public override void MarkContentAsSeen() { }
    protected override Task<bool> OnSelect() => Task.FromResult(DragonBallStore.GrantNext(Player));
}

using FrierenMod.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace FrierenMod.Scripts.Mechanics;

public struct NetCommissionAction : INetAction
{
    public uint Request;
    public int Quest, Revision, Floor;
    public CommissionOperation Operation;
    public bool InCombat;
    public GameAction ToGameAction(Player player) => new CommissionAction(player, this);
    public void Serialize(PacketWriter writer) {
        writer.WriteUInt(Request); writer.WriteInt(Quest); writer.WriteInt(Revision); writer.WriteInt(Floor);
        writer.WriteInt((int)Operation); writer.WriteBool(InCombat);
    }
    public void Deserialize(PacketReader reader) {
        Request = reader.ReadUInt(); Quest = reader.ReadInt(); Revision = reader.ReadInt(); Floor = reader.ReadInt();
        Operation = (CommissionOperation)reader.ReadInt(); InCombat = reader.ReadBool();
    }
}

public sealed class CommissionAction(Player player, NetCommissionAction data) : GameAction
{
    public override ulong OwnerId => player.NetId;
    public override GameActionType ActionType => data.InCombat ? GameActionType.CombatPlayPhaseOnly : GameActionType.NonCombat;
    protected override async Task ExecuteAction() {
        bool success = false;
        try {
            if (data.InCombat == CombatManager.Instance.IsInProgress && data.Floor == player.RunState.TotalFloor)
                success = await CommissionCmd.Execute(player, data.Quest, data.Operation, data.Revision, new GameActionPlayerChoiceContext(this));
        } finally { MagicDeckNetwork.Complete(player, data.Request, success); }
    }
    protected override void CancelAction() => MagicDeckNetwork.Complete(player, data.Request, false);
    public override INetAction ToNetAction() => data;
}

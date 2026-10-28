using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace FrierenMod.Scripts.Mechanics;

// Owner 由原生动作信封的发送者决定，包内不能指定其他玩家。
public struct NetMagicDeckAction : INetAction
{
    public bool Setup;
    public bool Shop;
    public uint Request;
    public int Prop;
    public int[] Targets;
    public string Snapshot;
    public bool InCombat;
    public int Turn;

    public GameAction ToGameAction(Player player) => new MagicDeckAction(player, this);
    public void Serialize(PacketWriter writer)
    {
        writer.WriteBool(Setup);
        writer.WriteBool(Shop);
        writer.WriteUInt(Request);
        if (Setup || Shop) return;
        writer.WriteInt(Prop);
        writer.WriteByte((byte)Targets.Length, 2);
        foreach (int id in Targets) writer.WriteInt(id);
        writer.WriteString(Snapshot);
        writer.WriteBool(InCombat);
        writer.WriteInt(Turn);
    }
    public void Deserialize(PacketReader reader)
    {
        Setup = reader.ReadBool();
        Shop = reader.ReadBool();
        Request = reader.ReadUInt();
        Targets = [];
        Snapshot = "";
        if (Setup || Shop) return;
        Prop = reader.ReadInt();
        Targets = new int[reader.ReadByte(2)];
        for (int i = 0; i < Targets.Length; i++) Targets[i] = reader.ReadInt();
        Snapshot = reader.ReadString();
        InCombat = reader.ReadBool();
        Turn = reader.ReadInt();
    }
}

using System;

namespace NetLib.Packets
{
    public enum PacketType
    {
        Connection,
        Presentation,
        Information,
        FileSystem,
        DesktopConfig,
        Desktop,
        CameraConfig,
        Camera,
        Process,
        Shell,
        Disconnect,
        Uninstall,
    }

    [Serializable]
    public class Packet
    {
        public virtual PacketType PacketType { get; set; }

        public int ReceiverId
        {
            get { return _receiverId; }
            set { _receiverId = value; }
        }

        [NonSerialized] private int _receiverId = -1;
    }
}
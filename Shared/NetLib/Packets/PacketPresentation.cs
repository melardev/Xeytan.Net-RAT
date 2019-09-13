using System;
using NetLib.Models;

namespace NetLib.Packets
{
    [Serializable]
    public class PacketPresentation : Packet
    {
        public override PacketType PacketType => PacketType.Presentation;

        public SystemInfo SystemInfo { get; set; }
    }
}
using System;

namespace NetLib.Packets
{
    [Serializable]
    public class PacketSystemInformation : PacketPresentation
    {
        public override PacketType PacketType => PacketType.Information;
        
    }
}
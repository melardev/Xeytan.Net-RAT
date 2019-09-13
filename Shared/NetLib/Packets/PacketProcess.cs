using System;
using System.Collections.Generic;
using NetLib.Models;

namespace NetLib.Packets
{
    [Serializable]
    public class PacketProcess : Packet
    {
        public override PacketType PacketType
        {
            get => PacketType.Process;
        }

        public List<ProcessInfo> Processes { get; set; }
    }
}
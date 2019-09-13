using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetLib.Packets
{
    public enum ShellAction
    {
        Start,
        Push,
        Stop
    }

    [Serializable]
    public class PacketShell : Packet
    {
        public override PacketType PacketType => PacketType.Shell;
        public ShellAction ShellAction { get; set; }
        public string Data { get; set; }
    }
}
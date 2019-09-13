using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetLib.Packets
{
    public enum DesktopAction
    {
        Start,
        Push,
        Stop
    }

    [Serializable]
    public class PacketDesktop : Packet
    {
        public override PacketType PacketType => PacketType.Desktop;

        public DesktopAction DesktopAction { get; set; }
        public byte[] ImageData { get; set; }
    }
}
using System;
using System.Collections.Generic;
using NetLib.Models;

namespace NetLib.Packets
{
    [Serializable]
    public class PacketFileSystem : Packet
    {
        public enum FileSystemFocus
        {
            Roots,
            DirectoryEntries,
            DirectoryExists
        }

        public override PacketType PacketType => PacketType.FileSystem;
        public string BasePath { get; set; }
        public List<FileInfo> Files { get; set; }
        public List<DiskDriveInfo> Drives { get; set; }
        public FileSystemFocus FsFocus { get; set; }
    }
}
using System;

namespace NetLib.Models
{
    [Serializable]
    public class DiskDriveInfo
    {
        public string Name { get; set; }
        public string DriveFormat { get; set; }
        public string Label { get; set; }
        
    }
}

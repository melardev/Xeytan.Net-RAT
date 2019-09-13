using System;
using System.IO;

namespace NetLib.Models
{
    [Serializable]
    public class FileInfo
    {
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public FileAttributes FileAttributes { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime LastAccessTime { get; set; }
        public DateTime LastWriteTime { get; set; }
    }
}
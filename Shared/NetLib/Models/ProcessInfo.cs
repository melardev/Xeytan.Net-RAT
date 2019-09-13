using System;

namespace NetLib.Models
{
    [Serializable]
    public class ProcessInfo
    {
        public int Pid { get; set; }
        public string ProcessName { get; set; }
        public string FilePath { get; set; }
    }
}
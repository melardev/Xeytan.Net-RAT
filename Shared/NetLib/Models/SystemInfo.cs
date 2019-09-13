using System;
using System.Collections;

namespace NetLib.Models
{
    [Serializable]
    public class SystemInfo
    {
        public string PcName { get; set; }
        public string OperatingSystem { get; set; }
        public string UserName { get; set; }
        public string DotNetVersion { get; set; }
        public IDictionary EnvironmentVariables { get; set; }
    }
}
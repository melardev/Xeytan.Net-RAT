using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XeytanCSharpServer.Models
{
    class Client
    {
        public int Id { get; set; }
        public string RemoteIpAddress { get; set; }
        public int RemotePort { get; set; }
        public string PcName { get; set; }
        public string UserName { get; set; }
        public string DotNetVersion { get; set; }
        public string OperatingSystem { get; set; }
    }
}
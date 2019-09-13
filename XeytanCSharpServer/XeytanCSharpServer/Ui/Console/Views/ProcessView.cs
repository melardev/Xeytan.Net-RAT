using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetLib.Models;
using XeytanCSharpServer.Models;
using C = System.Console;

namespace XeytanCSharpServer.Ui.Console.Views
{
    class ProcessView : IClientView
    {
        private Client Client { get; set; }

        public void PrintBanner()
        {
            throw new NotImplementedException();
        }

        public string Loop()
        {
            throw new NotImplementedException();
        }

        public void SetActiveClient(Client client)
        {
            Client = client;
        }

        public static void PrintProcesses(Client client, List<ProcessInfo> processes)
        {
            C.WriteLine("Processes for {0}", client.PcName);
            foreach (ProcessInfo processInfo in processes)
            {
                C.WriteLine($"\tName: {Path.GetFileName(processInfo.FilePath)}");
                C.WriteLine($"\t\tPid: {processInfo.Pid}");
                C.WriteLine($"\t\tFilePath: {processInfo.FilePath}");
            }
        }
    }
}
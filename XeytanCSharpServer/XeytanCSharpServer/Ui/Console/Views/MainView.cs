using System.Collections.Generic;
using XeytanCSharpServer.Models;
using C = System.Console;

namespace XeytanCSharpServer.Ui.Console.Views
{
    class MainView : IView
    {
        private Client Client { get; set; }
        private string Banner { get; } = "XeytanCSharp>$ ";
        private string BannerFormatWithClient { get; } = "XeytanCSharp/{0}>$ ";
        private string BannerWithClient { get; set; }

        public void PrintBanner()
        {
            if (Client == null)
                C.Write(Banner);
            else
                C.Write(BannerFormatWithClient, Client.Id);
        }

        public string Loop()
        {
            while (true)
            {
                PrintBanner();
                string instruction = System.Console.ReadLine();
                if (instruction == null) continue;
                if (!ProcessInstruction(instruction))
                    return instruction;
            }
        }

        private bool ProcessInstruction(string instruction)
        {
            return false;
        }

        public void ShowClientConnection(Client client)
        {
            C.WriteLine($"\n[+] New connection from {client.RemoteIpAddress}\n" +
                        $"\tSession Id: {client.Id}\n" +
                        $"\tUsername: {client.UserName}\n" +
                        $"\tPc Name: {client.PcName}\n" +
                        $"\tOS: {client.OperatingSystem}\n" +
                        $"\tDotNet Version: {client.DotNetVersion}\n");
        }

        public void SetActiveClient(Client client)
        {
            Client = client;
            if (Client != null)
                BannerWithClient = string.Format(BannerFormatWithClient, Client.Id);
        }

        public void ListSessions(List<Client> clients)
        {
            foreach (Client client in clients)
            {
                C.Out.WriteLine(
                    $"{client.PcName}\n\tId: {client.Id}" +
                    $"\n\tAddress: ({client.RemoteIpAddress},{client.RemotePort}" +
                    $"\n\tUserName: {client.UserName}" +
                    $"\n\tDotNet Version: {client.DotNetVersion}");
            }
        }
    }
}
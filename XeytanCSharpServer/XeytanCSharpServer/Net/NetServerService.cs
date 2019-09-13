using NetLib.Packets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using XeytanCSharpServer.Models;

namespace XeytanCSharpServer.Net
{
    class NetServerService
    {
        private TcpListener _listener;
        private readonly BlockingCollection<Packet> _packetsToSend;
        private Thread _packetSenderThread;

        public bool Running { get; set; }
        public XeytanApplication Application { get; set; }
        public Dictionary<int, NetClientService> Clients { get; set; } = new Dictionary<int, NetClientService>();
        public Thread ServerThread { get; set; }

        private ReaderWriterLock ClientsLock { get; }

        public NetServerService()
        {
            _packetsToSend = new BlockingCollection<Packet>(new ConcurrentQueue<Packet>());
            ClientsLock = new ReaderWriterLock();
        }

        public void StartAsync()
        {
            ServerThread = new Thread(this.Start);
            ServerThread.Start();
        }

        public void Start()
        {
            _listener = new TcpListener(IPAddress.Loopback, 3002);
            _listener.Start();

            Running = true;
            _packetSenderThread = new Thread(SendPacketsThreadFunc);
            _packetSenderThread.Start();

            while (Running)
            {
                TcpClient client = _listener.AcceptTcpClient();

                int clientId = (int) client.Client.Handle;
                NetClientService netClientService = new NetClientService(this, client);
                bool added = false;
                try
                {
                    ClientsLock.AcquireWriterLock(15 * 1000); /* We could also use Timeout.Infinite */
                    Clients.Add(clientId, netClientService);
                    ClientsLock.ReleaseWriterLock();
                    added = true;
                }
                catch (ApplicationException exception)
                {
                    Console.WriteLine(
                        "======================================================================================");
                    Console.WriteLine(
                        "Deadlock detected? 30 seconds waiting for ClientsClock in NetServerService::Start()");
                    Console.WriteLine(
                        "======================================================================================");
                }

                if (added)
                    netClientService.InteractAsync();
                else
                    netClientService.ShutdownConnection();
            }
        }

        public void SendPacketsThreadFunc()
        {
            while (Running)
            {
                Packet packet = _packetsToSend.Take();
                NetClientService client = null;
                try
                {
                    ClientsLock.AcquireReaderLock(15 * 1000);
                    client = Clients[packet.ReceiverId];
                    ClientsLock.ReleaseReaderLock();
                }
                catch (ApplicationException exception)
                {
                    Console.WriteLine("=============================================================================");
                    Console.WriteLine("NetServerService::SendPacketsThreadFunc was not able to lock in 15 sec");
                    Console.WriteLine("=============================================================================");

                    if (ClientsLock.IsReaderLockHeld) // Should always be true...
                        ClientsLock.ReleaseReaderLock();
                    continue;
                }

                // SendPacket outside synchronization block, because it may take a while
                // And we don't care if at that time the user has just disconnected and removed
                // from Clients Dictionary, we handle that scenario in the Net client class
                client?.SendPacket(packet);
            }
        }

        public void OnPacketReceived(Client client, Packet packet)
        {
            Trace.WriteLine("Server::OnPacketReceived()");
            switch (packet.PacketType)
            {
                case PacketType.Presentation:
                    Application.OnPresentationData(client);
                    break;
                case PacketType.Information:
                {
                    PacketSystemInformation packetInfo = (PacketSystemInformation) packet;
                    Application.OnClientSystemInformation(client, packetInfo.SystemInfo);
                    break;
                }
                case PacketType.FileSystem:
                {
                    PacketFileSystem packetFs = (PacketFileSystem) packet;
                    if (packetFs.FsFocus == PacketFileSystem.FileSystemFocus.Roots)
                        Application.OnFileSystemRoots(client, packetFs.Drives);
                    else
                        Application.OnFileSystemDirEntries(client, packetFs.BasePath, packetFs.Files);
                    break;
                }
                case PacketType.Process:
                {
                    Application.OnProcessListReceived(client, ((PacketProcess) packet).Processes);
                    break;
                }
                case PacketType.Shell:
                {
                    Application.OnShellResultReceived(client, ((PacketShell) packet).ShellAction,
                        ((PacketShell) packet).Data);
                    break;
                }
                case PacketType.Desktop:
                {
                    Application.OnDesktopImage(client,
                        ((PacketDesktop) packet).DesktopAction,
                        ((PacketDesktop) packet).ImageData);
                    break;
                }
                default:
                    Trace.WriteLine("Unhandled packet type");
                    break;
            }
        }

        public void OnClientDisconnected(Client client, Exception exception)
        {
            try
            {
                ClientsLock.AcquireWriterLock(15 * 1000);
                Clients.Remove(client.Id);
            }
            catch (ApplicationException applicationException)
            {
                Console.WriteLine("=============================================================================");
                Console.WriteLine("NetServerService::SendPacketsThreadFunc was not able to lock in 15 sec");
                Console.WriteLine("=============================================================================");
            }
            finally
            {
                ClientsLock.ReleaseWriterLock();
            }

            Application.UiMediator.OnClientDisconnected(client);
        }


        public void FetchSystemInfo(Client client)
        {
            EnqueuePacket(new PacketSystemInformation
            {
                ReceiverId = client.Id
            });
        }

        public void FetchFileSystemDrives(Client client)
        {
            EnqueuePacket(new PacketFileSystem()
            {
                ReceiverId = client.Id,
                FsFocus = PacketFileSystem.FileSystemFocus.Roots
            });
        }

        public void FetchDirEntries(Client client, string basePath)
        {
            EnqueuePacket(new PacketFileSystem()
            {
                ReceiverId = client.Id,
                FsFocus = PacketFileSystem.FileSystemFocus.DirectoryEntries,
                BasePath = basePath
            });
        }

        public void FetchProcessList(Client client)
        {
            EnqueuePacket(new PacketProcess
            {
                ReceiverId = client.Id
            });
        }

        public void StartShell(Client client)
        {
            EnqueuePacket(new PacketShell
            {
                ReceiverId = client.Id
            });
        }

        public void SendShellCommand(Client client, string command)
        {
            EnqueuePacket(new PacketShell
            {
                ShellAction = ShellAction.Push,
                ReceiverId = client.Id,
                Data = command
            });
        }

        public void StartDesktop(Client client)
        {
            EnqueuePacket(new PacketDesktop
                {DesktopAction = DesktopAction.Start, ReceiverId = client.Id});
        }

        public void StopDesktop(Client client)
        {
            EnqueuePacket(new PacketDesktop
            {
                DesktopAction = DesktopAction.Stop, ReceiverId = client.Id
            });
        }

        public void EnqueuePacket(Packet packet)
        {
            if (packet.ReceiverId == -1)
                throw new ArgumentException("Packet receiver id must be set");
            _packetsToSend.Add(packet);
        }

        public Client GetClientById(int clientId)
        {
            try
            {
                ClientsLock.AcquireReaderLock(15 * 1000);
                if (Clients.TryGetValue(clientId, out var client))
                    return client.Client;
            }
            catch (ApplicationException exception)
            {
                Console.WriteLine("=============================================================================");
                Console.WriteLine("NetServerService::GetClientById was not able to lock in 15 sec");
                Console.WriteLine("=============================================================================");
            }
            finally
            {
                ClientsLock.ReleaseReaderLock();
            }

            return null;
        }

        public List<Client> GetClients()
        {
            try
            {
                ClientsLock.AcquireReaderLock(15 * 1000);
                return new List<Client>(Clients.Values.Select(netClient => netClient.Client));
            }
            catch (ApplicationException exception)
            {
            }
            finally
            {
                ClientsLock.ReleaseReaderLock();
            }

            return new List<Client>();
        }
    }
}
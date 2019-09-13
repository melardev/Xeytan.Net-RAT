using System.Collections.Concurrent;
using System.Threading;
using NetLib.Packets;

namespace NetLib.Services
{
    /// <summary>
    /// A Client Service that knows how to send Packets in a thread safe manner.
    /// In Synchronous socket programming there should not be much problem with synchronization
    /// at all, but In asynchronous sockets this is a must, here I use synchronous sockets, but
    /// anyways, it is a good practice to synchronize how packets are sent.
    /// </summary>
    public abstract class NetClientServiceThreadSafeSender : BaseNetClientService
    {
        private readonly BlockingCollection<Packet> _packetsToSend;
        private Thread _packetSenderThread;

        protected NetClientServiceThreadSafeSender()
        {
            _packetsToSend = new BlockingCollection<Packet>(new ConcurrentQueue<Packet>());
        }

        public virtual void Start()
        {
            Running = true;
            _packetSenderThread = new Thread(SendPacketsThreadFunc);
            _packetSenderThread.Start();
        }

        public void SendPacketsThreadFunc()
        {
            while (Running)
            {
                Packet packet = _packetsToSend.Take();
                base.SendPacket(packet);
            }
        }

        public void EnqueuePacket(Packet packet)
        {
            _packetsToSend.Add(packet);
        }

        public override void SendPacket(Packet packet)
        {
            _packetsToSend.Add(packet);
        }
    }
}
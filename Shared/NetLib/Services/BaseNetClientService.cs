using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using NetLib.Packets;

namespace NetLib.Services
{
    public abstract class BaseNetClientService
    {
        private Thread _runningThread;

        protected TcpClient TcpClient { get; private set; }

        private NetworkStream _networkStream;
        protected volatile bool Running;

        protected BaseNetClientService()
        {
        }

        protected BaseNetClientService(TcpClient tcpClient)
        {
            TcpClient = tcpClient;
            _networkStream = TcpClient.GetStream();
        }


        public virtual void InteractAsync()
        {
            _runningThread = new Thread(this.Interact);
            _runningThread.Start();
        }

        public virtual void Interact()
        {
            IFormatter formatter = new BinaryFormatter();
            Running = true;
            try
            {
                while (Running)
                {
                    Packet packet = (Packet) formatter.Deserialize(_networkStream);
                    OnPacketReceived(packet);
                }
            }
            catch (IOException exception)
            {
                Debug.WriteLine("An Error Occurred {0}", exception.ToString());
                OnException(exception);
            }
            catch (SerializationException exception)
            {
                Debug.WriteLine("Error on serialization {0}", exception.ToString());
                OnException(exception);
            }
        }

        protected abstract void OnException(Exception exception);

        public virtual void ShutdownConnection()
        {
            Running = false;
            _networkStream.Close();
            TcpClient.Close();
        }

        protected abstract void OnPacketReceived(Packet packet);

        public virtual void SendPacket(Packet packet)
        {
            IFormatter formatter = new BinaryFormatter();
            formatter.Serialize(_networkStream, packet);

            // Without Flush we may have bugs
            _networkStream.Flush();
        }

        public void SetTcpClient(TcpClient tcpClient)
        {
            this.TcpClient = tcpClient;
            this._networkStream = TcpClient.GetStream();
        }
    }
}
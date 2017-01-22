using Ghost.Network.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks.Dataflow;

namespace Ghost.Network
{
    public class NetPeer
    {
        private const int HeaderSize = 5;
        private static readonly EndPoint Any = new IPEndPoint(IPAddress.Any, 0);
        private static readonly TimeSpan Infinity = TimeSpan.FromMilliseconds(-1);

        private Timer m_ping;
        private Timer m_heart;
        private Socket m_socket;
        private ActionBlock<INetMessage> m_handler;
        private EventHandler<SocketAsyncEventArgs> m_io_handler;
        private TransformBlock<INetMessage, INetMessage> m_transform02;
        private ConcurrentDictionary<EndPoint, NetConnection> m_connections;
        private TransformManyBlock<SocketAsyncEventArgs, INetMessage> m_transform01;

        public INetMemoryManager Memory
        {
            get; private set;
        }

        public NetPeerConfiguration Configuration
        {
            get; private set;
        }

        public NetPeer(INetMemoryManager manager)
        {
            Memory = manager;
            m_connections = new ConcurrentDictionary<EndPoint, NetConnection>();
            m_io_handler = new EventHandler<SocketAsyncEventArgs>(NetIOComplete);
            m_ping = new Timer(OnPingTimerTick, this, Timeout.Infinite, Timeout.Infinite);
            m_heart = new Timer(OnHeartTimerTick, this, Timeout.Infinite, Timeout.Infinite);
            {
                var exOptions = new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount };
                m_handler = new ActionBlock<INetMessage>(new Action<INetMessage>(ProcessFlow), exOptions);
                m_transform02 = new TransformBlock<INetMessage, INetMessage>(new Func<INetMessage, INetMessage>(TransformFlow), exOptions);
                m_transform01 = new TransformManyBlock<SocketAsyncEventArgs, INetMessage>(new Func<SocketAsyncEventArgs, IEnumerable<INetMessage>>(TransformFlow), exOptions);
                m_transform02.LinkTo(m_handler);
                m_transform01.LinkTo(m_handler, x => x.Type != NetMessageType.Special);
                m_transform01.LinkTo(m_transform02, x => x.Type == NetMessageType.Special);
            }
        }
        public INetSerializable CreateHail()
        {
            return null;
        }

        public void Initialize(NetPeerConfiguration configuration)
        {
            Configuration = configuration;
            m_socket = new Socket(configuration.BindPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            m_socket.Bind(configuration.BindPoint);
            m_ping.Change(Configuration.PingInterval, Infinity);
            m_heart.Change(Configuration.HeartbeatInterval, Infinity);
            var args = Memory.GetReciveArgs();
            args.Completed += m_io_handler;
            if (!m_socket.ReceiveFromAsync(args))
                NetIOComplete(m_socket, args);
        }

        private void OnPingTimerTick(object state)
        {
            foreach (var item in m_connections)
            {
                var connection = item.Value;
                if (connection.State > NetConnectionState.ConnectionInitiation)
                    connection.Ping();
            }
            m_ping.Change(Configuration.PingInterval, Infinity);
        }

        private void OnHeartTimerTick(object state)
        {
            foreach (var item in m_connections)
            {
                var connection = item.Value;
                if (connection.State > NetConnectionState.ConnectionInitiation)
                {
                    if ((connection.LastPing + Configuration.PingTimeout) < NetTime.Now)
                        connection.Disconect("Connection timed out!");
                }
                if (connection.State == NetConnectionState.AwaitingApproval)
                {
                    if ((connection.ConnectionTime + Configuration.ConnectionTimeout) < NetTime.Now)
                        connection.Disconect("Connection timed out!");
                }
                else if (connection.State == NetConnectionState.Disconnected)
                {
                    if (m_connections.TryRemove(item.Key, out connection))
                        Memory.Free(connection.Clean());
                }
            }
            m_heart.Change(Configuration.HeartbeatInterval, Infinity);
        }

        private void ProcessFlow(INetMessage message)
        {
            var connection = message.Sender;
            switch (message.Type)
            {
                case NetMessageType.Connect:
                    if (Configuration.CanAccept)
                        connection.ProcessConnect(0d, message);
                    else
                    {

                    }
                    break;
            }
            message.Free();
        }

        private NetConnection Generate(EndPoint remote)
        {
            return Memory.GetConnection().Initialize(this, remote, NetConnectionState.ConnectionInitiation);
        }

        private INetMessage TransformFlow(INetMessage message)
        {
            var flags = (SpecialMessageFlags)message.ReadByte();
            if ((flags & SpecialMessageFlags.Encrypted) > 0)
            {

            }
            if ((flags & SpecialMessageFlags.Compressed) > 0)
            {

            }
            return message;
        }

        private void NetIOComplete(object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError != SocketError.Success)
            {

            }
            switch (args.LastOperation)
            {
                case SocketAsyncOperation.ReceiveFrom:
                    if (m_transform01.Post(args))
                    {

                    }
                    var newArgs = Memory.GetReciveArgs();
                    newArgs.Completed += m_io_handler;
                    if (!m_socket.ReceiveFromAsync(newArgs))
                        NetIOComplete(m_socket, newArgs);
                    break;
                case SocketAsyncOperation.SendTo:
                    if (args.UserToken is INetMessage message) message.Free();
                    break;
            }
        }

        private IEnumerable<INetMessage> TransformFlow(SocketAsyncEventArgs args)
        {
            var buffer = (INetMessage)args.UserToken;
            buffer.Length = args.BytesTransferred;
            var collection = NetMessageCollection<INetMessage>.Allocate();
            var connection = m_connections.GetOrAdd(args.RemoteEndPoint, Generate);
            while (buffer.Remaining > HeaderSize)
            {
                var message = Memory.GetMessage();
                message.Peer = this;
                message.Sender = connection;
                message.Type = (NetMessageType)buffer.ReadByte();
                var temp = buffer.ReadUInt16();
                message.Sequence = (ushort)(temp >> 1);
                message.IsFragmented = (temp & 1) == 1;
                temp = buffer.ReadUInt16();
                buffer.Read(message, (temp + 7) >> 3);
                message.Position = 0;
                collection.Add(message);
            }
            args.Completed -= m_io_handler;
            Memory.Free(args);
            return collection;
        }
    }
}
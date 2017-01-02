using DryIoc;
using Ghost.Core;
using Ghost.Network.Buffers;
using Ghost.Network.Utilities;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks.Dataflow;

namespace Ghost.Network
{
    public class NetPeerConfiguration
    {
        public string AppId
        {
            get; private set;
        }

        public bool CanAccept
        {
            get; private set;
        }

        public EndPoint BindPoint
        {
            get; set;
        }

        public NetPeerConfiguration(string appId, bool canAccept)
        {
            AppId = appId;
            CanAccept = canAccept;
        }
    }

    public class NetPeer
    {
        private const int HeaderSize = 5;
        private static readonly EndPoint Any = new IPEndPoint(IPAddress.Any, 0);

        private Timer m_ping;
        private Timer m_heart;
        private Socket m_socket;
        private ActionBlock<INetMessage> m_handler;
        private NetPeerConfiguration m_configuration;
        private EventHandler<SocketAsyncEventArgs> m_io_handler;
        private TransformBlock<INetMessage, INetMessage> m_transform02;
        private ConcurrentDictionary<EndPoint, NetConnection> m_connections;
        private TransformManyBlock<SocketAsyncEventArgs, INetMessage> m_transform01;

        public INetMemoryManager Memory
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

        public void Initialize(NetPeerConfiguration configuration)
        {
            m_configuration = configuration;
            m_socket = new Socket(configuration.BindPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            m_socket.Bind(configuration.BindPoint);
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
                connection.Ping();
            }
        }

        private void OnHeartTimerTick(object state)
        {
            foreach (var item in m_connections)
            {
                var connection = item.Value;
                //ToDo: use settings
                if (connection.LastPing.AddSeconds(3) < DateTime.Now)
                {
                    connection.Disconect("Connection timed out!");
                    if (m_connections.TryRemove(item.Key, out connection))
                    {
                        //ToDo: use logging
                    }
                }
                if (connection.WaitingForApproval)
                {
                    //ToDo: use settings
                    if (connection.ConnectionTime.AddSeconds(10) < DateTime.Now)
                    {
                        connection.Disconect("Connection timed out!");
                        if (m_connections.TryRemove(item.Key, out connection))
                        {
                            //ToDo: use logging
                        }
                    }
                }
            }
        }

        private void ProcessFlow(INetMessage message)
        {
            Console.WriteLine($"Recived {message.Type}");
            //throw new NotImplementedException();
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
                    (args.UserToken as INetMessage)?.Free();
                    break;
            }
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

        private IEnumerable<INetMessage> TransformFlow(SocketAsyncEventArgs args)
        {
            var collection = NetBufferCollection<INetMessage>.Allocate();
            var buffer = (INetBuffer)args.UserToken;
            buffer.SetBuffer(args);
            while (buffer.Remaining > HeaderSize)
            {
                var message = Memory.GetMessage();
                message.Type = (NetMessageType)buffer.ReadByte();
                var temp01 = buffer.ReadUInt16();
                var temp02 = buffer.ReadUInt16();
                buffer.Read(message, (temp02 + 7) >> 3);
                collection.Add(message);
            }
            args.Completed -= m_io_handler;
            Memory.Free(args);
            return collection;
        }
    }
}
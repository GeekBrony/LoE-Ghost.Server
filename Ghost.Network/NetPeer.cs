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
        private INetMemoryManager m_memory;
        private NetPeerConfiguration m_configuration;
        private ActionBlock<INetMessage> m_handler;
        private ConcurrentDictionary<EndPoint, NetConnection> m_connections;
        private TransformBlock<INetMessage, INetMessage> m_transform02;
        private TransformManyBlock<SocketAsyncEventArgs, INetMessage> m_transform01;

        public INetMemoryManager Memory
        {
            get { return m_memory; }
        }

        public NetPeer(IContainer container)
        {        
            m_memory = container.Resolve<INetMemoryManager>();
            m_connections = new ConcurrentDictionary<EndPoint, NetConnection>();
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
        }

        private static void OnPingTimerTick(object state)
        {
            var netPeer = (NetPeer)state;
            {
                var collection = netPeer.m_connections;
                foreach (var item in collection)
                {
                    var connection = item.Value;
                    connection.Ping();
                }
            }
        }

        private static void OnHeartTimerTick(object state)
        {
            var netPeer = (NetPeer)state;
            {
                var collection = netPeer.m_connections;
                foreach (var item in collection)
                {
                    var connection = item.Value;
                    //ToDo: use settings
                    if (connection.LastPing.AddSeconds(3) < DateTime.Now)
                    {
                        connection.Disconect("Connection timed out!");
                        if (collection.TryRemove(item.Key, out connection))
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
                            if (collection.TryRemove(item.Key, out connection))
                            {
                                //ToDo: use logging
                            }
                        }
                    }
                }
            }
        }

        private void ProcessFlow(INetMessage message)
        {
            throw new NotImplementedException();
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
                    var newArgs = m_memory.GetReceiveArgs();
                    newArgs.RemoteEndPoint = Any;
                    if (!m_socket.ReceiveFromAsync(newArgs))
                        NetIOComplete(m_socket, newArgs);
                    break;
                case SocketAsyncOperation.SendTo:
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
            var buffer = m_memory.GetEmptyBuffer();
            var collection = NetBufferCollection<INetMessage>.Allocate();
            buffer.SetBuffer(args);
            while (buffer.Remaining > HeaderSize)
            {
                var type = (NetMessageType)buffer.ReadByte();
                var message = m_memory.GetEmptyMessage();

                collection.Add(message);
            }
            buffer.FreeBuffer();
            m_memory.Free(buffer);
            return collection;
        }
    }
}
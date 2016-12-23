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

        private Timer m_ping;
        private Timer m_heart;
        private Socket m_socket;
        private INetMemoryManager m_memory;
        private NetPeerConfiguration m_configuration;
        private ActionBlock<INetIncomingMessage> m_handler;
        private ConcurrentDictionary<EndPoint, NetConnection> m_connections;
        private TransformBlock<INetIncomingMessage, INetIncomingMessage> m_transform02;
        private TransformManyBlock<SocketAsyncEventArgs, INetIncomingMessage> m_transform01;

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
                m_handler = new ActionBlock<INetIncomingMessage>(new Action<INetIncomingMessage>(ProcessFlow), exOptions);
                m_transform02 = new TransformBlock<INetIncomingMessage, INetIncomingMessage>(new Func<INetIncomingMessage, INetIncomingMessage>(TransformFlow), exOptions);
                m_transform01 = new TransformManyBlock<SocketAsyncEventArgs, INetIncomingMessage>(new Func<SocketAsyncEventArgs, IEnumerable<INetIncomingMessage>>(TransformFlow), exOptions);
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

        private static void ProcessFlow(INetIncomingMessage message)
        {
            var netPeer = message.Peer;
            throw new NotImplementedException();
        }

        private static void NetIOComplete(object sender, SocketAsyncEventArgs args)
        {
            var socket = (Socket)sender;
            var netPeer = (NetPeer)args.UserToken;
            if (args.SocketError != SocketError.Success)
            {

            }
            switch (args.LastOperation)
            {
                case SocketAsyncOperation.ReceiveFrom:
                    break;
                case SocketAsyncOperation.SendTo:
                    break;
            }
        }

        private static INetIncomingMessage TransformFlow(INetIncomingMessage message)
        {
            var netPeer = message.Peer;
            var flags = (SpecialMessageFlags)message.ReadByte();
            if ((flags & SpecialMessageFlags.Encrypted) > 0)
            {

            }
            if ((flags & SpecialMessageFlags.Compressed) > 0)
            {

            }
            return message;
        }

        private static IEnumerable<INetIncomingMessage> TransformFlow(SocketAsyncEventArgs args)
        {
            var netPeer = (NetPeer)args.UserToken;
            var buffer = s_memory.GetEmptyBuffer();
            buffer.SetBuffer(args);
            while (buffer.Remaining > HeaderSize)
            {

            }
            var collection = NetBufferCollection<INetIncomingMessage>.Allocate();
            return collection;
        }
    }
}
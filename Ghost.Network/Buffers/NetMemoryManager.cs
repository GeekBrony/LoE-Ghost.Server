using Ghost.Core;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace Ghost.Network.Buffers
{
    internal class NetMemoryManager : INetMemoryManager
    {
        private static readonly EndPoint Any = new IPEndPoint(IPAddress.Any, 0);

        private class Factory
        {
            public readonly Func<INetMessage> MessageGenerator;
            public readonly Func<NetConnection> ConnectionGenerator;
            public readonly Func<SocketAsyncEventArgs> SocketArgsGenerator;

            private NetMemoryManager m_manager;

            public Factory(NetMemoryManager manager)
            {
                m_manager = manager;
                MessageGenerator = BitConverter.IsLittleEndian ? new Func<INetMessage>(CreateMessageLE) : new Func<INetMessage>(CreateMessageBE);
                ConnectionGenerator = CreateConnection;
                SocketArgsGenerator = CreateSocketAsyncEventArgs;
            }
            private INetMessage CreateMessageLE()
            {
                return new NetMessageLE(m_manager);
            }
            private INetMessage CreateMessageBE()
            {
                throw new NotImplementedException();
            }
            private NetConnection CreateConnection()
            {
                return new NetConnection();
            }
            private SocketAsyncEventArgs CreateSocketAsyncEventArgs()
            {
                return new SocketAsyncEventArgs();
            }
        }

        private const int MinIndex = 6;
        private const int MinLength = 1 << MinIndex;
        private const int RecivePools = 6;
        private const int SegmentPools = 8;
        private const int ReciveLength = 8192;
        private const int MemoryPoolSegmentsCount = SegmentPools + RecivePools;
        private const int MemoryPoolSegmentsLength = 524288;

        private Factory m_factory;
        private MemoryPool m_pool_memory;
        private ObjectPool<INetMessage> m_pool_messages;
        private ObjectPool<NetConnection> m_pool_connections;
        private ConcurrentQueue<BufferSegment>[] m_segments;
        private ObjectPool<SocketAsyncEventArgs> m_pool_socket_args;

        public NetMemoryManager()
        {
            m_factory = new Factory(this);
            m_segments = new ConcurrentQueue<BufferSegment>[SegmentPools];
            m_pool_messages = new ObjectPool<INetMessage>(256, m_factory.MessageGenerator);
            m_pool_connections = new ObjectPool<NetConnection>(256, m_factory.ConnectionGenerator);
            m_pool_memory = new MemoryPool(MemoryPoolSegmentsLength, MemoryPoolSegmentsCount);
            m_pool_socket_args = new ObjectPool<SocketAsyncEventArgs>(512, m_factory.SocketArgsGenerator);
            for (int index = 0; index < m_segments.Length; index++)
            {
                m_segments[index] = new ConcurrentQueue<BufferSegment>();
                GenerateSegments(index, true);
            }
            for (int index = 0; index < RecivePools; index++)
                GenerateSegments(m_segments.Length - 1, true);
        }

        public INetMessage GetMessage()
        {
            return m_pool_messages.Allocate();
        }

        public NetConnection GetConnection()
        {
            return m_pool_connections.Allocate();
        }

        public INetMessage GetMessage(int minSize)
        {
            var segment = Allocate(minSize);
            var message = m_pool_messages.Allocate();
            message.SetBuffer(segment);
            return message;
        }

        public SocketAsyncEventArgs GetSendArgs()
        {
            return m_pool_socket_args.Allocate();
        }

        public SocketAsyncEventArgs GetReciveArgs()
        {
            var message = GetMessage(ReciveLength);
            var socketArgs = m_pool_socket_args.Allocate();
            socketArgs.RemoteEndPoint = Any;
            message.BindTo(socketArgs);
            message.Free();
            return socketArgs;
        }

        public BufferSegment Allocate(int minSize)
        {
            var index = Index(minSize);
            BufferSegment segment;
            while (!m_segments[index].TryDequeue(out segment))
                GenerateSegments(index, false);
            return segment;
        }

        public void Free(INetMessage message)
        {
            m_pool_messages.Release(message);
        }

        public void Free(BufferSegment segment)
        {
            if (segment.Manager != this)
                throw new InvalidOperationException();
            m_segments[segment.Index].Enqueue(segment);
        }

        public void Free(NetConnection connection)
        {
            m_pool_connections.Release(connection);
        }

        public void Free(SocketAsyncEventArgs args)
        {
            if (args.UserToken is INetMessage message)
                message.Free();
            args.UserToken = null;
            args.SetBuffer(null, 0, 0);
            args.RemoteEndPoint = null;
            m_pool_socket_args.Release(args);
        }

        private void GenerateSegments(int index, bool force)
        {
            var pool = m_segments[index];
            lock (pool)
            {
                if (!force && pool.Count > 0)
                    return;
                index += MinIndex;
                int length = 1 << index, count = MemoryPoolSegmentsLength >> index;
                index -= MinIndex;
                var buffer = m_pool_memory.Allocate();
                for (int i = 0; i < count; i++)
                    pool.Enqueue(new BufferSegment(index, buffer, i * length, length, this));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Index(int length)
        {
            if (length <= MinLength)
                return 0;
            if (length >= MemoryPoolSegmentsLength)
                throw new ArgumentOutOfRangeException(nameof(length));
            var vdouble = 0d;
            length--;
            length |= length >> 1;
            length |= length >> 2;
            length |= length >> 4;
            length |= length >> 8;
            length |= length >> 16;
            length++;
            unsafe
            {
                var dest = (uint*)&vdouble;
                if (BitConverter.IsLittleEndian)
                {
                    dest[1] = 0x43300000;
                    dest[0] = (uint)length;
                    vdouble -= 4503599627370496.0d;
                    return (int)(((dest[1] >> 20) - 0x3FF) - MinIndex);
                }
                else
                {
                    dest[0] = 0x43300000;
                    dest[1] = (uint)length;
                    vdouble -= 4503599627370496.0d;
                    return (int)(((dest[0] >> 20) - 0x3FF) - MinIndex);
                }
            }
        }
    }
}
using Ghost.Core;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace Ghost.Network.Buffers
{
    public interface INetMemoryManager
    {
        INetBuffer GetBuffer();

        INetMessage GetMessage();

        NetConnection GetConnection();

        INetBuffer GetBuffer(int minSize);

        INetMessage GetMessage(int minSize);

        SocketAsyncEventArgs GetSendArgs();

        SocketAsyncEventArgs GetReciveArgs();

        void Free(SocketAsyncEventArgs args);
    }

    public struct BufferSegment
    {
        private NetMemoryManager m_manager;

        public int Index
        {
            get; private set;
        }

        public int Offset
        {
            get; private set;
        }

        public int Length
        {
            get; private set;
        }

        public byte[] Buffer
        {
            get; private set;
        }

        public bool IsManaged => m_manager != null;

        public bool IsAllocated => Buffer != null;

        public INetMemoryManager Manager => m_manager;

        internal BufferSegment(int index, byte[] buffer, int offset, int length, NetMemoryManager manager)
        {
            Index = index;
            Buffer = buffer;
            Offset = offset;
            Length = length;
            m_manager = manager;
        }

        public void Free()
        {
            m_manager?.Free(this);
            Buffer = null;
            m_manager = null;
            Offset = 0;
            Length = 0;
        }
    }

    internal class NetMemoryManager : INetMemoryManager
    {
        private static readonly EndPoint Any = new IPEndPoint(IPAddress.Any, 0);

        private class Factory
        {
            public readonly Func<INetBuffer> BufferGenerator;
            public readonly Func<INetMessage> MessageGenerator;
            public readonly Func<NetConnection> ConnectionGenerator;
            public readonly Func<SocketAsyncEventArgs> SocketArgsGenerator;

            private NetMemoryManager m_manager;

            public Factory(NetMemoryManager manager)
            {
                m_manager = manager;
                BufferGenerator = BitConverter.IsLittleEndian ? new Func<INetBuffer>(CreateBufferLE) : new Func<INetBuffer>(CreateBufferBE);
                MessageGenerator = BitConverter.IsLittleEndian ? new Func<INetMessage>(CreateMessageLE) : new Func<INetMessage>(CreateMessageBE);
                ConnectionGenerator = CreateConnection;
                SocketArgsGenerator = CreateSocketAsyncEventArgs;
            }

            private INetBuffer CreateBufferLE()
            {
                return new NetBufferLE(m_manager);
            }

            private INetBuffer CreateBufferBE()
            {
                throw new NotImplementedException();
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
        private ObjectPool<INetBuffer> m_pool_buffers;
        private ObjectPool<INetMessage> m_pool_messages;
        private ObjectPool<NetConnection> m_pool_connections;
        private ConcurrentQueue<BufferSegment>[] m_segments;
        private ObjectPool<SocketAsyncEventArgs> m_pool_socket_args;

        public NetMemoryManager()
        {
            m_factory = new Factory(this);
            m_segments = new ConcurrentQueue<BufferSegment>[SegmentPools];
            m_pool_buffers = new ObjectPool<INetBuffer>(256, m_factory.BufferGenerator);
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

        public INetBuffer GetBuffer()
        {
            return m_pool_buffers.Allocate();
        }

        public INetMessage GetMessage()
        {
            return m_pool_messages.Allocate();
        }

        public NetConnection GetConnection()
        {
            return m_pool_connections.Allocate();
        }

        public INetBuffer GetBuffer(int minSize)
        {
            var segment = Allocate(minSize);
            var buffer = m_pool_buffers.Allocate();
            buffer.SetBuffer(segment);
            return buffer;
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

        public void Free<T>(T buffer)
            where T : INetBuffer
        {
            if (buffer is INetMessage)
                m_pool_messages.Release((INetMessage)buffer);
            else m_pool_buffers.Release(buffer);
        }

        public void Free(BufferSegment segment)
        {
            if (segment.Manager != this)
                throw new InvalidOperationException();
            m_segments[segment.Index].Enqueue(segment);
        }

        public void Free(SocketAsyncEventArgs args)
        {
            if (args.UserToken is INetBuffer)
                ((INetBuffer)args.UserToken).Free();
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
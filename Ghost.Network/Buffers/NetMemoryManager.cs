using Ghost.Core;
using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace Ghost.Network.Buffers
{
    public interface INetMemoryManager
    {
        INetBuffer GetEmptyBuffer();

        INetMessage GetEmptyMessage();

        INetBuffer GetBuffer(int minSize);

        INetMessage GetMessage(int minSize);

        SocketAsyncEventArgs GetReceiveArgs();

        void Free<T>(T buffer)
            where T : INetBuffer;
    }

    internal class NetMemoryManager : INetMemoryManager
    {
        private class Factory
        {
            public readonly Func<INetBuffer> BufferGenerator;
            public readonly Func<INetMessage> MessageGenerator;
            public readonly Func<SocketAsyncEventArgs> SocketArgsGenerator;

            private NetMemoryManager m_manager;

            public Factory(NetMemoryManager manager)
            {
                m_manager = manager;
                BufferGenerator = BitConverter.IsLittleEndian ? new Func<INetBuffer>(CreateBufferLE) : new Func<INetBuffer>(CreateBufferBE);
                MessageGenerator = BitConverter.IsLittleEndian ? new Func<INetMessage>(CreateMessageLE) : new Func<INetMessage>(CreateMessageBE);
                SocketArgsGenerator = () => new SocketAsyncEventArgs();
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
        }

        private const int MinIndex = 6;
        private const int MaxIndex = 18;
        private const int MinLength = 1 << MinIndex;
        private const int MaxLength = 1 << MaxIndex;
        private const int RecivePools = 4;
        private const int SegmentPools = MaxIndex - MinIndex;
        private const int MemoryPools = SegmentPools + RecivePools;

        private Factory m_factory;
        private MemoryPool m_pool_memory;
        private ObjectPool<INetBuffer> m_pool_buffers;
        private ObjectPool<INetMessage> m_pool_messages;
        private ObjectPool<SocketAsyncEventArgs> m_pool_socket_args;
        private ConcurrentQueue<ArraySegment<byte>>[] m_segments;

        public NetMemoryManager()
        {
            m_factory = new Factory(this);
            m_pool_memory = new MemoryPool(MaxLength, MemoryPools);
            m_segments = new ConcurrentQueue<ArraySegment<byte>>[SegmentPools];
            m_pool_buffers = new ObjectPool<INetBuffer>(256, m_factory.BufferGenerator);
            m_pool_messages = new ObjectPool<INetMessage>(256, m_factory.MessageGenerator);
            m_pool_socket_args = new ObjectPool<SocketAsyncEventArgs>(256, m_factory.SocketArgsGenerator);
            for (int index = 0; index < m_segments.Length; index++)
            {
                m_segments[index] = new ConcurrentQueue<ArraySegment<byte>>();
                GenerateSegments(index);
            }
            for (int index = 0; index < RecivePools; index++)
                GenerateSegments(m_segments.Length - 1);
        }

        public INetBuffer GetEmptyBuffer()
        {
            return m_pool_buffers.Allocate();
        }

        public INetMessage GetEmptyMessage()
        {
            return m_pool_messages.Allocate();
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

        public SocketAsyncEventArgs GetReceiveArgs()
        {
            var segment = Allocate(MaxLength);
            var socketArgs = m_pool_socket_args.Allocate();
            socketArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);
            return socketArgs;
        }

        public ArraySegment<byte> Allocate(int minSize)
        {
            var index = Index(minSize);
            ArraySegment<byte> segment;
            while (!m_segments[index].TryDequeue(out segment))
                GenerateSegments(index);
            return segment;
        }

        public void Free<T>(T buffer)
            where T : INetBuffer
        {
            if (buffer is INetMessage)
                m_pool_messages.Release((INetMessage)buffer);
            else m_pool_buffers.Release((INetMessage)buffer);
        }

        public void Free(ArraySegment<byte> segment)
        {
            m_segments[Index(segment.Count)].Enqueue(segment);
        }

        private void GenerateSegments(int index)
        {
            var pool = m_segments[index];
            lock (pool)
            {
                if (pool.Count > 0)
                    return;
                index += MinIndex;
                int length = 1 << index, count = MaxLength >> index;
                var buffer = m_pool_memory.Allocate();
                for (int i = 0; i < count; i++)
                    pool.Enqueue(new ArraySegment<byte>(buffer, i * length, length));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Index(int length)
        {
            if (length <= MinLength)
                return 0;
            if (length >= MaxLength)
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
                dest[BitConverter.IsLittleEndian ? 1 : 0] = 0x43300000;
                dest[BitConverter.IsLittleEndian ? 0 : 1] = (uint)length;
                vdouble -= 4503599627370496.0d;
                return (int)(((dest[BitConverter.IsLittleEndian ? 1 : 0] >> 20) - 0x3FF) - MinIndex);
            }
        }
    }
}
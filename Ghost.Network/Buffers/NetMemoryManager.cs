using Ghost.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ghost.Network.Buffers
{
    public interface INetMemoryManager
    {

    }

    internal class NetMemoryManager
    {
        private MemoryPool m_pool_memory;
        private ObjectPool<INetBuffer> m_pool_buffers;
        private ObjectPool<INetMessage> m_pool_messages;

        private ConcurrentQueue<ArraySegment<byte>>[] m_segments;

        public NetMemoryManager()
        {
            m_pool_memory = new MemoryPool(262144, 16);
            m_segments = new ConcurrentQueue<ArraySegment<byte>>[26];
            for (int index = 0; index < m_segments.Length; index++)
                m_segments[index] = new ConcurrentQueue<ArraySegment<byte>>();
        }

        public INetMessage CreateMessage(int minSize)
        {
            var message = m_pool_messages.Allocate();

            return message;
        }
    }
}
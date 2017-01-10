using System;
using System.Collections.Concurrent;

namespace Ghost.Core
{
    public class MemoryPool
    {
        public const int MinimumLength = 262144;
        private const int MinimumMask = MinimumLength - 1;

        private static readonly ConcurrentQueue<WeakReference<byte[]>> s_refs_pool = new ConcurrentQueue<WeakReference<byte[]>>();

        private static void CreateWeakRefs(int capacity)
        {
            for (int count = capacity; count > 0; count--)
                s_refs_pool.Enqueue(new WeakReference<byte[]>(null));
        }

        private static ConcurrentQueue<object> CreatePool(int capacity, int length)
        {
            var pool = new ConcurrentQueue<object>();
            for (int count = capacity; count > 0; count--)
                pool.Enqueue(new byte[length]);
            return pool;
        }

        private int m_length;
        private int m_capacity;

        private ConcurrentQueue<object> m_pool;

        public MemoryPool(int length, int capacity)
        {
            m_length = (length + MinimumMask) & ~MinimumMask;
            m_capacity = capacity;
            m_pool = CreatePool(m_capacity, m_length);
            CreateWeakRefs(capacity >> 1);
        }

        public byte[] Allocate()
        {
             while (m_pool.TryDequeue(out var item))
            {
                if (item is byte[] strong)
                    return strong;
                else if (item is WeakReference<byte[]> weak)
                { 
                    if (weak.TryGetTarget(out strong))
                    {
                        weak.SetTarget(null);
                        s_refs_pool.Enqueue(weak);
                        return strong;
                    }
                }
            }
            return new byte[m_length];
        }

        public void Release(byte[] buffer)
        {
            if (buffer?.Length != m_length)
                throw new ArgumentException("Argument not valid", nameof(buffer));
            Array.Clear(buffer, 0, buffer.Length);
            if (m_pool.Count >= m_capacity)
            {
                if (s_refs_pool.TryDequeue(out var weak))
                {
                    weak.SetTarget(buffer);
                    m_pool.Enqueue(weak);
                }
                else m_pool.Enqueue(new WeakReference<byte[]>(buffer));
            }
            else m_pool.Enqueue(buffer);
        }
    }
}
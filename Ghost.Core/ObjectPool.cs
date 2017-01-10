using System;
using System.Collections.Concurrent;

namespace Ghost.Core
{
    public class ObjectPool<T>
        where T : class
    {
        private static readonly ConcurrentQueue<WeakReference<T>> s_refs_pool = new ConcurrentQueue<WeakReference<T>>();

        private static void CreateWeakRefs(int capacity)
        {
            for (int count = capacity; count > 0; count--)
                s_refs_pool.Enqueue(new WeakReference<T>(null));
        }

        private static ConcurrentQueue<object> CreatePool(int capacity, Func<T> generator)
        {
            var pool = new ConcurrentQueue<object>();
            for (int count = capacity; count > 0; count--)
                pool.Enqueue(generator());
            return pool;
        }

        private int m_capacity;
        private Func<T> m_generator;
        private ConcurrentQueue<object> m_pool;

        public ObjectPool(int capacity, Func<T> generator)
        {
            m_capacity = capacity;
            m_generator = generator;
            m_pool = CreatePool(m_capacity, m_generator);
            CreateWeakRefs(capacity >> 1);
        }

        public T Allocate()
        {
            while (m_pool.TryDequeue(out var item))
            {
                if (item is T strong)
                    return strong;
                else if (item is WeakReference<T> weak)
                {
                    if (weak.TryGetTarget(out strong))
                    {
                        weak.SetTarget(null);
                        s_refs_pool.Enqueue(weak);
                        return strong;
                    }
                }
            }
            return m_generator();
        }

        public void Release(T item)
        {
            if (m_pool.Count >= m_capacity)
            {
                if (s_refs_pool.TryDequeue(out var weak))
                {
                    weak.SetTarget(item);
                    m_pool.Enqueue(weak);
                }
                else m_pool.Enqueue(new WeakReference<T>(item));
            }
            else m_pool.Enqueue(item);
        }
    }
}
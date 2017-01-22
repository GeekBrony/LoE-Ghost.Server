using Ghost.Core;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Ghost.Network.Utilities
{
    internal class NetMessageCollection<T> : IEnumerable<T>, IEnumerator<T>
        where T : INetMessage
    {
        private const int GrowthCount = 8;

        private static readonly ObjectPool<NetMessageCollection<T>> s_pool = new ObjectPool<NetMessageCollection<T>>(128, () => new NetMessageCollection<T>(GrowthCount));

        public static NetMessageCollection<T> Allocate()
        {
            return s_pool.Allocate();
        }

        private int m_index;
        private int m_position;
        private T[] m_collection;

        T IEnumerator<T>.Current
        {
            get
            {
                if (m_position < 0 || m_position >= m_collection.Length)
                    return default(T);
                return m_collection[m_position];
            }
        }

        object IEnumerator.Current
        {
            get
            {
                if (m_position < 0 || m_position >= m_collection.Length)
                    return default(T);
                return m_collection[m_position];
            }
        }

        private NetMessageCollection(int capacity)
        {
            m_position = -1;
            m_collection = new T[capacity];
        }

        public void Add(T item)
        {
            var newLength = ((m_index + 1) + (GrowthCount - 1)) & ~(GrowthCount - 1);
            if (newLength > m_collection.Length)
                Array.Resize(ref m_collection, newLength);
            m_collection[m_index] = item;
            m_index++;
        }

        void IEnumerator.Reset()
        {
            m_position = -1;
        }

        void IDisposable.Dispose()
        {
            if (m_collection.Length > GrowthCount)
                m_collection = new T[GrowthCount];
            else Array.Clear(m_collection, 0, m_collection.Length);
            s_pool.Release(this);
        }

        bool IEnumerator.MoveNext()
        {
            return ++m_position < m_index;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
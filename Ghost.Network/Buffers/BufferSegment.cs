namespace Ghost.Network.Buffers
{

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
}
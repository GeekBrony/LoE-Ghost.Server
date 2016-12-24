using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Network.Buffers
{
    internal unsafe class NetBufferLE : INetBuffer
    {
        protected byte* m_end;
        protected byte* m_start;
        protected byte* m_offset;
        protected byte* m_length;
        protected byte m_bits_offset;
        protected GCHandle m_handle;
        protected NetMemoryManager m_manager;
        protected ArraySegment<byte> m_segment;

        public long Length
        {
            get
            {
                return m_length - m_start;
            }
        }

        public long Position
        {
            get
            {
                return m_offset - m_start;
            }
        }

        public long Capacity
        {
            get
            {
                return m_end - m_start;
            }
        }

        public long Remaining
        {
            get
            {
                return m_length - m_start;
            }
        }

        public long LengthBits
        {
            get
            {
                return (m_length - m_start) << 3;
            }
        }

        public long PositionBits
        {
            get
            {
                return ((m_offset - m_start) << 3) + m_bits_offset;
            }
        }

        public long CapacityBits
        {
            get
            {
                return (m_end - m_start) << 3;
            }
        }

        public NetBufferLE(NetMemoryManager manager)
        {
            m_manager = manager;
        }

        public void SetBuffer(byte[] buffer)
        {
            SetBuffer(buffer, 0, buffer.Length);
        }

        public void SetBuffer(int offset, int length)
        {
            SetBuffer(m_segment.Array, offset, length);
        }

        public void SetBuffer(ArraySegment<byte> seg)
        {
            if (seg.Array == null)
                throw new ArgumentNullException(nameof(seg));
            SetBuffer(seg.Array, seg.Offset, seg.Count);
        }

        public void SetBuffer(SocketAsyncEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));
            SetBuffer(args.Buffer, args.Offset, args.Count);
            if (args.BytesTransferred > 0)
                m_length = m_start + args.BytesTransferred;
        }

        public void SetBuffer(byte[] buffer, int offset, int length)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || offset > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (length < 0 || (offset + length) > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(length));
            if (m_handle.IsAllocated) FreeBuffer();
            m_handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            m_start = (byte*)m_handle.AddrOfPinnedObject().ToPointer() + offset;
            m_end = m_start + length;
            m_length = m_start;
            m_offset = m_start;
            m_bits_offset = 0;
        }

        public bool ReadBoolean()
        {
            CheckRead(0);
            var value = ((*m_offset >> m_bits_offset) & 1) == 1;
            m_bits_offset = (byte)((m_bits_offset + 1) % (sizeof(byte) << 3));
            if (m_bits_offset == 0) m_offset++;
            return value;
        }

        public byte ReadByte()
        {
            CheckRead(sizeof(byte));
            byte value;
            if (m_bits_offset == 0)
                value = *m_offset;
            else
                value = (byte)(*(ushort*)m_offset >> m_bits_offset);
            m_offset += sizeof(byte);
            return value;
        }

        public int ReadInt32()
        {
            CheckRead(sizeof(int));
            int value;
            if (m_bits_offset == 0)
                value = *(int*)m_offset;
            else
                value = (int)((*(uint*)m_offset | (ulong)m_offset[sizeof(uint)] << (sizeof(uint) << 3)) >> m_bits_offset);
            m_offset += sizeof(int);
            return value;
        }

        public long ReadInt64()
        {
            CheckRead(sizeof(long));
            long value;
            if (m_bits_offset == 0)
                value = *(long*)m_offset;
            else
                value = (long)((*(ulong*)m_offset >> m_bits_offset) | ((ulong)m_offset[sizeof(ulong)] << ((sizeof(ulong) << 3) - m_bits_offset)));
            m_offset += sizeof(long);
            return value;
        }

        public void Write(bool value)
        {
            CheckWrite(0);
            if (value)
                *m_offset |= (byte)(1 << m_bits_offset);
            else
                *m_offset &= (byte)~(1 << m_bits_offset);
            m_bits_offset = (byte)((m_bits_offset + 1) % (sizeof(byte) << 3));
            if (m_bits_offset == 0)
            {
                m_offset++;
                if (m_offset > m_length) m_length = m_offset;
            }
        }

        public void Write(byte value)
        {
            CheckWrite(sizeof(byte));
            if (m_bits_offset == 0)
            {
                *m_offset = value;
                m_offset += sizeof(byte);
            }
            else
            {
                *(ushort*)m_offset = (ushort)((*(ushort*)m_offset & ~(byte.MaxValue << m_bits_offset)) | (value << m_bits_offset));
                m_offset += sizeof(byte);
            }
            if (m_offset > m_length) m_length = m_offset;
        }

        public void Write(int value)
        {
            CheckWrite(sizeof(int));
            if (m_bits_offset == 0)
            {
                *(int*)m_offset = value;
                m_offset += sizeof(int);
            }
            else
            {
                *(int*)m_offset = (*m_offset & ~(byte.MaxValue << m_bits_offset)) | (value << m_bits_offset);
                m_offset += sizeof(int);
                *m_offset = (byte)((byte)(*m_offset & (byte.MaxValue << m_bits_offset)) | ((uint)value >> (sizeof(int) << 3) - m_bits_offset));
            }
            if (m_offset > m_length) m_length = m_offset;
        }

        public void Write(long value)
        {
            CheckWrite(sizeof(long));
            if (m_bits_offset == 0)
            {
                *(long*)m_offset = value;
                m_offset += sizeof(long);
            }
            else
            {
                *(long*)m_offset = (byte)(*m_offset & ~(byte.MaxValue << m_bits_offset)) | (value << m_bits_offset);
                m_offset += sizeof(long);
                *m_offset = (byte)((byte)(*m_offset & (byte.MaxValue << m_bits_offset)) | ((ulong)value >> (sizeof(long) << 3) - m_bits_offset));
            }
            if (m_offset > m_length) m_length = m_offset;
        }

        public void FreeBuffer()
        {
            if (m_handle.IsAllocated)
            {
                m_end = (byte*)0;
                m_start = (byte*)0;
                m_offset = (byte*)0;
                m_length = (byte*)0;
                m_bits_offset = 0;
                m_handle.Free();
                m_segment = default(ArraySegment<byte>);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void CheckRead(int length)
        {
            if ((m_offset + length) > m_length)
                throw new InvalidOperationException("Read past buffer length");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void CheckWrite(int length)
        {
            if ((m_offset + length) > m_end)
            {
                var start = m_start;
                var segment = m_manager.Allocate(m_segment.Count << 1);
                var handle = GCHandle.Alloc(segment.Array, GCHandleType.Pinned);
                Buffer.MemoryCopy(m_start, handle.AddrOfPinnedObject().ToPointer(), segment.Count, Length);
                m_handle.Free();
                m_start = (byte*)handle.AddrOfPinnedObject().ToPointer() + segment.Offset;
                m_end = m_start + segment.Count;
                m_length = m_start + (m_length - start);
                m_offset = m_start + (m_offset - start);
                m_handle = handle;
            }
        }
    }
}
using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Network.Buffers
{
    internal unsafe class NetBufferLE : INetBuffer
    {
        private byte* m_end;
        private byte* m_start;
        private byte* m_offset;
        private byte* m_length;
        private byte m_bits_offset;
        private GCHandle m_handle;

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

        public bool Expandable
        {
            get;
            set;
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

        public void SetBuffer(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (m_handle.IsAllocated) FreeBuffer();
            m_handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            m_start = (byte*)m_handle.AddrOfPinnedObject().ToPointer();
            m_end = m_start + buffer.Length;
            m_length = m_start;
            m_offset = m_start;
            m_bits_offset = 0;
        }

        public void SetBuffer(int offset, int length)
        {
            if (m_handle.IsAllocated)
            {
                var buffer = (byte[])m_handle.Target;
                if (offset < 0 || offset > buffer.Length)
                    throw new ArgumentOutOfRangeException(nameof(offset));
                if (length < 0 || (offset + length) > buffer.Length)
                    throw new ArgumentOutOfRangeException(nameof(length));
                m_start = (byte*)m_handle.AddrOfPinnedObject().ToPointer() + offset;
                m_end = m_start + length;
                m_length = m_start;
                m_offset = m_start;
                m_bits_offset = 0;
            }
            else throw new InvalidOperationException("Buffer not set");
        }

        public void SetBuffer(SocketAsyncEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));
            SetBuffer(args.Buffer, args.Offset, args.Count);
            if (args.BytesTransferred > 0)
                m_length = m_start + args.BytesTransferred;
        }

        public void PrepareToSend(SocketAsyncEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));
            if (m_handle.IsAllocated)
            {
                var buffer = (byte[])m_handle.Target;
                if (args.Buffer == buffer)
                    args.SetBuffer((int)(m_start - (byte*)m_handle.AddrOfPinnedObject().ToPointer()), (int)Length);
                else
                    args.SetBuffer(buffer, (int)(m_start - (byte*)m_handle.AddrOfPinnedObject().ToPointer()), (int)Length);
                FreeBuffer();
            }
            else throw new InvalidOperationException("Buffer not set");
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FreeBuffer()
        {
            if (m_handle.IsAllocated)
            {
                m_end = (byte*)0;
                m_start = (byte*)0;
                m_offset = (byte*)0;
                m_length = (byte*)0;
                m_bits_offset = 0;
                m_handle.Free();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckRead(int length)
        {
            if ((m_offset + length) > m_length)
                throw new InvalidOperationException("Read past buffer length");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckWrite(int length)
        {
            if ((m_offset + length) > m_end)
            {
                if (Expandable)
                {
                    throw new NotImplementedException();
                }
                else throw new InvalidOperationException("Write past buffer capacity");
            }
        }
    }
}
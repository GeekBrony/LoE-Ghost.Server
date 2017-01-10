using Ghost.Core.Utilities;
using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Ghost.Network.Buffers
{
    internal unsafe class NetBufferLE : INetBuffer
    {
        protected byte* m_end;
        protected byte* m_start;
        protected byte* m_offset;
        protected byte* m_length;
        protected int m_ref_count;
        protected GCHandle m_handle;
        protected byte m_bits_offset;
        protected BufferSegment m_segment;
        protected NetMemoryManager m_manager;

        public long Length
        {
            get
            {
                return m_length - m_start;
            }
            set
            {
                if (value < 0 || value >= Capacity)
                    throw new ReplaceMeException();
                m_length = m_start + value;
            }
        }

        public long Position
        {
            get => m_offset - m_start;
            set
            {
                if (value < 0 || value >= Capacity)
                    throw new ReplaceMeException();
                m_offset = m_start + value;
                m_bits_offset = 0;
                if (m_offset > m_length)
                    m_length = m_offset;
            }
        }

        public long Capacity
        {
            get => m_end - m_start;
        }

        public long Remaining
        {
            get => m_length - m_offset;
        }

        public long LengthBits
        {
            get => (m_length - m_start) << 3;
        }

        public long PositionBits
        {
            get => ((m_offset - m_start) << 3) + m_bits_offset;
        }

        public long CapacityBits
        {
            get => (m_end - m_start) << 3;
        }

        public NetBufferLE(NetMemoryManager manager)
        {
            m_manager = manager;
        }

        public void Free()
        {
            if (m_segment.IsAllocated)
            {
                if (m_segment.IsManaged)
                {
                    if (Interlocked.Decrement(ref m_ref_count) == 0)
                        m_segment.Free();
                    else return;
                }
                FreeBuffer();
                m_manager.Free(this);
            }
        }

        public void SetBuffer(byte[] buffer)
        {
            SetBuffer(buffer, 0, buffer.Length);
        }

        public void SetBuffer(BufferSegment segment)
        {
            if (!segment.IsAllocated)
                throw new ArgumentNullException(nameof(segment));
            SetBuffer(segment.Buffer, segment.Offset, segment.Length);
            m_ref_count = 1;
            m_segment = segment;
        }

        public void SetBuffer(SocketAsyncEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));
            if (args.Buffer != m_segment.Buffer)
                SetBuffer(args.Buffer, args.Offset, args.Count);
            if (args.BytesTransferred > 0)
                m_length = m_start + args.BytesTransferred;
        }

        public void SetBuffer(byte[] buffer, int offset, int length)
        {
            CheckBuffer(buffer, offset, length);
            if (m_segment.IsAllocated)
                m_segment.Free();
            if (m_handle.IsAllocated)
                FreeBuffer();
            m_segment = new BufferSegment(-1, buffer, offset, length, null);
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

        public short ReadInt16()
        {
            CheckRead(sizeof(short));
            short value;
            if (m_bits_offset == 0)
                value = *(short*)m_offset;
            else
                value = (short)((*(ushort*)m_offset | (ulong)m_offset[sizeof(ushort)] << (sizeof(ushort) << 3)) >> m_bits_offset);
            m_offset += sizeof(short);
            return value;
        }

        public ushort ReadUInt16()
        {
            CheckRead(sizeof(ushort));
            ushort value;
            if (m_bits_offset == 0)
                value = *(ushort*)m_offset;
            else
                value = (ushort)((*(ushort*)m_offset | (ulong)m_offset[sizeof(ushort)] << (sizeof(ushort) << 3)) >> m_bits_offset);
            m_offset += sizeof(ushort);
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

        public float ReadSingle()
        {
            CheckRead(sizeof(float));
            float value;
            if (m_bits_offset == 0)
                value = *(float*)m_offset;
            else
            {
                var temp = (*(uint*)m_offset | (ulong)m_offset[sizeof(float)] << (sizeof(float) << 3)) >> m_bits_offset;
                value = *(float*)&temp;
            }
            m_offset += sizeof(float);
            return value;
        }

        public double ReadDouble()
        {
            CheckRead(sizeof(double));
            double value;
            if (m_bits_offset == 0)
                value = *(double*)m_offset;
            else
            {
                var temp = (*(ulong*)m_offset >> m_bits_offset) | ((ulong)m_offset[sizeof(double)] << ((sizeof(double) << 3) - m_bits_offset));
                value = *(double*)&temp;
            }
            m_offset += sizeof(double);
            return value;
        }

        public string ReadString()
        {
            var length = (int)ReadVarUInt32();
            if (length > 0 && length <= Remaining)
            {
                var value = string.Empty;
                if (m_bits_offset == 0)
                    value = Encoding.UTF8.GetString(m_offset, length);
                else
                {
                    var buffer = stackalloc byte[length];
                    Copy(this, buffer, length);
                    value = Encoding.UTF8.GetString(buffer, length);
                }
                m_offset += length;
                return value;
            }
            return string.Empty;
        }

        public int ReadVarInt32()
        {
            var result = ReadUInt32Var();
            return (int)(result >> 1) ^ -(int)(result & 1);
        }

        public uint ReadVarUInt32()
        {
            var result = 0;
            var offset = 0;
            while (Remaining > 0)
            {
                var temp = ReadByte();
                result |= (temp & 0x7f) << offset;
                offset += 7;
                if ((temp & 0x80) == 0 || offset == 35)
                    break;
            }
            return (uint)result;
        }

        public void Read(INetBuffer buffer, int bytes)
        {
            CheckRead(sizeof(byte) * bytes);
            if (m_bits_offset == 0)
                buffer.Write(m_segment.Buffer, (int)(m_segment.Offset + Position), bytes);
            else Copy(this, buffer, bytes);
            m_offset += bytes;
        }

        public void Write(bool value)
        {
            CheckWrite(m_bits_offset != 0 ? 0 : 1);
            if (value)
                *m_offset |= (byte)(1 << m_bits_offset);
            else
                *m_offset &= (byte)~(1 << m_bits_offset);
            if (m_bits_offset == 0) m_length = m_offset + 1;
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
            if (m_offset > m_length)
                m_length = m_offset + (m_bits_offset == 0 ? 0 : 1);
        }

        public void Write(short value)
        {
            CheckWrite(sizeof(short));
            if (m_bits_offset == 0)
            {
                *(short*)m_offset = value;
                m_offset += sizeof(short);
            }
            else
            {
                *(short*)m_offset = (short)((*m_offset & ~(byte.MaxValue << m_bits_offset)) | (value << m_bits_offset));
                m_offset += sizeof(short);
                *m_offset = (byte)((byte)(*m_offset & (byte.MaxValue << m_bits_offset)) | ((uint)value >> (sizeof(short) << 3) - m_bits_offset));
            }
            if (m_offset > m_length)
                m_length = m_offset + (m_bits_offset == 0 ? 0 : 1);
        }

        public void Write(ushort value)
        {
            CheckWrite(sizeof(ushort));
            if (m_bits_offset == 0)
            {
                *(ushort*)m_offset = value;
                m_offset += sizeof(ushort);
            }
            else
            {
                *(ushort*)m_offset = (ushort)((*m_offset & ~(byte.MaxValue << m_bits_offset)) | (value << m_bits_offset));
                m_offset += sizeof(ushort);
                *m_offset = (byte)((byte)(*m_offset & (byte.MaxValue << m_bits_offset)) | ((uint)value >> (sizeof(ushort) << 3) - m_bits_offset));
            }
            if (m_offset > m_length)
                m_length = m_offset + (m_bits_offset == 0 ? 0 : 1);
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
            if (m_offset > m_length)
                m_length = m_offset + (m_bits_offset == 0 ? 0 : 1);
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
            if (m_offset > m_length)
                m_length = m_offset + (m_bits_offset == 0 ? 0 : 1);
        }

        public void Write(float value)
        {
            CheckWrite(sizeof(float));
            if (m_bits_offset == 0)
            {
                *(float*)m_offset = value;
                m_offset += sizeof(float);
            }
            else
            {
                var temp = *(uint*)&value;
                *(uint*)m_offset = (uint)(*m_offset & ~(byte.MaxValue << m_bits_offset)) | (temp << m_bits_offset);
                m_offset += sizeof(float);
                *m_offset = (byte)((byte)(*m_offset & (byte.MaxValue << m_bits_offset)) | (temp >> (sizeof(float) << 3) - m_bits_offset));
            }
            if (m_offset > m_length)
                m_length = m_offset + (m_bits_offset == 0 ? 0 : 1);
        }

        public void Write(double value)
        {
            CheckWrite(sizeof(double));
            if (m_bits_offset == 0)
            {
                *(double*)m_offset = value;
                m_offset += sizeof(double);
            }
            else
            {
                var temp = *(ulong*)&value;
                *(ulong*)m_offset = (byte)(*m_offset & ~(byte.MaxValue << m_bits_offset)) | (temp << m_bits_offset);
                m_offset += sizeof(double);
                *m_offset = (byte)((byte)(*m_offset & (byte.MaxValue << m_bits_offset)) | (temp >> (sizeof(double) << 3) - m_bits_offset));
            }
            if (m_offset > m_length)
                m_length = m_offset + (m_bits_offset == 0 ? 0 : 1);
        }

        public void Write(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                WriteVar(0);
                return;
            }
            int maxLength = Encoding.UTF8.GetMaxByteCount(value.Length), length = 0;
            var buffer = stackalloc byte[maxLength];
            fixed (char* src = value)
                length = Encoding.UTF8.GetBytes(src, value.Length, buffer, maxLength);
            WriteVar((uint)length);
            CheckWrite(length);
            if (m_bits_offset == 0)
                Buffer.MemoryCopy(buffer, m_offset, m_end - m_offset, length);
            else Copy(buffer, this, length);
            m_offset += length;
            if (m_offset > m_length)
                m_length = m_offset + (m_bits_offset == 0 ? 0 : 1);
        }

        public void WriteVar(int value)
        {
            var temp = (uint)(value << 1) ^ (uint)(value >> 31);
            while (temp > 0x80)
            {
                Write((byte)((temp & 0x7F) | 0x80));
                temp >>= 7;
            }
            Write((byte)temp);
        }

        public void WriteVar(uint value)
        {
            while (value > 0x80)
            {
                Write((byte)((value & 0x7F) | 0x80));
                value >>= 7;
            }
            Write((byte)value);
        }

        public void Write(byte[] buffer)
        {
            Write(buffer, 0, buffer.Length);
        }

        public void Write(byte[] buffer, int offset)
        {
            Write(buffer, offset, buffer.Length - offset);
        }

        public void Write(byte[] buffer, int offset, int length)
        {
            CheckBuffer(buffer, offset, length);
            CheckWrite(sizeof(byte) * length);
            fixed (byte* src = &buffer[offset])
            {
                if (m_bits_offset == 0)
                    Buffer.MemoryCopy(src, m_offset, m_end - m_offset, length);
                else Copy(src, this, length);
            }
            m_offset += length;
            if (m_offset > m_length)
                m_length = m_offset + (m_bits_offset == 0 ? 0 : 1);
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
                DoExpand((int)((m_offset - m_start) + length));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void Copy(byte* src, INetBuffer dest, int count)
        {
            for (var index = count >> 3; index > 0; index--, src += sizeof(long))
                dest.Write(*(long*)src);
            if ((count & 0x4) == 0x4)
            {
                dest.Write(*(int*)src);
                src += sizeof(int);
            }
            for (var index = count & 0x3; index > 0; index--, src++)
                dest.Write(*src);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void Copy(INetBuffer src, byte* dest, int count)
        {
            for (var index = count >> 3; index > 0; index--, dest += sizeof(long))
                *(long*)dest = src.ReadInt64();
            if ((count & 0x4) == 0x4)
            {
                *(int*)dest = src.ReadInt32();
                dest += sizeof(int);
            }
            for (var index = count & 0x3; index > 0; index--, dest++)
                *dest = src.ReadByte();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void Copy(INetBuffer src, INetBuffer dest, int count)
        {
            for (var index = count >> 3; index > 0; index--)
                dest.Write(src.ReadInt64());
            for (var index = count & 0x7; index > 0; index--)
                dest.Write(src.ReadByte());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void CheckBuffer(byte[] buffer, int offset, int length)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || offset > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (length < 0 || (offset + length) > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(length));
        }

        private void FreeBuffer()
        {
            m_end = (byte*)0;
            m_start = (byte*)0;
            m_offset = (byte*)0;
            m_length = (byte*)0;
            m_bits_offset = 0;
            m_handle.Free();
            m_segment = default(BufferSegment);
        }

        private void DoExpand(int minSize)
        {
            var start = m_start;
            var segment = m_manager.Allocate(minSize);
            var handle = GCHandle.Alloc(segment.Buffer, GCHandleType.Pinned);
            var offset = (byte*)handle.AddrOfPinnedObject().ToPointer() + segment.Offset;
            Buffer.MemoryCopy(m_start, offset, segment.Length, Length);
            if (m_handle.IsAllocated) m_handle.Free();
            if (m_segment.IsAllocated) m_segment.Free();
            m_start = offset;
            m_end = m_start + segment.Length;
            m_length = m_start + (m_length - start);
            m_offset = m_start + (m_offset - start);
            m_handle = handle;
            m_segment = segment;
        }
    }
}
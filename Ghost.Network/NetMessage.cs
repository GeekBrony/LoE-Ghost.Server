using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Ghost.Network
{
    internal class NetMessageBE : NetMessage
    {
        public override byte ReadByte()
        {
            throw new NotImplementedException();
        }

        public override double ReadDouble()
        {
            throw new NotImplementedException();
        }

        public override short ReadInt16()
        {
            throw new NotImplementedException();
        }

        public override int ReadInt32()
        {
            throw new NotImplementedException();
        }

        public override long ReadInt64()
        {
            throw new NotImplementedException();
        }

        public override sbyte ReadSByte()
        {
            throw new NotImplementedException();
        }

        public override float ReadSingle()
        {
            throw new NotImplementedException();
        }

        public override ushort ReadUInt16()
        {
            throw new NotImplementedException();
        }

        public override uint ReadUInt32()
        {
            throw new NotImplementedException();
        }

        public override ulong ReadUInt64()
        {
            throw new NotImplementedException();
        }

        public override void Write(int value)
        {
            throw new NotImplementedException();
        }

        public override void Write(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(ulong value)
        {
            throw new NotImplementedException();
        }

        public override void Write(float value)
        {
            throw new NotImplementedException();
        }

        public override void Write(double value)
        {
            throw new NotImplementedException();
        }

        public override void Write(uint value)
        {
            throw new NotImplementedException();
        }

        public override void Write(short value)
        {
            throw new NotImplementedException();
        }

        public override void Write(ushort value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte value)
        {
            throw new NotImplementedException();
        }

        public override void Write(sbyte value)
        {
            throw new NotImplementedException();
        }
    }

    internal unsafe class NetMessageLE : NetMessage
    {
        private byte* m_end;
        private byte* m_start;
        private byte* m_length;
        private byte* m_r_offset;
        private byte* m_w_offset;
        private int m_bit_offset;
        private GCHandle m_handle;

        private ulong* m_end_l;
        private ulong* m_start_l;
        private ulong* m_length_l;
        private ulong* m_r_offset_l;
        private ulong* m_w_offset_l;
        private int m_bit_offset_l;
        private GCHandle m_handle_l;

        public NetMessageLE()
        {
            var bytes = new byte[64];
            new Random().NextBytes(bytes);
            bytes[0] = byte.MaxValue;
            bytes[1] = byte.MaxValue;
            bytes[2] = byte.MaxValue;
            bytes[3] = byte.MaxValue;
            bytes[4] = byte.MaxValue;
            m_handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            m_start = (byte*)m_handle.AddrOfPinnedObject().ToPointer();
            m_end = m_start + bytes.Length;
            m_r_offset = m_start;
            m_w_offset = m_r_offset;
            m_length = m_r_offset + 8;
            m_bit_offset = 3;
        }

        public override byte ReadByte()
        {
            if (m_r_offset >= m_length)
                throw new InvalidOperationException("Reading past buffer length!");
            if (m_bit_offset == 0)
            {
                var value = *m_r_offset;
                m_r_offset++;
                return value;
            }
            else
            {
                var value = (byte)((*(ushort*)m_r_offset >> m_bit_offset) & byte.MaxValue);
                m_r_offset++;
                return value;
            }
        }

        public override double ReadDouble()
        {
            throw new NotImplementedException();
        }

        public override short ReadInt16()
        {
            throw new NotImplementedException();
        }

        public override int ReadInt32()
        {
            throw new NotImplementedException();
        }

        public override long ReadInt64()
        {
            throw new NotImplementedException();
        }

        public override sbyte ReadSByte()
        {
            throw new NotImplementedException();
        }

        public override float ReadSingle()
        {
            throw new NotImplementedException();
        }

        public override ushort ReadUInt16()
        {
            throw new NotImplementedException();
        }

        public override uint ReadUInt32()
        {
            throw new NotImplementedException();
        }

        public override ulong ReadUInt64()
        {
            throw new NotImplementedException();
        }

        public override void Write(int value)
        {
            throw new NotImplementedException();
        }

        public override void Write(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(ulong value)
        {
            throw new NotImplementedException();
        }

        public override void Write(float value)
        {
            throw new NotImplementedException();
        }

        public override void Write(double value)
        {
            throw new NotImplementedException();
        }

        public override void Write(uint value)
        {
            throw new NotImplementedException();
        }

        public override void Write(short value)
        {
            throw new NotImplementedException();
        }

        public override void Write(ushort value)
        {
            var offset01 = m_bit_offset;
            var offset02 = m_bit_offset + (sizeof(ushort) << 3);

            if (offset02 > (sizeof(long) << 3))
            {
                *m_w_offset_l = *m_w_offset_l & ~((ulong)ushort.MaxValue << offset01) | ((ulong)value << offset01);
                offset02 %= (sizeof(long) << 3);
                m_w_offset_l++;
                *m_w_offset_l = *m_w_offset_l & ~((ulong)ushort.MaxValue >> ((sizeof(ushort) << 3) - offset02)) | ((ulong)value >> ((sizeof(ushort) << 3) - offset02));
            }
            else *m_w_offset_l = *m_w_offset_l & ~((ulong)ushort.MaxValue << offset01) | ((ulong)value << offset01);
            m_bit_offset = offset02;
        }

        public override void Write(byte value)
        {
            if (m_w_offset >= m_length)
            {
                m_length++;
                if (m_length >= m_end)
                    GrowthBuffer();
            }
            if (m_bit_offset == 0)
            {
                *m_w_offset = value;
                m_w_offset++;
            }
            else
            {
                *(ushort*)m_w_offset = (ushort)(*(ushort*)m_w_offset & ~(byte.MaxValue << m_bit_offset) | (value << m_bit_offset));
                m_w_offset++;
            }
        }

        public override void Write(sbyte value)
        {
            if (m_w_offset >= m_length)
            {
                m_length++;
                if (m_length >= m_end)
                    GrowthBuffer();
            }
            if (m_bit_offset == 0)
            {
                *m_w_offset = (byte)value;
                m_w_offset++;
            }
            else
            {
                *(ushort*)m_w_offset = (ushort)(*(ushort*)m_w_offset & ~(byte.MaxValue << m_bit_offset) | ((byte)value << m_bit_offset));
                m_w_offset++;
            }
        }

        private void GrowthBuffer()
        {
            throw new NotImplementedException();
        }
    }

    public abstract class NetMessage
    {
        protected const int Size = sizeof(long) << 3;

        public abstract sbyte ReadSByte();
        public abstract byte ReadByte();
        public abstract short ReadInt16();
        public abstract ushort ReadUInt16();
        public abstract int ReadInt32();
        public abstract uint ReadUInt32();
        public abstract long ReadInt64();
        public abstract ulong ReadUInt64();
        public abstract float ReadSingle();
        public abstract double ReadDouble();

        public abstract void Write(sbyte value);
        public abstract void Write(byte value);
        public abstract void Write(short value);
        public abstract void Write(ushort value);
        public abstract void Write(int value);
        public abstract void Write(uint value);
        public abstract void Write(long value);
        public abstract void Write(ulong value);
        public abstract void Write(float value);
        public abstract void Write(double value);

        public static NetMessage CreateNew()
        {
            if (BitConverter.IsLittleEndian)
                return new NetMessageLE();
            return new NetMessageBE();
        }
    }
}
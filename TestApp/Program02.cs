using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TestApplication
{
    public class MemoryPool
    {

    }

    public unsafe class NetMessage
    {
        private ulong* m_end;
        private ulong* m_start;
        private ulong* m_offset;
        private ulong* m_length;
        private byte m_bits_offset;
        private GCHandle m_handle;

        public bool Expandable
        {
            get;
            set;
        }

        public NetMessage()
        {

        }

        public void SetBuffer(byte[] buffer)
        {
            m_handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            m_start = (ulong*)m_handle.AddrOfPinnedObject().ToPointer();
            m_end = m_start + (buffer.Length >> 3);
            m_offset = m_start;
        }
        public void SetBitOffsetTest(byte bits)
        {
            m_bits_offset = bits;
        }

        public void ResetTest()
        {
            m_offset = m_start;
        }

        public void PrintTest()
        {
            for (ulong* cur = m_start, end = m_end; cur != end; cur++)
                Console.WriteLine((*cur).ToString("X16"));
        }

        public void SetBuffer(int offset, int length)
        {

        }

        public void SetBuffer(byte[] buffer, int offset, int length)
        {

        }

        public long ReadInt64()
        {
            if (m_bits_offset == 0)
            {
                var value = *m_offset;
                m_offset++;
                return (long)value;
            }
            else
            {
                var value = *m_offset >> m_bits_offset;
                m_offset++;
                value |= *m_offset << ((sizeof(long) << 3) - m_bits_offset);
                return (long)value;
            }
        }

        public void Write(long value)
        {
            WriteProlog(sizeof(long));
            if (m_bits_offset == 0)
            {
                *m_offset = (ulong)value;
                m_offset++;
            }
            else
            {
                var temp = *m_offset;
                temp &= ulong.MaxValue >> ((sizeof(ulong) << 3) - m_bits_offset);
                temp |= (ulong)value << m_bits_offset;
                *m_offset = temp;
                m_offset++;
                temp = *m_offset;
                temp &= ulong.MaxValue << m_bits_offset;
                temp |= (ulong)value >> ((sizeof(ulong) << 3) - m_bits_offset);
                *m_offset = temp;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FreeBuffer()
        {
            if (m_handle.IsAllocated)
            {
                m_end = (ulong*)0;
                m_start = (ulong*)0;
                m_offset = (ulong*)0;
                m_length = (ulong*)0;
                m_handle.Free();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteProlog(int size)
        {

        }
    }

    class Program02
    {

        static void Main(string[] args)
        {
            var buffer = new byte[128];
            var message = new NetMessage();
            message.SetBuffer(buffer);

            message.SetBitOffsetTest(32);
            message.Write(-1);
            message.Write(-1);
            message.Write(-1);
            message.Write(-1);
            message.PrintTest();
            message.ResetTest();
            Debug.Assert(message.ReadInt64() == -1, "NotPass");
            Debug.Assert(message.ReadInt64() == -1, "NotPass");
            Debug.Assert(message.ReadInt64() == -1, "NotPass");
            Debug.Assert(message.ReadInt64() == -1, "NotPass");
            Console.ReadLine();
        }
    }
}
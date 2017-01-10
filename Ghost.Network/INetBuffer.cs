using Ghost.Network.Buffers;
using System.Net.Sockets;

namespace Ghost.Network
{

    public interface INetBuffer
    {
        long Length
        {
            get;
            set;
        }

        long Position
        {
            get;
            set;
        }

        long Capacity
        {
            get;
        }

        long Remaining
        {
            get;
        }

        bool ReadBoolean();

        byte ReadByte();

        short ReadInt16();

        ushort ReadUInt16();

        int ReadInt32();

        long ReadInt64();

        float ReadSingle();

        double ReadDouble();

        string ReadString();

        int ReadVarInt32();

        uint ReadVarUInt32();

        void Read(INetBuffer buffer, int length);

        void Write(bool value);

        void Write(byte value);

        void Write(short value);

        void Write(ushort value);

        void Write(int value);

        void Write(long value);

        void Write(float value);

        void Write(double value);

        void Write(string value);

        void WriteVar(int value);

        void WriteVar(uint value);

        void Write(byte[] buffer);

        void Write(byte[] buffer, int offset);

        void Write(byte[] buffer, int offset, int length);

        void Free();

        void SetBuffer(byte[] buffer);

        void SetBuffer(BufferSegment segment);

        void SetBuffer(SocketAsyncEventArgs args);

        void SetBuffer(byte[] buffer, int offset, int length);

    }
}
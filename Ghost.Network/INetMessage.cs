using Ghost.Network.Buffers;
using Ghost.Network.Utilities;
using System.Net.Sockets;
using System.Numerics;

namespace Ghost.Network
{
    public interface INetMessage
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

        long LengthBits
        {
            get;
        }

        long PositionBits
        {
            get;
        }

        long CapacityBits
        {
            get;
        }

        NetPeer Peer
        {
            get;
            set;
        }

        ushort Sequence
        {
            get;
            set;
        }

        bool IsFragmented
        {
            get;
            set;
        }

        NetMessageType Type
        {
            get;
            set;
        }

        NetConnection Sender
        {
            get;
            set;
        }

        INetMemoryManager Manager
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

        Vector2 ReadVector2();

        Vector3 ReadVector3();

        int ReadVarInt32();

        uint ReadVarUInt32();

        void Read(INetMessage message, int length);

        void Write(bool value);

        void Write(byte value);

        void Write(short value);

        void Write(ushort value);

        void Write(int value);

        void Write(long value);

        void Write(float value);

        void Write(double value);

        void Write(string value);

        void Write(Vector2 value);

        void Write(Vector3 value);

        void Write(Vector4 value);

        void Write(Quaternion value);

        void WriteVar(int value);

        void WriteVar(uint value);

        void Write(byte[] buffer);

        void Write(byte[] buffer, int offset);

        void Write(byte[] buffer, int offset, int length);

        void Free();

        void BindTo(SocketAsyncEventArgs args);

        void SetBuffer(byte[] buffer);

        void SetBuffer(BufferSegment segment);

        void SetBuffer(SocketAsyncEventArgs args);

        void SetBuffer(byte[] buffer, int offset, int length);
    }
}
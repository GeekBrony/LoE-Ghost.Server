using System.Net.Sockets;

namespace Ghost.Network
{
    public interface INetMemoryManager
    {
        INetBuffer GetBuffer();

        INetMessage GetMessage();

        NetConnection GetConnection();

        INetBuffer GetBuffer(int minSize);

        INetMessage GetMessage(int minSize);

        SocketAsyncEventArgs GetSendArgs();

        SocketAsyncEventArgs GetReciveArgs();

        void Free(NetConnection connection);

        void Free(SocketAsyncEventArgs args);
    }
}
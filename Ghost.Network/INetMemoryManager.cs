using System.Net.Sockets;

namespace Ghost.Network
{
    public interface INetMemoryManager
    {
        INetMessage GetMessage();

        NetConnection GetConnection();

        INetMessage GetMessage(int minSize);

        SocketAsyncEventArgs GetSendArgs();

        SocketAsyncEventArgs GetReciveArgs();

        void Free(NetConnection connection);

        void Free(SocketAsyncEventArgs args);
    }
}
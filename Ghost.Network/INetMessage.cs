using System.Net.Sockets;

namespace Ghost.Network
{

    public interface INetMessage : INetBuffer
    {
        NetPeer Peer
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

        void BindTo(SocketAsyncEventArgs args);
    }
}
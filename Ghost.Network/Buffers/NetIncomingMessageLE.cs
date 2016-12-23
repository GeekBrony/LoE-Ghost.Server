using System;

namespace Ghost.Network.Buffers
{
    internal class NetIncomingMessageLE : NetMessageLE, INetIncomingMessage
    {
        public NetPeer Peer
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public NetMessageType Type
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public NetConnection Sender
        {
            get
            {
                throw new NotImplementedException();
            }
        }


        public NetIncomingMessageLE(NetMemoryManager manager)
            : base(manager)
        {

        }
    }
}
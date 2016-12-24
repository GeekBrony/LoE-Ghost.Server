using System;

namespace Ghost.Network.Buffers
{
    internal class NetIncomingMessageLE : NetMessageLE, INetIncomingMessage
    {
        public NetIncomingMessageLE(NetMemoryManager manager)
            : base(manager)
        {

        }
    }
}
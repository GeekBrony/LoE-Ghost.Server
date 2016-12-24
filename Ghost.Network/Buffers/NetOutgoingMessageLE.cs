namespace Ghost.Network.Buffers
{
    internal class NetOutgoingMessageLE : NetMessageLE, INetOutgoingMessage
    {
        public NetOutgoingMessageLE(NetMemoryManager manager) 
            : base(manager)
        {

        }
    }
}
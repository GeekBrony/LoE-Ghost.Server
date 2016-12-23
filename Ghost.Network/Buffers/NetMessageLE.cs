namespace Ghost.Network.Buffers
{
    internal class NetMessageLE : NetBufferLE, INetMessage
    {
        public NetMessageLE(NetMemoryManager manager)
            : base(manager)
        {

        }
    }
}
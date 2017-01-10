namespace Ghost.Network
{
    public abstract class NetChannel
    {
        public abstract void Send(INetMessage message);

        public abstract void Recive(INetMessage message);
    }
}
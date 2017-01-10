namespace Ghost.Network
{

    public interface INetSerializable
    {
        int AllocSize { get; }

        void OnSerialize(INetMessage message);

        void OnDeserialize(INetMessage message);
    }
}
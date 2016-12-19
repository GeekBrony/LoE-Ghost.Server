namespace Ghost.Core
{
    public interface IMemoryPool
    {
        byte[] Allocate();

        void Release(byte[] buffer);
    }
}
using Ghost.Network.Buffers;

namespace Ghost.Network
{
    public static class TestContext
    {
        public static INetMemoryManager CreateNetMemoryManager()
        {
            return new NetMemoryManager();
        }
    }
}

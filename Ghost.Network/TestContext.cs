using Ghost.Network.Buffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

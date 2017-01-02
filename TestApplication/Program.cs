
using Ghost.Core;
using Ghost.Network;
using System;
using System.Net;

namespace TestApplication
{
    public class TestApplication : GhostApplication
    {
        public TestApplication()
            : base("Test")
        {

        }
    }
    class Program
    {

        static void Main(string[] args)
        {
            var peer = new NetPeer(TestContext.CreateNetMemoryManager());
            peer.Initialize(new NetPeerConfiguration("PNet", true) { BindPoint = new IPEndPoint(IPAddress.Any, 14000) });
            Console.ReadLine();
        }
    }
}

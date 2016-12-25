using Ghost.Core;
using Ghost.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestApplication
{
    class Program
    {
        public class LoEServer : GhostApplication
        {
            public LoEServer() 
                : base("Legends of Equestria Server")
            {
            }
        }

        static void Main(string[] args)
        {
            GhostApplication.Startup<LoEServer>();

            //var test = TestContext.CreateNetMemoryManager();
            //var test01 = test.GetMessage(12);
            ////var test02 = test.GetMessage(120);
            ////var test03 = test.GetMessage(128);
            //test01.Write(true);
            //test01.Write(true);
            //test01.Write(0);
            //test01.Write(true);
            //test01.Write(true);
            //test01.Write(false);
            //test01.Write(false);
            //test01.Write(true);
            //test01.Write(true);
            //test01.Write(byte.MaxValue);
            //test01.Write(short.MinValue);

            //test01.Write(long.MaxValue);
            //test01.Write(long.MaxValue);
            //test01.Write(long.MaxValue);
            //test01.Write(long.MaxValue);
            //test01.Write(long.MaxValue);
            //test01.Write(long.MaxValue);
            //test01.Write(int.MaxValue);
            //test01.Write(short.MaxValue);
            //test01.Write(byte.MaxValue);
            //test01.Write(byte.MaxValue);
            //test01.Write(true);
            //Console.ReadLine();
        }
    }
}

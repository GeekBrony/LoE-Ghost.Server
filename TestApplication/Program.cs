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
        static void Main(string[] args)
        {
            var test = TestContext.CreateNetMemoryManager();
            var test01 = test.GetMessage(12);
            var test02 = test.GetMessage(120);
            var test03 = test.GetMessage(128);
            Console.ReadLine();
        }
    }
}

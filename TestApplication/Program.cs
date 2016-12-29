
using Ghost.Core;
using Ghost.Core.Utilities;
using System;

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
            GhostApplication.Startup<TestApplication>();
        }
    }
}

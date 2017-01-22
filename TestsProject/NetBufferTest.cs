using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ghost.Network;
using System.Numerics;
using System.Linq;

namespace TestsProject
{
    [TestClass]
    public class NetBufferTest
    {
        [TestMethod]
        public void TestReadComplex()
        {
            var manager = (INetMemoryManager)typeof(INetMemoryManager).Assembly.GetTypes()
                .FirstOrDefault(x => x.Name.Contains("NetMemoryManager") && x.IsClass)?
                .GetConstructor(Type.EmptyTypes)?.Invoke(null);
            Assert.IsNotNull(manager);
            var buffer = manager.GetBuffer(1);
            var vector = new Vector3(13.666f, 0.123f, 4.815162342f);
            buffer.Write(true);
            buffer.Write(vector);
            Assert.AreEqual(12L, buffer.Position);
            Assert.AreEqual(13L, buffer.Length);
            buffer.Position = 0L;
            Assert.AreEqual(13L, buffer.Remaining);
            var boolean = buffer.ReadBoolean();
            var vector2 = buffer.ReadVector3();
            Assert.AreEqual(true, boolean);
            Assert.AreEqual(vector, vector2);
        }
    }
}
using DryIoc;
using Ghost.Network.Utilities;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Ghost.Network
{
    public class RpcProxyGenerator
    {
        public IContainer Container
        {
            get; private set;
        }

        public RpcProxyGenerator(IContainer container)
        {
            Container = container;
        }

        public void ServerProxy<T>()
            where T : class
        {
            var type = typeof(T);
            if (Container.IsRegistered(type))
                return;
            if (type.IsInterface)
                GenerateServerProxy(type);
            else throw new InvalidOperationException($"Only interfaces can be used as {nameof(ServerProxy)}!");
        }

        private void GenerateServerProxy(Type type)
        {

        }
    }
}
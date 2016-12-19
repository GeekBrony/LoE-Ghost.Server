using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Ghost.Network.Utilities
{
    public static class ReflectionExt
    {
        public static class NetMessageChache
        {


            static NetMessageChache()
            {

            }

        }

        public static class RpcServerProxyChache
        {
            private static readonly Type[] s_targets = new[] 
            {
                typeof(PNetR.Player),//public void Send(NetMessage message, RpcMode mode)
                typeof(PNetR.Server),//public void Send(NetMessage message, RpcMode mode)
                typeof(PNetR.NetworkView),//public void Send(NetMessage message, RpcMode mode)

                typeof(PNetS.Room),//public void Send(NetMessage message, RpcMode mode)
                typeof(PNetS.Player),//public void Send(NetMessage message, RpcMode mode)
                typeof(PNetS.Server)
            };

            static RpcServerProxyChache()
            {


            }
        }
    }
}
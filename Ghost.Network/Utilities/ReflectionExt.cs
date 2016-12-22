using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ghost.Network.Utilities
{
    public static class ReflectionExt
    {
        public static class NetMessageChache
        {
            private const string ReadName = "Read";
            private const string WriteName = "Write";

            private static readonly Type NetSerializable = typeof(INetSerializable);
            private static readonly Type[] SupportedTypes = new[]
            {
                typeof(IList<>),
                typeof(ICollection<>),
                typeof(IEnumerable<>),
                typeof(IDictionary<,>),
            };

            private static readonly Dictionary<Type, MethodInfo> s_read_chache;
            private static readonly Dictionary<Type, MethodInfo> s_write_chache;
            //private static readonly Dictionary<Type, MethodInfo> s_try_read_chache;
            //private static readonly Dictionary<Type, MethodInfo> s_var_read_chache;
            //private static readonly Dictionary<Type, MethodInfo> s_var_write_chache;
            //private static readonly Dictionary<Type, MethodInfo> s_rang_read_chache;
            //private static readonly Dictionary<Type, MethodInfo> s_rang_write_chache;

            static NetMessageChache()
            {
                var methods = typeof(INetBuffer).GetMethods();
                s_read_chache = methods.Where(x => 
                {
                    return x.Name.StartsWith(ReadName) && x.ReturnType.Name == x.Name.Substring(ReadName.Length) && x.GetParameters().Length == 0;
                })
                .ToDictionary(x => x.ReturnType, x => x);
                s_write_chache = methods.Select(x => new
                {
                    x.Name,
                    Method = x,
                    x.ReturnType,
                    Params = x.GetParameters()
                })
                .Where(x => x.Name.StartsWith(WriteName) && x.ReturnType == typeof(void) && x.Params.Length == 1)
                .ToDictionary(x => x.Params[0].ParameterType, x => x.Method);
            }

            public static MethodInfo GetDeserializer<T>() => GetDeserializer(typeof(T));

            public static MethodInfo GetDeserializer(Type type)
            {
                MethodInfo method;
                if (s_read_chache.TryGetValue(type, out method))
                    return method;
                return null;
            }

            public static bool CanDeserialize<T>() => CanDeserialize(typeof(T));

            public static bool CanDeserialize(Type type)
            {
                if (!s_read_chache.ContainsKey(type))
                {
                    if (NetSerializable.IsAssignableFrom(type))
                        return true;
                    if (type.IsArray)
                        return CanDeserialize(type.GetElementType());
                    if (type.IsEnum)
                        return CanDeserialize(Enum.GetUnderlyingType(type));
                    if (type.IsGenericType)
                    {
                        var genericTypeDef = type.GetGenericTypeDefinition();
                        if (genericTypeDef == typeof(Nullable<>))
                            return CanDeserialize(Nullable.GetUnderlyingType(type));
                        if (genericTypeDef.GetInterfaces().Where(x=> x.IsGenericType).Any(x => SupportedTypes.Contains(x.GetGenericTypeDefinition())))
                            return !type.GetGenericArguments().Any(x => !CanDeserialize(x));    
                    }
                    return false;
                }
                return true;
            }
        }

        public static class RpcServerProxyChache
        {
            private static readonly Type[] s_targets = new[]
            {
                //typeof(PNetR.Player),//public void Send(NetMessage message, RpcMode mode)
                //typeof(PNetR.Server),//public void Send(NetMessage message, RpcMode mode)
                //typeof(PNetR.NetworkView),//public void Send(NetMessage message, RpcMode mode)

                //typeof(PNetS.Room),//public void Send(NetMessage message, RpcMode mode)
                //typeof(PNetS.Player),//public void Send(NetMessage message, RpcMode mode)
                //typeof(PNetS.Server)
            };

            static RpcServerProxyChache()
            {


            }
        }
    }
}
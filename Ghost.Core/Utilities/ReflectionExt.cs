using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ghost.Core.Utilities
{
    public static class ReflectionExt
    {
        public static class CommandArgsChache
        {
            private static readonly Dictionary<Type, MethodInfo> s_chache;

            static CommandArgsChache()
            {
                s_chache = typeof(CommandArgs).GetMethods()
                    .Select(x => new { Method = x, x.Name, x.ReturnType, Params = x.GetParameters() })
                    .Where(x => x.Name == "TryGet" && x.ReturnType == typeof(bool) && x.Params.Length == 1 && x.Params[0].IsOut)
                    .ToDictionary(x => x.Params[0].ParameterType.GetElementType(), x => x.Method);
            }

            public static bool IsSupportedType<T>() => s_chache.ContainsKey(typeof(T));

            public static bool IsSupportedType(Type type) => s_chache.ContainsKey(type);

            public static bool TryGetDeserializer<T>(out MethodInfo method) => s_chache.TryGetValue(typeof(T), out method);

            public static bool TryGetDeserializer(Type type, out MethodInfo method) => s_chache.TryGetValue(type, out method);
        }
    }
}
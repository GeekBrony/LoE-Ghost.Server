using DryIoc;
using FluentNHibernate;
using Ghost.Data.Entities.Mappings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ghost.Data.Utilities
{
    public static class ReflectionExt
    {
        public static class Repository
        {
            private static readonly Type[] s_chache;

            static Repository()
            {
                s_chache = typeof(Repository).Assembly.GetTypes()
                    .Where(x => x.Name.EndsWith("Map") && (x.BaseType?.IsGenericType ?? false))
                    .Where(x => x.BaseType.GetGenericTypeDefinition() == typeof(BaseEntityMap<>))
                    .Select(x => x.BaseType.GetGenericArguments()[0]).ToArray();
            }
        }
    }
}
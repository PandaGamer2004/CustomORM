using System;
using System.Collections;
using System.Linq;

namespace CustomORM.Extensions
{
    public static class TypeExtensions
    {
        public static bool IsEntityType(this Type entityType)
        {
            
            return !entityType.IsValueType && entityType.GetConstructors()
                .Any(info => info.GetParameters().Length == 0);
        }


        public static Type GetRealTypeFromNavigationalPropertyType(this Type navPropertyType)
        {
            if (typeof(IEnumerable).IsAssignableFrom(navPropertyType) 
                && navPropertyType.IsGenericType && navPropertyType.GetGenericArguments().Length == 1
                && navPropertyType.GetGenericArguments()[0].IsValueType)
            {
                return navPropertyType.GetGenericArguments()[0];
            }

            return navPropertyType;
        }
    }
}
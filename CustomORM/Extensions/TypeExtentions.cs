using System;
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
    }
}
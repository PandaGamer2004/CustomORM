using System;
using System.Collections.Generic;
using System.Linq;

namespace CustomORM.Helpers
{
    public static class EnumHelpers
    {
        public static Dictionary<String, TEnum> GetDictionaryOfEnumNamesAndValues<TEnum>()
            where TEnum : Enum
        {
            return typeof(TEnum).GetEnumNames().ToDictionary(item => item,
                item => (TEnum) Enum.Parse(typeof(TEnum),item), StringComparer.OrdinalIgnoreCase);
        }
    }
}
using System;

namespace CustomORM.Extensions
{
    public static class ArrayExtensions
    {
        public static Boolean ExactlyEqual(this Array arr1, Array arr2)
        {
            if (arr1.Length != arr2.Length) return false;

            for (var iterator = 0; iterator < arr1.Length; iterator++)
            {
                if (arr1.GetValue(iterator) != arr2.GetValue(iterator)) return false;
            }

            return true;
        }
        
    }
}
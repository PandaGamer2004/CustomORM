
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace CustomORM.Interfaces
{
    public interface IEntityEqualityComparer<T> where T : class, new()
    {
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="trackedEntity"></param>
        /// <param name="copiedEntity"></param>
        /// <returns>Returns IEnumerable that contains not equal properties</returns>
        public bool CheckEntityPropertiesEqual(T trackedEntity, T copiedEntity);

        public IEnumerable<PropertyInfo> GetNotEqualNavigationalProperties(T trackedEntity, T copiedEntity);
    }
}
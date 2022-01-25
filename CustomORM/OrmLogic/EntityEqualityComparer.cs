using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CustomORM.Extensions;
using CustomORM.Interfaces;

namespace CustomORM.OrmLogic
{
    public class EntityEqualityComparer<T> : IEntityEqualityComparer<T> where T : class, new()
    {
        private readonly EntityInfo _entityInfo = EntityInfoCollector.Instance.GetEntityInfoForType(typeof(T));

        private Boolean CheckPropertyValuesEqual(Object val1, Object val2)
            => val1.Equals(val2);

        private Boolean CheckIsArray(PropertyInfo property)
            => property.PropertyType.IsArray;

        private Boolean CheckArraysAreEqual(Object value1, Object value2)
        {
            var arrayTrackedValue = (Array) value1!;
            var arrayCopiedValue = (Array) value2!;
            return arrayTrackedValue.ExactlyEqual(arrayCopiedValue);
        }

        private Boolean CheckIsEnumerable(PropertyInfo prop)
            => typeof(IEnumerable).IsAssignableFrom(prop.PropertyType);

        private Boolean CheckOneNullAndOtherNot(Object? value1, Object? value2)
            => value1 is null && value2 is not null
               || value2 is null && value1 is not null;

        private Boolean CheckBothNull(Object? value1, Object? value2)
            => value1 is null && value2 is null;

        public IEnumerable<PropertyInfo> GetNotEqualNavigationalProperties(T trackedEntity, T copiedEntity)
        {
            var navigationalProperties = _entityInfo.NavigationalProperties;

            return navigationalProperties.Where(prop =>
            {
                var trackedEntityValue = _entityInfo.GetPropertyValueForEntity(prop, trackedEntity);
                var copiedEntityValue = _entityInfo.GetPropertyValueForEntity(prop, copiedEntity);

                return !CheckIsEnumerable(prop)
                       && !CheckBothNull(trackedEntityValue, copiedEntityValue)
                       && (CheckOneNullAndOtherNot(trackedEntityValue, copiedEntityValue)
                       || !CheckPropertyValuesEqual(trackedEntityValue!, copiedEntityValue!));
            });
        }

        public bool CheckEntityPropertiesEqual(T trackedEntity, T copiedEntity)
        {
            var allEntityProperties = _entityInfo.EntityProperties;
            return allEntityProperties.All(property =>
            {
                var trackedEntityValue = _entityInfo.GetPropertyValueForEntity(property, trackedEntity);
                var copiedEntityValue = _entityInfo.GetPropertyValueForEntity(property, copiedEntity);
                return CheckBothNull(trackedEntityValue, copiedEntityValue)
                       || !CheckOneNullAndOtherNot(trackedEntityValue, copiedEntityValue)
                       && (CheckIsArray(property) && CheckArraysAreEqual(trackedEntityValue, copiedEntityValue)
                           || CheckPropertyValuesEqual(trackedEntityValue, copiedEntityValue));
            });
        }
    }
}
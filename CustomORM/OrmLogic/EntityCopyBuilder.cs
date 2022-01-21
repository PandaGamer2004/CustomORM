using System;
using System.Reflection;
using CustomORM.Interfaces;

namespace CustomORM.OrmLogic
{
    
    public class EntityCopyBuilder<T> : IEntityCopyBuilder<T> where T : class, new()
    {
        private EntityInfo _entityInfo = EntityInfoCollector.Instance.GetEntityInfoForType(typeof(T));

        public T CopyEntity(T entity)
        {
            var copiedEntity = Activator.CreateInstance<T>();
            var entityProperties = _entityInfo.EntityProperties;
            
            foreach (var entityProperty in entityProperties)
            {
                if (entityProperty.PropertyType.IsArray)
                {
                    var arrayCopy = GetArrayCopy(entity, entityProperty);
                    if (arrayCopy is not null)
                    {
                        _entityInfo.SetValueForProperty(entityProperty, arrayCopy, copiedEntity);
                    }
                }
                else
                {
                    var valueToSet = _entityInfo.GetPropertyValueForEntity(entityProperty, entity);
                    _entityInfo.SetValueForProperty(entityProperty, valueToSet, copiedEntity);
                }
            }

            return copiedEntity;
        }

        private Array? GetArrayCopy(T entity, PropertyInfo entityProperty)
        {
            var arrayPropertyValue = (Array?) _entityInfo.GetPropertyValueForEntity(entityProperty, entity);
            Array? destinationArray = null;
            if (arrayPropertyValue != null)
            {
                var elementTypeOfSourceArray = arrayPropertyValue.GetType().GetElementType();
                destinationArray = Array.CreateInstance(elementTypeOfSourceArray!, arrayPropertyValue.Length);
                Array.Copy(arrayPropertyValue, destinationArray, arrayPropertyValue.Length);
            }

            return destinationArray;
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CustomORM.Exceptions;
using CustomORM.Interfaces;

namespace CustomORM.OrmLogic
{
    public class EntityIncludingProvider<T> : IEntityIncludingProvider<T> where T : class, new()
    {
        private readonly Dictionary<Type, List<Object>> _entitiesToInclude = new();
        private readonly EntityInfoCollector _entityInfoCollector = EntityInfoCollector.Instance;
        private readonly EntityInfo _currentEntityInfo;

        public EntityIncludingProvider()
        {
            _currentEntityInfo = _entityInfoCollector.GetEntityInfoForType(typeof(T));
        }
        

        public void AddIncludedEntities(List<EntityTrackingItem<T>>
            trackingItemsForInclude)
        {
            if(trackingItemsForInclude.Count == 0) return;
            var navigationalProperties = _currentEntityInfo.NavigationalProperties.ToList();
            
            navigationalProperties.ForEach(navigationalProperty =>
            {
                ChooseIncludingPlan(trackingItemsForInclude, navigationalProperty);
            });
            
        }

        private void ChooseIncludingPlan(List<EntityTrackingItem<T>> trackingItemsForInclude, PropertyInfo navigationalProperty)
        {
            var navigationalPropertyType = navigationalProperty.PropertyType;
            
            if (_entitiesToInclude.ContainsKey(navigationalPropertyType))
            {
                IncludeToTrackedEntitiesNavigationalPropertyRelationTypeOne(navigationalProperty,
                    _entitiesToInclude[navigationalProperty.PropertyType], trackingItemsForInclude);
            }
            else if (typeof(IEnumerable).IsAssignableFrom(navigationalPropertyType)
                    && navigationalPropertyType.IsGenericType
                    && _entitiesToInclude.ContainsKey(navigationalPropertyType.GetGenericArguments()[0]))
            {
                var entitiesToIncludeWithGivenNavigationPropertyType =
                    _entitiesToInclude[navigationalPropertyType.GetGenericArguments()[0]];
                IncludeToTrackedEntitiesNavigationalPropertyRelationTypeMany(entitiesToIncludeWithGivenNavigationPropertyType,
                    navigationalProperty
                    ,trackingItemsForInclude);
            }
        }

        public void RegisterEntitiesForInclude(IEnumerable<Object> entitiesToInclude)
        {
            var entitiesToIncludeList = entitiesToInclude.ToList();
            if(!entitiesToIncludeList.Any()) return;

            var entitiesType = entitiesToIncludeList[0].GetType();
            try
            {
                _entitiesToInclude.Add(entitiesType, entitiesToIncludeList);
            }
            catch (ArgumentException)
            {
                throw new EntitiesAlreadyIncludedException();
            }
        }

        private void IncludeToTrackedEntitiesNavigationalPropertyRelationTypeMany(List<object> entitiesToIncludeList,
            PropertyInfo navigationalProperty, List<EntityTrackingItem<T>> trackingItemsForInclude)
        {
                var entitiesToIncludeType = navigationalProperty.PropertyType.GetGenericArguments()[0];
                var entityToIncludeInfo = _entityInfoCollector.GetEntityInfoForType(entitiesToIncludeType);

            var navigationalPropertyForForeignKey =
                entityToIncludeInfo.NavigationalProperties.First(info => info.PropertyType == typeof(T));

            var foreignKeyPropertyInfo =
                entityToIncludeInfo.GetForeignKeyForNavigationProperty(navigationalPropertyForForeignKey);

            foreach (var trackedEntity in trackingItemsForInclude)
            {
                var entitiesToSetNavigationalPropertyForTrackedEntity
                    = GetEntitiesToIncludeForTracked(entitiesToIncludeList,
                        trackedEntity,
                        entityToIncludeInfo,
                        foreignKeyPropertyInfo);

                var navigationalPropsToSetIncludedValue =
                    GetNavigationalPropertyToIncludeValue(entitiesToIncludeType,
                        navigationalProperty, trackedEntity);

                var propsToSetIncludedValue = (IList) navigationalPropsToSetIncludedValue!;

                entitiesToSetNavigationalPropertyForTrackedEntity.ForEach(entityToAdd
                    => propsToSetIncludedValue.Add(entityToAdd));
            }
        }

        private object GetNavigationalPropertyToIncludeValue(Type entitiesToIncludeType,
            PropertyInfo navigationalPropertyForEntityToInclude, EntityTrackingItem<T> trackingItem)
        {
            var navigationalPropsToSetIncludedValue =
                _currentEntityInfo.GetPropertyValueForEntity(navigationalPropertyForEntityToInclude,
                    trackingItem.TrackedEntity);
            if (navigationalPropsToSetIncludedValue is null)
            {
                var listType = typeof(List<>);
                var listOfEntitiesToIncludeTypeGeneric = listType.MakeGenericType(entitiesToIncludeType);
                var listOfEntitiesToInclude = Activator.CreateInstance(listOfEntitiesToIncludeTypeGeneric);


                _currentEntityInfo.SetValueForProperty(navigationalPropertyForEntityToInclude,
                    listOfEntitiesToInclude
                    , trackingItem.TrackedEntity);

                navigationalPropsToSetIncludedValue = listOfEntitiesToInclude;
            }

            return navigationalPropsToSetIncludedValue!;
        }

        private List<object> GetEntitiesToIncludeForTracked(List<object> entitiesToIncludeList,
            EntityTrackingItem<T> trackedEntityItem,
            EntityInfo entityToIncludeInfo, PropertyInfo foreignKeyPropertyInfo)
        {
            var trackedEntityPkValue = _currentEntityInfo.GetPropertyValueForEntity(_currentEntityInfo.PrimaryKey,
                trackedEntityItem.TrackedEntity);

            var entitiesToSetNavigationalPropertyForTrackedEntity
                = entitiesToIncludeList.FindAll(entityToInclude =>
                {
                    var foreignKeyValue = entityToIncludeInfo
                        .GetPropertyValueForEntity(foreignKeyPropertyInfo, entityToInclude);

                    return foreignKeyValue.Equals(trackedEntityPkValue);
                });
            return entitiesToSetNavigationalPropertyForTrackedEntity;
        }

        private void IncludeToTrackedEntitiesNavigationalPropertyRelationTypeOne(PropertyInfo navigationalProp,
            List<object> entitiesToIncludeList,
            List<EntityTrackingItem<T>> trackingItemsForInclude)
        {
            
            var entityToIncludeInfo = _entityInfoCollector.GetEntityInfoForType(navigationalProp.PropertyType);

            var foreignKeyProperty = _currentEntityInfo.GetForeignKeyForNavigationProperty(navigationalProp);

            foreach (var trackingItemToInclude in trackingItemsForInclude)
            {
                var matchedEntityToInclude = entitiesToIncludeList.Find(entity =>
                {
                    var trackedEntity = trackingItemToInclude.TrackedEntity;
                    var foreignKeyValue =
                        _currentEntityInfo.GetPropertyValueForEntity(foreignKeyProperty, trackedEntity);
                    var primaryKeyValue = entityToIncludeInfo.GetPropertyValueForEntity(
                        entityToIncludeInfo.PrimaryKey, entity);

                    return foreignKeyValue?.Equals(primaryKeyValue) ?? false;
                });

                if(matchedEntityToInclude is null) continue;
                
                _currentEntityInfo
                    .SetValueForProperty(navigationalProp, matchedEntityToInclude,trackingItemToInclude.TrackedEntity);
            }
        }
    }
}
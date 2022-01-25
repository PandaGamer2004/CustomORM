using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CustomORM.Exceptions;
using CustomORM.Interfaces;

namespace CustomORM.OrmLogic
{
    public class EntityTrackingItem<T> where T : class, new()
    {
        private readonly IEntityEqualityComparer<T> _comparer;
        private readonly T _entityThatTracked;
        private readonly T _entityOnTrackedCopy;
        private EntityState _entityState;
        private readonly EntityInfoCollector _entityInfoCollector = EntityInfoCollector.Instance;
        private readonly EntityInfo _entityInfo;

        public EntityTrackingItem(T entityThatTracked, EntityState entityState, IEntityEqualityComparer<T> comparer,
            IEntityCopyBuilder<T> copyBuilder)
        {
            _entityThatTracked = entityThatTracked;
            _entityOnTrackedCopy = copyBuilder.CopyEntity(_entityThatTracked);
            _entityState = entityState;
            _comparer = comparer;
            _entityInfo = _entityInfoCollector.GetEntityInfoForType(typeof(T));
        }


        public EntityState State => _entityState;
        
        public T TrackedEntity => _entityThatTracked;
        public Boolean IsTrackedEntityEqual(T entity)
            => ReferenceEquals(_entityThatTracked, entity);

        public void CheckChangeForEntityState()
        {
            if (_entityState == EntityState.Deleted) return;
            var notEqualNavigationalProperties =
                _comparer.GetNotEqualNavigationalProperties(_entityThatTracked, _entityOnTrackedCopy);

            var notEqualNavigationalPropertiesList = notEqualNavigationalProperties.ToList();
            ChangeEntityForeignKeyForNotEqualNavProps(notEqualNavigationalPropertiesList);

            var equalEntityProps = _comparer.CheckEntityPropertiesEqual(_entityThatTracked, _entityOnTrackedCopy);
            if (notEqualNavigationalPropertiesList.Count != 0 
                || !_comparer.CheckEntityPropertiesEqual(_entityThatTracked, _entityOnTrackedCopy))
            {
                SetChangedState();
            }
        }

        private void ChangeEntityForeignKeyForNotEqualNavProps(List<PropertyInfo> navigationalProperties)
        {
            foreach (var navigationalProperty in navigationalProperties)
            {
                var navPropertyValue =
                    _entityInfo.GetPropertyValueForEntity(navigationalProperty, _entityThatTracked);
                var foreignKeyForNavProperty = _entityInfo.GetForeignKeyForNavigationProperty(navigationalProperty);
                var foreignKeyType = foreignKeyForNavProperty.PropertyType;
                if (navPropertyValue is null)
                {
                    Object? valueForForeignKey = null;
                    if (foreignKeyType.IsValueType && Nullable.GetUnderlyingType(foreignKeyType) is null)
                    {
                        throw new DbIntegrityException("Can't set to foreign key null value for not nullable type");
                    }

                    _entityInfo.SetValueForProperty(foreignKeyForNavProperty, valueForForeignKey,
                        _entityThatTracked);
                }
                else
                {
                    var navPropertyEntityInfo =
                        _entityInfoCollector.GetEntityInfoForType(navigationalProperty.PropertyType);
                    var navEntityPrimaryKey = navPropertyEntityInfo.PrimaryKey;
                    var primaryKeyValue =
                        navPropertyEntityInfo.GetPropertyValueForEntity(navEntityPrimaryKey, navPropertyValue);

                    _entityInfo.SetValueForProperty(foreignKeyForNavProperty, primaryKeyValue, _entityThatTracked);
                }
            }
        }


        private void SetChangedState()
        {
            if (_entityState == EntityState.Tracked)
            {
                _entityState = EntityState.Changed;
            }
        }

        public void MakeStateDeleted()
        {
            _entityState = EntityState.Deleted;
        }
    }
}
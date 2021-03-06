using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using CustomORM.Attributes;
using CustomORM.Exceptions;
using CustomORM.Extensions;

namespace CustomORM.OrmLogic
{
    public class EntityInfo
    {
        private readonly EntityTypesMapper _typesMapper = EntityTypesMapper.Instance;
        private readonly Type _entityType;
        private readonly Dictionary<PropertyInfo, IEnumerable<Attribute>> _propertiesAndTheirAttributes
            = new();

        //keyForeignKey value navigationalProperty
        private readonly Dictionary<PropertyInfo, PropertyInfo> _foreignKeyAndTheirNavigationalProperties
            = new();

        private readonly List<PropertyInfo> _allNavigationalPropertyInfos = new();


        private String? _tableName;

        internal EntityInfo(Type entityType)
        {
            if (!entityType.IsEntityType())
            {
                throw new TypeNotContainsDefaultConstructorOrNotRef(nameof(entityType));
            }
            _entityType = entityType;
            this.ConstructEntityInfo();
        }
        

        public string? TableName
        {
            get
            {
                if (_tableName is null)
                {
                    this.CalculateTableName();
                }

                return _tableName;
            }
        }

        
        public IEnumerable<PropertyInfo> NavigationalProperties => _allNavigationalPropertyInfos;
        public IEnumerable<PropertyInfo> EntityProperties => _propertiesAndTheirAttributes.Keys;

        public PropertyInfo PrimaryKey => _propertiesAndTheirAttributes
            .Where(kv => kv.Value.Any(attribute => attribute is PrimaryKeyAttribute))
            .Select(kv => kv.Key).FirstOrDefault()
                                          ?? throw new PrimaryKeyNotFoundException(nameof(_entityType));


        public PropertyInfo? this[String propertyName]
            => _propertiesAndTheirAttributes.Keys.FirstOrDefault(propInfo => propInfo.Name == propertyName)
               ?? _allNavigationalPropertyInfos.FirstOrDefault(propInfo =>
                   propInfo.Name == propertyName);

        private void AddAllPropertyInfos(TypeInfo typeInfo)
        {
            foreach (var propertyInfo in typeInfo.DeclaredProperties)
            {

              
                var listOfFilteredAttributes = propertyInfo
                    .GetCustomAttributes()
                    .Where(attribute => attribute is DataAnnotationAttribute)
                    .ToList();
                
                if (_propertiesAndTheirAttributes.ContainsKey(propertyInfo))
                {
                   _propertiesAndTheirAttributes[propertyInfo] = 
                       _propertiesAndTheirAttributes[propertyInfo].Union(listOfFilteredAttributes);
                }

                _propertiesAndTheirAttributes.Add(propertyInfo, listOfFilteredAttributes);
            }
        }

        private void ConstructEntityInfo()
        {
            var typeInfo = _entityType.GetTypeInfo();
            AddAllPropertyInfos(typeInfo);
            ConstructNavigationalPropertyInfos();
            RemoveNavigationalPropertiesFromAll();
        }

        private void RemoveNavigationalPropertiesFromAll()
        {
            foreach (var propertyInfo in _allNavigationalPropertyInfos)
            {
                _propertiesAndTheirAttributes.Remove(propertyInfo);
            }
        }


        private void ConstructNavigationalPropertyInfos()
        {
            foreach (var (property, attrList) in _propertiesAndTheirAttributes)
            {
                PropertyInfo? navigationalProperty = null;
                var foreignKey = (ForeignKeyAttribute?) attrList.FirstOrDefault(attr => attr is ForeignKeyAttribute);
                if (foreignKey is not null)
                {
                    navigationalProperty = _propertiesAndTheirAttributes.Keys.FirstOrDefault(propertyInfo =>
                        propertyInfo.Name == foreignKey.PropertyName);

                    _foreignKeyAndTheirNavigationalProperties[property] = navigationalProperty ??
                                                                          throw new NotFoundPropertyForForeignKeyException();
                }
                else if(typeof(IEnumerable).IsAssignableFrom(property.PropertyType)
                        && property.PropertyType.IsGenericType
                        && !property.PropertyType.GetGenericArguments()[0].IsValueType)
                {
                    navigationalProperty = property;
                }

                if (navigationalProperty is not null)
                {
                    _allNavigationalPropertyInfos.Add(navigationalProperty);
                }
            }
        }

        private void CalculateTableName()
        {
            var tableNameAttribute = (TableNameAttribute?) _entityType
                .GetTypeInfo()
                .GetCustomAttributes()
                .FirstOrDefault(attr => attr is TableNameAttribute);

            _tableName = tableNameAttribute?.TableName ?? _entityType.Name;
        }
        
        

        private bool CheckPropertyRelatedWithEntity(PropertyInfo propertyInfo)
        {
            return _propertiesAndTheirAttributes.ContainsKey(propertyInfo) ||
                   _allNavigationalPropertyInfos.Contains(propertyInfo);
        }

        private bool CheckPropertyIsNotNavigationalProperty(PropertyInfo propertyInfo)
        {
            return _propertiesAndTheirAttributes.ContainsKey(propertyInfo);
        }

        public String GetDbColumnNameFromPropertyInfo(PropertyInfo pr)
        {
            if (CheckPropertyRelatedWithEntity(pr) & CheckPropertyIsNotNavigationalProperty(pr))
            {
                return ((DbColumnNameAttribute?)_propertiesAndTheirAttributes[pr]
                    .FirstOrDefault(atr => atr is DbColumnNameAttribute))?.ColumnName ?? pr.Name;
            }
            
            throw new NotRelatedPropertyInfoToEntityException();
        }

        public IEnumerable<String> GetDbColumnNamesFromPropertyInfos(IEnumerable<PropertyInfo> propertyInfos)
        {
            return propertyInfos.Select(GetDbColumnNameFromPropertyInfo);
        }

        public Object GetDbColumnValueForEntity(PropertyInfo pr, Object? target)
        {
            var resValue = GetPropertyValueForEntity(pr, target);
            if (resValue is null)
            {
                return DBNull.Value;
            }

            return resValue;
        }
        public Object? GetPropertyValueForEntity(PropertyInfo? pr, object? target)
        {
            if (pr == null)
            {
                throw new ArgumentNullException(nameof(pr));
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (CheckPropertyRelatedWithEntity(pr))
            {
                var resValue = pr.GetValue(target);
                return resValue;
            }

            throw new NotRelatedPropertyInfoToEntityException();

        }

        public IEnumerable<Object?> GetDbColumnValuesForEntities(IEnumerable<PropertyInfo> propertyInfos, Object? target)
        {
            return propertyInfos.Select(prop => GetDbColumnValueForEntity(prop, target));
        }

        public void SetValueForProperty(PropertyInfo entityProperty, object? value, object? target)
        {
            if (entityProperty is null)
            {
                throw new ArgumentNullException(nameof(entityProperty));
            }

            if (target is null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (!CheckPropertyRelatedWithEntity(entityProperty)) 
                throw new NotRelatedPropertyInfoToEntityException();
                
            entityProperty.SetValue(target, value);
            
        }

        
        public PropertyInfo GetForeignKeyForNavigationProperty(PropertyInfo navigationalProperty)
        {
            try
            {
                var foundPair = _foreignKeyAndTheirNavigationalProperties
                    .First(kv => kv.Value == navigationalProperty);

                var foreignKey = foundPair.Key;

                return foreignKey;
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine(ex.Message);

                throw new ForeignKeyNotFoundException();
            }
        }

        public SqlDbType GetDbTypeForGivenProperty(PropertyInfo propertyInfo)
        {
            if (propertyInfo is null)
            {
                throw new ArgumentNullException(nameof(propertyInfo));
            }

            var propertyType = propertyInfo.PropertyType;
            
            if (CheckPropertyRelatedWithEntity(propertyInfo) && CheckPropertyIsNotNavigationalProperty(propertyInfo))
            {
                var propertyAttrs = _propertiesAndTheirAttributes[propertyInfo];
                var columnTypeAttribute = 
                    (ColumnTypeAttribute?)propertyAttrs.FirstOrDefault(attribute => attribute is ColumnTypeAttribute);
                if (columnTypeAttribute is not null)
                {
                    return _typesMapper.GetDbTypeFromString(columnTypeAttribute.DbTypeNameAttribute);
                }

                return _typesMapper.GetDbTypeFromFrameworkType(propertyType);
            }

            throw new NotRelatedPropertyInfoToEntityException();
        }
        
        
        
    }

}

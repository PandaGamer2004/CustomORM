using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using CustomORM.Exceptions;
using CustomORM.Interfaces;

namespace CustomORM.OrmLogic
{
    public class ModelSerializer<T> : IModelSerializer<T> where T: class, new()
    {
        private readonly EntityInfo _entityInfo = EntityInfoCollector.Instance.GetEntityInfoForType(typeof(T));
        public T SerializeRowToEntity(SqlDataReader reader)
        {
            СheckMatchedEntityToReader(reader);

            var entityToGetFromReader = Activator.CreateInstance<T>();
            var entityProperties = _entityInfo.EntityProperties;

            try
            {
                foreach (var entityProperty in entityProperties)
                {
                    var dbColumnName = _entityInfo.GetDbColumnNameFromPropertyInfo(entityProperty);
                    var valueFromReader = reader[dbColumnName];
                    _entityInfo.SetValueForProperty(entityProperty, valueFromReader, entityToGetFromReader);
                }
            }
            catch (IndexOutOfRangeException ex)
            {
                Debug.WriteLine(ex.Message);
                throw new EntitySerializationException("Not matched structure of entity and reader");
            }

            return entityToGetFromReader;
        }

        private void СheckMatchedEntityToReader(SqlDataReader reader)
        {
            var fieldsToSet = reader.FieldCount;
            if (_entityInfo.EntityProperties.Count() != fieldsToSet)
            {
                throw new EntitySerializationException("Can't serialize entity with not matched to reader columns count");
            }
        }
    }
    
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using CustomORM.EventArgsObjects;
using CustomORM.Exceptions;
using CustomORM.Extensions;
using CustomORM.Interfaces;

namespace CustomORM.OrmLogic
{
    public class DbEntitySet<T> : IEntitySet<T>
        where T : class, new()
    {
        public DbEntitySet(SqlCommand command)
        {
            _selectCommand = command;
            _entityInfo = _entityInfoCollector.GetEntityInfoForType(typeof(T));
        }
        
        private SqlCommand _selectCommand;
        private SqlCommand? _transactionCommand;
        private readonly ICommandBuilder<T> _commandBuilder = new SimpleCommandBuilder<T>();
        private readonly IModelSerializer _modelSerializer = new ModelSerializer(typeof(T));
        private IEntityStateTracker<T> _entityStateTracker = new EntityStateTracker<T>(
            new EntityCopyBuilder<T>(), new EntityEqualityComparer<T>());

        private readonly EntityInfoCollector _entityInfoCollector = EntityInfoCollector.Instance;
        private readonly EntityInfo _entityInfo;
        

        private void MakeAndExecuteQueryForEachEntity(IEnumerable<T> entities, Func<T, QueryEntity> queryBuilder)
        {
            if (_transactionCommand is null)
            {
                throw new CanNotMakeDbUpdatesWhenCurrentTransactionNullException();
            }
            
            foreach (var entityToInsert in entities)
            {
                var queryEntityForInsert = queryBuilder(entityToInsert);
                _transactionCommand.CommandText = queryEntityForInsert.QueryText;
                _transactionCommand.AddParamsList(queryEntityForInsert.CommandParams);
                _transactionCommand.ExecuteNonQuery();
                
            }
        }

        private void ChangeEntitiesInDb()
        {
            _entityStateTracker.CheckTrackedEntitiesToChangeState();

            var entitiesToInsert = _entityStateTracker.GetEntitiesToAdd();
            var entitiesToUpdate = _entityStateTracker.GetEntitiesToUpdate();
            var entitiesToDelete = _entityStateTracker.GetEntitiesToDelete();
            
            MakeAndExecuteQueryForEachEntity(entitiesToInsert, _commandBuilder.GenerateInsertCommand);
            MakeAndExecuteQueryForEachEntity(entitiesToUpdate, _commandBuilder.GenerateUpdateCommand);
            MakeAndExecuteQueryForEachEntity(entitiesToDelete, _commandBuilder.GenerateDeleteCommand);
        }

        public IEnumerator<T> GetEnumerator()
        {
            var selectionQueryEntity = _commandBuilder.GenerateSelectCommand();
            _selectCommand.CommandText = selectionQueryEntity.QueryText;
            _selectCommand.AddParamsList(selectionQueryEntity.CommandParams);
            
            var reader = _selectCommand.ExecuteReader();

            return new DbEntitySetEnumerator<T>(reader, _modelSerializer, _entityStateTracker);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void AddEntity(T entity)
        {
            if (entity is null) throw new ArgumentNullException(nameof(entity));
            _entityStateTracker.RegisterEntityToAdd(entity);
        }

        public void RemoveEntity(T entityToRemove)
        {
            if (entityToRemove is null) throw new ArgumentNullException(nameof(entityToRemove));
            _entityStateTracker.RegisterEntityToDelete(entityToRemove);
        }

        public void RemoveEntitiesRange(params T[] entitiesToRemove)
        {
            if (entitiesToRemove is null) throw new ArgumentNullException(nameof(entitiesToRemove));
            _entityStateTracker.RegisterEntitiesToDelete(entitiesToRemove);
        }

        public void AddRangeEntities(params T[] entitiesToAdd)
        {
            if (entitiesToAdd is null) throw new ArgumentNullException(nameof(entitiesToAdd));
            _entityStateTracker.RegisterEntitiesToAdd(entitiesToAdd);
        }
        

        public void Include(string nameOfIncludeProperty)
        {
            var propertyToInclude = _entityInfo[nameOfIncludeProperty];

            if (propertyToInclude is null || !_entityInfo
                .NavigationalProperties.Contains(propertyToInclude))
            {
                throw new NavigationalPropertyNotFoundException(nameOfIncludeProperty);
            }
            
            var includeQueryEntity = _commandBuilder.GenerateNavigationalPropertyIncludeQuery(propertyToInclude);
            _selectCommand.CommandText = includeQueryEntity.QueryText;
            _selectCommand.AddParamsList(includeQueryEntity.CommandParams);
            
            List<Object> entitiesToInclude = new();

            var entityToIncludeType = propertyToInclude.PropertyType.GetRealTypeFromNavigationalPropertyType();
            var includeEntityModelSerializer = new ModelSerializer(entityToIncludeType);
                
            using var reader = _selectCommand.ExecuteReader();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    var serializedEntity = includeEntityModelSerializer.SerializeRowToEntity(reader);
                    entitiesToInclude.Add(serializedEntity);                       
                }
                _entityStateTracker.RegisterIncludedEntities(entitiesToInclude);
            }
            
        }

        
        public void SetCurrentCommand(Object sender, CommandEventArgs commandEventArgs)
        {
            _transactionCommand = commandEventArgs.Command;
            this.ChangeEntitiesInDb();
            _entityStateTracker =
                new EntityStateTracker<T>(new EntityCopyBuilder<T>(), new EntityEqualityComparer<T>());
            _transactionCommand = null;
        }
    }
}
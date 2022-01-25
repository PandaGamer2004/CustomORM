using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using CustomORM.EventArgsObjects;
using CustomORM.Exceptions;
using CustomORM.Interfaces;

namespace CustomORM.OrmLogic
{
    public class DbEntitySet<T> : IEntitySet<T>
        where T : class, new()
    {
        public DbEntitySet(SqlCommand command)
        {
            _command = command;
            _entityInfo = _entityInfoCollector.GetEntityInfoForType(typeof(T));
        }
        
        private SqlCommand _command;
        private readonly ICommandBuilder<T> _commandBuilder = new SimpleCommandBuilder<T>();
        private readonly IModelSerializer _modelSerializer = new ModelSerializer(typeof(T));

        private readonly IEntityStateTracker<T> _entityStateTracker = new EntityStateTracker<T>(
            new EntityCopyBuilder<T>(), new EntityEqualityComparer<T>());

        private readonly EntityInfoCollector _entityInfoCollector = EntityInfoCollector.Instance;
        private readonly EntityInfo _entityInfo;
        

        private void MakeAndExecuteQueryForEachEntity(IEnumerable<T> entities, Func<T, QueryEntity> queryBuilder)
        {
            foreach (var entityToInsert in entities)
            {
                var queryEntityForInsert = queryBuilder(entityToInsert);
                _command.CommandText = queryEntityForInsert.QueryText;

                if (queryEntityForInsert.CommandParams is not null)
                {
                    _command.Parameters.AddRange((SqlParameter[]) queryEntityForInsert.CommandParams);
                }

                _command.ExecuteNonQuery();
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
            _command.CommandText = selectionQueryEntity.QueryText;
            
            if (selectionQueryEntity.CommandParams is not null)
            {
                _command.Parameters.AddRange((SqlParameter[]) selectionQueryEntity.CommandParams);
            }
            var reader = _command.ExecuteReader();

            return new DbEntitySetEnumerator<T>(reader, _modelSerializer, _entityStateTracker);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void AddEntity(T entity)
        {
            _entityStateTracker.RegisterEntityToAdd(entity);
        }

        public void RemoveEntity(T entityToRemove)
        {
            _entityStateTracker.RegisterEntityToDelete(entityToRemove);
        }

        public void RemoveEntitiesRange(params T[] entitiesToRemove)
        {
            _entityStateTracker.RegisterEntitiesToDelete(entitiesToRemove);
        }

        public void AddRangeEntities(params T[] entitiesToAdd)
        {
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
            if (includeQueryEntity.CommandParams is not null)
            {
                _command.Parameters.AddRange((SqlParameter[]) includeQueryEntity.CommandParams);
            }
            
            _command.CommandText = includeQueryEntity.QueryText;
            List<Object> entitiesToInclude = new();
            var includeEntityModelSerializer = new ModelSerializer(propertyToInclude.PropertyType);
                
            using var reader = _command.ExecuteReader();
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

        public void Include<T2>(Expression<Func<T, T2>> expression)
        {
            throw new NotImplementedException();
        }


        public void SetCurrentCommand(Object sender, CommandEventArgs commandEventArgs)
        {
            _command = commandEventArgs.Command;
            this.ChangeEntitiesInDb();
        }
    }
}
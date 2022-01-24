using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using CustomORM.EventArgsObjects;
using CustomORM.Interfaces;

namespace CustomORM.OrmLogic
{
    public class DbEntitySet<T> : IEntitySet<T>
        where T : class, new()
    {
        public DbEntitySet(SqlCommand command)
        {
            _command = command;
        }

        private SqlCommand _command;
        private readonly ICommandBuilder<T> _commandBuilder = new SimpleCommandBuilder<T>();
        private readonly IModelSerializer<T> _modelSerializer = new ModelSerializer<T>();

        private readonly IEntityStateTracker<T> _entityStateTracker = new EntityStateTracker<T>(
            new EntityCopyBuilder<T>(), new EntityEqualityComparer<T>());

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
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void AddEntity(T entity)
        {
        }

        public void RemoveEntity(T entityToRemove)
        {
        }

        public void AddRangeEntities(params T[] entitiesToAdd)
        {
        }


        public T Find(int id)
        {
            throw new NotImplementedException();
        }

        public T Find(Guid id)
        {
            throw new NotImplementedException();
        }

        public void Include(string nameOfIncludeProperty)
        {
            throw new NotImplementedException();
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
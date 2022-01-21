using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
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
        } 
        private readonly Queue<String> _commandsToExecute = new();
        private SqlCommand _command;
        private readonly ICommandBuilder<T> _commandBuilder = new SimpleCommandBuilder<T>();
        private readonly IModelSerializer<T> _modelSerializer = new ModelSerializer<T>() ;
        private readonly IEntityStateTracker<T> _entityStateTracker = new EntityStateTracker<T>(
            new EntityCopyBuilder<T>(), new EntityEqualityComparer<T>());
        
        private T GetEntityForFind(String commandText)
        {
            _command.CommandText = commandText;
        
            using var reader = _command.ExecuteReader();
                    
            if (!reader.HasRows)
            {
                throw new EntityNotFoundException();
            }
                    
            reader.Read();
            return _modelSerializer.SerializeRowToEntity(reader);
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
            //TODO WRITE HERE
            var commandText = _commandBuilder.GenerateSelectCommand(id);
            return GetEntityForFind(commandText);
        }

        public T Find(Guid id)
        {
            //TODO WRITE HERE
            var commandText = _commandBuilder.GenerateSelectCommand(id);
            return GetEntityForFind(commandText);
        }

        public void Include(string nameOfIncludeProperty)
        {
            throw new NotImplementedException();
        }

        public void Include<T2>(Expression<Func<T, T2>> expression)
        {
            throw new NotImplementedException();
        }


        private void ExecuteAllCommands()
        {
            while (_commandsToExecute.TryDequeue(out var commandToExecute))
            {
                _command.CommandText = commandToExecute;
                _command.ExecuteNonQuery();
            }
        }

        public void SetCurrentCommand(Object sender, CommandEventArgs commandEventArgs)
        {
            _command = commandEventArgs.Command;
            this.ExecuteAllCommands();
        }
    }
}
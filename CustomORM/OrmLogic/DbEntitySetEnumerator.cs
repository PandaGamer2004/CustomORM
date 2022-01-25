using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using CustomORM.Interfaces;

namespace CustomORM.OrmLogic
{
    public class DbEntitySetEnumerator<T> : IEnumerator<T>
    where T : class, new()
    {

        private IEntityStateTracker<T> _stateTracker;
        private SqlDataReader _reader;
        private readonly IModelSerializer _modelSerializer;
        public DbEntitySetEnumerator(SqlDataReader reader, IModelSerializer modelSerializer, IEntityStateTracker<T> stateTracker)
        {
            _reader = reader;
            _modelSerializer = modelSerializer;
            _stateTracker = stateTracker;
        }
        
        public bool MoveNext()
        {
            if (!_reader.IsClosed && !_reader.Read())
            {
                _reader.Close();
            }
            
            return !_reader.IsClosed;
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public object Current
        {
            get
            {
                var serializedEntity = (T)_modelSerializer.SerializeRowToEntity(_reader);
                _stateTracker.StartTracking(serializedEntity);
                return serializedEntity;
            }
        }

        T IEnumerator<T>.Current => (T) Current;
        
        public void Dispose()
        {
            if(!_reader.IsClosed) _reader.Close();
        }
    }
}
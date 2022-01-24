using System;
using System.Collections;
using System.Collections.Generic;

namespace CustomORM.OrmLogic
{
    public class DbEntitySetEnumerator<T> : IEnumerator<T>
    {
        
        
        public bool MoveNext()
        {
            throw new System.NotImplementedException();
        }

        public void Reset()
        {
            throw new System.NotImplementedException();
        }

        public object? Current { get; }

        T IEnumerator<T>.Current => throw new NotImplementedException();
        
        public void Dispose()
        {
            throw new System.NotImplementedException();
        }
    }
}
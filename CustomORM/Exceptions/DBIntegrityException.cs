using System;

namespace CustomORM.Exceptions
{
    public class DbIntegrityException : Exception
    {
        public DbIntegrityException(String message) : base(message)
        {
            
        }
    }
}
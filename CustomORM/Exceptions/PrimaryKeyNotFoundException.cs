using System;

namespace CustomORM.Exceptions
{
    public class PrimaryKeyNotFoundException : Exception
    {
        public PrimaryKeyNotFoundException(String? message) : base(message)
        {
            
        }
    }
}
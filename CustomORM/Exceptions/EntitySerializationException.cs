using System;

namespace CustomORM.Exceptions
{
    public class EntitySerializationException : Exception
    {

        public EntitySerializationException()
        {
            
        }
        public EntitySerializationException(string? message) : base(message) 
        {
            
        }
    }
}
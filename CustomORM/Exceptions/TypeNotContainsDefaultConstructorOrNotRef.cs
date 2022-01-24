using System;

namespace CustomORM.Exceptions
{
    public class TypeNotContainsDefaultConstructorOrNotRef : Exception
    {

        public TypeNotContainsDefaultConstructorOrNotRef(String message) : base(message)
        {
            
        }
        
    }
}
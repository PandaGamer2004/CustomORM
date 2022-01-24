using System;

namespace CustomORM.Exceptions
{
    public class NavigationalPropertyNotFoundException : Exception
    {
        public NavigationalPropertyNotFoundException(string message) : base(message)
        {
            
        }
    }
}
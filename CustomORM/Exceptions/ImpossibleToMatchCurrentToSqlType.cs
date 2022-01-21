using System;

namespace CustomORM.Exceptions
{
    public class ImpossibleToMatchCurrentToSqlType : Exception
    {
        public ImpossibleToMatchCurrentToSqlType(string frameworkTypeName) : base(frameworkTypeName)
        {
            
        }
    }
}
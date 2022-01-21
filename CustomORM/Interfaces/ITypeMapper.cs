using System;
using System.Data;

namespace CustomORM.Interfaces
{
    public interface ITypeMapper
    {
        public SqlDbType GetDbTypeFromString(String dbTypeName);

        public SqlDbType GetDbTypeFromFrameworkType(Type frameworkType);
    }
}
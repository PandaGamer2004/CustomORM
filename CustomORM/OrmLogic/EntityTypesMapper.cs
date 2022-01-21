using System;
using System.Collections.Generic;
using System.Data;
using CustomORM.Exceptions;
using CustomORM.Helpers;
using CustomORM.Interfaces;

namespace CustomORM.OrmLogic
{
    public class EntityTypesMapper : ITypeMapper
    {
        private Dictionary<String, SqlDbType> _typeNamesAndDbTypes
            = EnumHelpers.GetDictionaryOfEnumNamesAndValues<SqlDbType>();

        private Dictionary<Type, SqlDbType>
            _frameworkToSqlType = new()
            {
                {typeof(Int64), SqlDbType.BigInt},
                {typeof(Byte[]), SqlDbType.VarBinary},
                {typeof(Boolean), SqlDbType.Bit},
                {typeof(String), SqlDbType.NVarChar},
                {typeof(DateTime), SqlDbType.DateTime},
                {typeof(DateTimeOffset), SqlDbType.DateTimeOffset},
                {typeof(Double), SqlDbType.Float},
                {typeof(Int32), SqlDbType.Int},
                {typeof(Decimal), SqlDbType.Decimal},
                {typeof(Single), SqlDbType.Real},
                {typeof(Int16), SqlDbType.SmallInt},
                {typeof(Object), SqlDbType.Variant},
                {typeof(TimeSpan), SqlDbType.Time},
                {typeof(Byte), SqlDbType.TinyInt},
                {typeof(Guid), SqlDbType.UniqueIdentifier}
            };

        private EntityTypesMapper()
        {
            //Final init Type dictionary
            _typeNamesAndDbTypes.Add("rowversion", SqlDbType.Timestamp);
            _typeNamesAndDbTypes.Add("numeric", SqlDbType.Decimal);
            _typeNamesAndDbTypes.Add("FILESTREAM", SqlDbType.VarBinary);
            _typeNamesAndDbTypes.Add("image", SqlDbType.Binary);
            _typeNamesAndDbTypes.Add("smalldatetime", SqlDbType.DateTime);
            _typeNamesAndDbTypes.Add("sql_variant", SqlDbType.Variant);
            
        }
        
        public static EntityTypesMapper Instance { get; } = new();
        
        public SqlDbType GetDbTypeFromString(string dbTypeName)
        {
            if (_typeNamesAndDbTypes.TryGetValue(dbTypeName, out var sqlType))
            {
                return sqlType;
            }

            throw new ImpossibleToMatchCurrentToSqlType(nameof(dbTypeName));
        }

        public SqlDbType GetDbTypeFromFrameworkType(Type frameworkType)
        {
            if (_frameworkToSqlType.TryGetValue(frameworkType, out var sqlType))
            {
                return sqlType;
            }

            throw new ImpossibleToMatchCurrentToSqlType(nameof(frameworkType));
        }
    }

    
}
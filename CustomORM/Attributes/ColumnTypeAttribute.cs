using System;

namespace CustomORM.Attributes
{
    public class ColumnTypeAttribute : DataAnnotationAttribute
    {
        public ColumnTypeAttribute(string dbTypeNameAttribute)
        {
            DbTypeNameAttribute = dbTypeNameAttribute;
        }

        public String DbTypeNameAttribute { get; set; }       
    }
}
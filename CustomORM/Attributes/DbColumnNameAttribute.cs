using System;

namespace CustomORM.Attributes
{
    
    public class DbColumnNameAttribute : DataAnnotationAttribute

    {
    public String ColumnName { get; set; }

    public DbColumnNameAttribute(String colName)
    {
        ColumnName = colName;
    }
    }
}
using System;

namespace CustomORM.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class TableNameAttribute : Attribute
    {
        public String TableName { get; set; }

        public TableNameAttribute(String tableName)
        {
            TableName = tableName;
        }
    }
}
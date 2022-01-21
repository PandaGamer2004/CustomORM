using System;

namespace CustomORM.Attributes
{
   

    public class ForeignKeyAttribute : DataAnnotationAttribute
    {
        public String PropertyName { get; set; }

        public ForeignKeyAttribute(String propName)
        {
            PropertyName = propName;
        }
    }
}
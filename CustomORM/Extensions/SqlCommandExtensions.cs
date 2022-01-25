using System.Collections.Generic;
using System.Data.SqlClient;

namespace CustomORM
{
    public static class SqlCommandExtensions
    {
        public static void AddParamsList(this SqlCommand command, IEnumerable<SqlParameter>? sqlParameters)
        {
            if(sqlParameters is null) return;

            foreach (var sqlParameter in sqlParameters)
            {
                command.Parameters.Add(sqlParameter);
            }
        } 
    }
}
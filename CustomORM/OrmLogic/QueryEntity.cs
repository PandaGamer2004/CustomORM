using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace CustomORM.OrmLogic
{
    public class QueryEntity
    {
        public QueryEntity(string queryText, IEnumerable<SqlParameter>? commandParams)
        {
            QueryText = queryText;
            CommandParams = commandParams;
        }

        public String QueryText { get;}
        public IEnumerable<SqlParameter>? CommandParams { get; }
        
    }
}
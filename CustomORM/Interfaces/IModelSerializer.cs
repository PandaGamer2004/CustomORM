using System;
using System.Data.SqlClient;

namespace CustomORM.Interfaces
{
    public interface IModelSerializer
    {
        Object SerializeRowToEntity(SqlDataReader reader);
    }
}
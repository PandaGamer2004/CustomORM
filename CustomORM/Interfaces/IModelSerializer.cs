using System.Data.SqlClient;

namespace CustomORM.Interfaces
{
    public interface IModelSerializer<T>
    {
        T SerializeRowToEntity(SqlDataReader reader);
    }
}
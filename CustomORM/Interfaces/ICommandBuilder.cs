using System;
using CustomORM.OrmLogic;

namespace CustomORM.Interfaces
{
    public interface ICommandBuilder<T>
    {
        QueryEntity GenerateSelectCommand();
        QueryEntity GenerateSelectCommand(Guid id);
        QueryEntity GenerateSelectCommand(Int32 id);
        QueryEntity GenerateInsertCommand(T entity);
        QueryEntity GenerateUpdateCommand(T entity);
        
        QueryEntity GenerateDeleteCommand(T entity);
    }
}
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using CustomORM.EventArgsObjects;

namespace CustomORM.Interfaces
{
    public interface IEntitySet<T> : IEnumerable<T>
    {
        void AddEntity(T entity);
        void RemoveEntity(T entityToRemove);
        void RemoveEntitiesRange(params T[] entitiesToRemove);
        void AddRangeEntities(params T[] entitiesToAdd);
        void Include(String nameOfIncludeProperty);
        void SetCurrentCommand(Object sender, CommandEventArgs cm);
    }
}
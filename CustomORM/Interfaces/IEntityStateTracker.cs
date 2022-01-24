using System.Collections;
using System.Collections.Generic;

namespace CustomORM.Interfaces
{
    public interface IEntityStateTracker<T> where T : class, new()
    {
        void StartTracking(T entity);

        void RegisterEntityToAdd(T entity);

        void RegisterEntitiesToAdd(params T[] entities);
        
        void RegisterEntityToDelete(T entity);

        void RegisterEntitiesToDelete(params T[] entities);
        void StartTrackingRange(params T[] entities);
        void RemoveFromTracking(T entity);
        void RemoveFromTracking(params T[] entities);

        void CheckTrackedEntitiesToChangeState();
        
        IEnumerable<T> GetEntitiesToAdd();

        IEnumerable<T> GetEntitiesToUpdate();

        IEnumerable<T> GetEntitiesToDelete();
    }
}
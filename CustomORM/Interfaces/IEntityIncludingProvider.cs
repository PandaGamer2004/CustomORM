using System;
using System.Collections.Generic;
using CustomORM.OrmLogic;

namespace CustomORM.Interfaces
{
    public interface IEntityIncludingProvider<T> where T: class, new()
    {
        public void AddIncludedEntities(List<EntityTrackingItem<T>>
            trackingItemsForInclude);

        public void RegisterEntitiesForInclude(IEnumerable<Object> entitiesToInclude);
    }
}
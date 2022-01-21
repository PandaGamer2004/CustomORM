using System.Collections.Generic;
using System.Linq;
using CustomORM.Exceptions;
using CustomORM.Interfaces;

namespace CustomORM.OrmLogic
{
    public class EntityStateTracker<T> : IEntityStateTracker<T> where T : class, new()
    {
        private readonly IEntityEqualityComparer<T> _entityEqualityComparer;
        private readonly IEntityCopyBuilder<T> _entityCopyBuilder;
        //TODO THINK ABOUT ANOTHER DATA STRUCTURE FOR TRACKED ENTITES
        private readonly List<EntityTrackingItem<T>> _trackedEntities = new();
        
        public EntityStateTracker(IEntityCopyBuilder<T> entityCopyBuilder, IEntityEqualityComparer<T> entityEqualityComparer)
        {
            _entityCopyBuilder = entityCopyBuilder;
            _entityEqualityComparer = entityEqualityComparer;
        }

        
        private IEnumerable<T> GetEntitiesWithState(EntityState state) =>
            _trackedEntities.Where(item => item.State == state).Select(item => item.TrackedEntity);

        private void MakeNewTrackItem(T entity, EntityState state)
        {
            var trackingItem = new EntityTrackingItem<T>(entity,state ,_entityEqualityComparer, _entityCopyBuilder);
            _trackedEntities.Add(trackingItem);
        }
        public void StartTracking(T entity)
        {
            MakeNewTrackItem(entity, EntityState.Tracked);
        }
        
        
        public void RegisterEntityToAdd(T entity)
        {
            MakeNewTrackItem(entity, EntityState.Added);
        }

        public void RegisterEntitiesToAdd(params T[] entities)
        {
            foreach (var entity in entities)
            {
                RegisterEntityToAdd(entity);
            }
        }

        public void RegisterEntityToDelete(T entity)
        {
            var registerEntityToDelete = _trackedEntities.FirstOrDefault(item =>
                item.IsTrackedEntityEqual(entity));

            if (registerEntityToDelete is null)
            {
                throw new EntityAlreadyDeletedException();
            }

            registerEntityToDelete.MakeStateDeleted();
        }

        public void StartTrackingRange(params T[] entities)
        {
            foreach (var entity in entities)
            {
                StartTracking(entity);
            }
        }
        
        public void RemoveFromTracking(T entity)
        {
            var trackedEntityItem = 
                _trackedEntities.FirstOrDefault(item => item.IsTrackedEntityEqual(entity));
            
            if (trackedEntityItem is not null)
            {
                _trackedEntities.Remove(trackedEntityItem);
            }
            
        }

        public void RemoveFromTracking(params T[] entities)
        {
            foreach (var entity in entities)
            {
                RemoveFromTracking(entity);
            }
        }

        public void CheckTrackedEntitiesToChangeState()
        {
            _trackedEntities.ForEach(item => item.CheckChangeForEntityState());
        }

        public IEnumerable<T> GetEntitiesToAdd() => GetEntitiesWithState(EntityState.Added);

        public IEnumerable<T> GetEntitiesToUpdate() => GetEntitiesWithState(EntityState.Changed);

        public IEnumerable<T> GetEntitiesToDelete() => GetEntitiesWithState(EntityState.Deleted);
    }
} 
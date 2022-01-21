using System;
using System.Collections.Generic;

namespace CustomORM.OrmLogic
{
    public class EntityInfoCollector
    {
        private static readonly Dictionary<Type, EntityInfo>
            EntityInfos = new();
        
        private EntityInfoCollector()
        {
            
        }

        public static EntityInfoCollector Instance { get; } = new();
        public EntityInfo GetEntityInfoForType(Type entityType)
        {
            if (!EntityInfos.ContainsKey(entityType))
            {
                EntityInfos[entityType] = new EntityInfo(entityType);
            }

            return EntityInfos[entityType];
        }
    }
}
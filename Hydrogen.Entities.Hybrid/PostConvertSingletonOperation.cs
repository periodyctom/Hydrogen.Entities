using System;
using Unity.Entities;
using UnityEngine;

namespace Hydrogen.Entities
{
    public abstract class PostConvertSingletonOperation<T> : PostConvertOperation
        where T : struct, IComponentData
    {
        static readonly Type k_SingletonType = typeof(T);
        
        [SerializeField] T m_Data;

        public override void Perform(EntityManager manager)
        {
            var query = manager.CreateEntityQuery(k_SingletonType);

            try
            {
                if (query.CalculateEntityCountWithoutFiltering() == 0)
                {
                    var archetype = manager.CreateArchetype(k_SingletonType);
                    manager.CreateEntity(archetype);
                }
            
                query.SetSingleton(m_Data);
            }
            finally
            {
                query.Dispose();
            }
        }
    }
}

using System;
using Unity.Collections;
using Unity.Entities;

namespace Hydrogen.Entities
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateBefore(typeof(EndInitializationEntityCommandBufferSystem))]
    public class SingletonConverterSystem<T0, T1> : ComponentSystem
        where T0 : ISingletonConverter<T1>
        where T1 : struct, IComponentData
    {
        private struct ConvertEntry
        {
            public T1 Data;
            public bool DontReplace;
            public bool Refresh;
        }
        
        private EntityQuery m_converterQuery;
        private EntityQuery m_singletonQuery;
        private EntityQuery m_refreshSingletonQuery;

        private NativeList<ConvertEntry> m_convertEntries;
        
        protected override void OnCreate()
        {
            m_convertEntries = new NativeList<ConvertEntry>(4, Allocator.Persistent);
            var converterDesc = new EntityQueryDesc
            {
                All = new[] {ComponentType.ReadOnly<T0>()},
                None = Array.Empty<ComponentType>(),
                Any = new[]
                {
                    ComponentType.ReadOnly<SingletonDontReplace>(), ComponentType.ReadOnly<SingletonRequiresRefresh>()
                }
            };

            m_converterQuery = GetEntityQuery(converterDesc);
            m_singletonQuery = GetEntityQuery(ComponentType.ReadWrite<T1>());
            m_refreshSingletonQuery = GetEntityQuery(ComponentType.ReadWrite<SingletonRefresh<T1>>());
        }

        protected override void OnDestroy()
        {
            m_convertEntries.Dispose();
        }

        protected override void OnUpdate()
        {
            RequireForUpdate(m_converterQuery);
        }
    }
}

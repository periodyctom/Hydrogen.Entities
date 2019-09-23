using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Assertions;

namespace Hydrogen.Entities
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class SingletonConvertSystem<T1> : ComponentSystem
        where T1 : struct, IComponentData
    {
        private EntityQuery m_preConvertedQuery;
        private EntityQuery m_postConvertedQuery;
        private EntityQuery m_singletonQuery;

        private readonly EntityQueryBuilder.F_D<SingletonConverter<T1>> m_process;

        private NativeList<(T1, bool)> m_candidates;
        private EntityArchetype m_singletonArchetype;
        private ComponentType m_convertedType;

        private bool m_hasPreviousValue;

        public SingletonConvertSystem() => m_process = Process;

        protected override void OnCreate()
        {
            m_candidates = new NativeList<(T1, bool)>(4, Allocator.Persistent);

            // ReSharper disable InconsistentNaming
            ComponentType singletonTypeRW = ComponentType.ReadWrite<T1>();
            ComponentType singletonTypeRO = ComponentType.ReadOnly<SingletonConverter<T1>>();

            // ReSharper restore InconsistentNaming

            m_singletonArchetype = EntityManager.CreateArchetype(singletonTypeRW);
            m_convertedType = ComponentType.ReadWrite<SingletonConverted>();

            m_preConvertedQuery = GetEntityQuery(singletonTypeRO, ComponentType.Exclude<SingletonConverted>());
            m_postConvertedQuery = GetEntityQuery(singletonTypeRO, ComponentType.ReadOnly<SingletonConverted>());
            m_singletonQuery = GetEntityQuery(singletonTypeRW);
        }

        protected override void OnDestroy()
        {
            m_candidates.Dispose();
        }

        protected override void OnUpdate()
        {
            int postConvertChunksLen = m_postConvertedQuery.CalculateChunkCountWithoutFiltering();

            if (postConvertChunksLen > 0)
                PostUpdateCommands.DestroyEntity(m_postConvertedQuery);

            int preConvertChunksLen = m_preConvertedQuery.CalculateChunkCountWithoutFiltering();

            if (preConvertChunksLen <= 0)
                return;

            m_hasPreviousValue = m_singletonQuery.CalculateChunkCountWithoutFiltering() == 1;

            Entities.ForEach(m_process);
            PostUpdateCommands.AddComponent(m_preConvertedQuery, m_convertedType);

            int candidatesLength = m_candidates.Length;
            Assert.IsTrue(candidatesLength > 0);

            if (candidatesLength < 1)
            {
                (T1 data, bool dontReplace) = m_candidates[0];
                FinalizeCandidate(data, dontReplace);
            }
            else
            {
                Debug.LogWarningFormat(
                    "There are {0} singleton conversion candidates for {1}! Resolving in the order acquired!",
                    candidatesLength.ToString(),
                    nameof(T1));

                if (!m_hasPreviousValue)
                {
                    (T1 data, bool dontReplace) = m_candidates[0];

                    for (int i = 1; i < candidatesLength; i++)
                    {
                        (T1 nextData, bool nextDontReplace) = m_candidates[i];

                        if (nextDontReplace)
                            continue;

                        data = nextData;
                        dontReplace = nextDontReplace;
                    }

                    FinalizeCandidate(data, dontReplace);
                }
                else
                {
                    (var data, bool dontReplace) = (m_singletonQuery.GetSingleton<T1>(), false);

                    for (int i = 0; i < candidatesLength; i++)
                    {
                        (T1 nextData, bool nextDontReplace) = m_candidates[i];

                        if (nextDontReplace)
                            continue;

                        data = nextData;
                    }

                    FinalizeCandidate(data, dontReplace);
                }
            }

            m_candidates.Clear();
        }

        private void FinalizeCandidate(T1 data, bool dontReplace)
        {
            if (!m_hasPreviousValue)
            {
                EntityManager.CreateEntity(m_singletonArchetype);
                m_singletonQuery.SetSingleton(data);
            }
            else if (!dontReplace)
            {
                m_singletonQuery.SetSingleton(data);
            }
        }

        private void Process([ReadOnly] ref SingletonConverter<T1> converter) =>
            m_candidates.Add((converter.Value, converter.DontReplace));
    }
}

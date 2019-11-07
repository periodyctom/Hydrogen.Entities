using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEngine;
using UnityEngine.Assertions;

namespace Hydrogen.Entities
{
    /// <summary>
    /// A generic system for handling the actual transformation from
    /// <see cref="SingletonConverter{T}"/> to a Singleton of <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">Singleton Component Data type.</typeparam>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class SingletonConvertSystem<T> : ComponentSystem
        where T : struct, IComponentData
    {
        private EntityQuery m_preConvertedQuery;
        private EntityQuery m_postConvertedQuery;
        private EntityQuery m_singletonQuery;

        private readonly EntityQueryBuilder.F_D<SingletonConverter<T>> m_process;

        private NativeList<(T, bool)> m_candidates;
        private EntityArchetype m_singletonArchetype;
        private ComponentType m_convertedType;

        private bool m_hasPreviousValue;

        private static readonly string sm_typeName = typeof(T).Name;

        public SingletonConvertSystem() => m_process = Process;

        protected override void OnCreate()
        {
            m_candidates = new NativeList<(T, bool)>(4, Allocator.Persistent);

            // ReSharper disable InconsistentNaming

            ComponentType singletonTypeRW = ComponentType.ReadWrite<T>();
            ComponentType singletonTypeRO = ComponentType.ReadOnly<SingletonConverter<T>>();

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

            if (candidatesLength == 1)
            {
                (T data, bool dontReplace) = m_candidates[0];
                FinalizeCandidate(data, dontReplace);
            }
            else
            {
                Debug.LogWarningFormat(
                    "There are {0} singleton conversion candidates for {1}! Resolving in the order acquired!",
                    candidatesLength.ToString(),
                    sm_typeName);

                if (!m_hasPreviousValue)
                {
                    (T data, bool dontReplace) = m_candidates[0];

                    for (int i = 1; i < candidatesLength; i++)
                    {
                        (T nextData, bool nextDontReplace) = m_candidates[i];

                        if (nextDontReplace)
                            continue;

                        data = nextData;
                        dontReplace = false;
                    }

                    FinalizeCandidate(data, dontReplace);
                }
                else
                {
                    (var data, bool dontReplace) = (m_singletonQuery.GetSingleton<T>(), false);

                    for (int i = 0; i < candidatesLength; i++)
                    {
                        (T nextData, bool nextDontReplace) = m_candidates[i];

                        if (nextDontReplace)
                            continue;

                        data = nextData;
                    }

                    FinalizeCandidate(data, dontReplace);
                }
            }

            m_candidates.Clear();
        }

        private void FinalizeCandidate(T data, bool dontReplace)
        {
            data = Prepare(data);

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

        private void Process([ReadOnly] ref SingletonConverter<T> converter) =>
            m_candidates.Add((converter.Value, converter.DontReplace));

        protected virtual T Prepare(T data) => data;
    }

    /// <summary>
    /// A generic system for handling the actual transformation from
    /// <see cref="SingletonConverter{BlobDataRef{T}}"/> to a Singleton of <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">Singleton Blob Reference struct Type</typeparam>
    public unsafe class SingletonBlobConvertSystem<T> : SingletonConvertSystem<BlobRefData<T>>
        where T : struct
    {
        protected sealed override BlobRefData<T> Prepare(BlobRefData<T> data)
        {
            // yea olde in-place copy-paste trick
            var writer = new MemoryBinaryWriter();
            writer.Write(data.Value);

            var reader = new MemoryBinaryReader(writer.Data);
            BlobAssetReference<T> copy = reader.Read<T>();

            writer.Dispose();
            reader.Dispose();

            return new BlobRefData<T>(copy);
        }
    }

    /// <summary>
    /// Base class for implementing Singleton Loader Component Systems that react
    /// to Singletons being created or changed.
    /// Used to avoid having the user write as much Boilerplate code.
    /// </summary>
    /// <typeparam name="T">Singleton Component Data type.</typeparam>
    public abstract class SingletonLoadedComponentSystem<T> : ComponentSystem
        where T : struct, IComponentData
    {
        protected override void OnCreate()
        {
            RequireForUpdate(
                GetEntityQuery(
                    ComponentType.ReadOnly<SingletonConverter<T>>(),
                    ComponentType.ReadOnly<SingletonConverted>()));

            RequireSingletonForUpdate<T>();
        }
    }

    /// <summary>
    /// Base class for implementing Singleton Blob Loader Component Systems that react
    /// to Blob Singletons being created or changed.
    /// Used to avoid having the user write as much Boilerplate code.
    /// </summary>
    /// <typeparam name="T">Asset Blob struct type.</typeparam>
    public abstract class SingletonBlobLoadedComponentSystem<T> : SingletonLoadedComponentSystem<BlobRefData<T>>
        where T : struct { }

    /// <summary>
    /// Base class for implementing Singleton Loader Job Component Systems that react
    /// to Singletons being created or changed.
    /// Used to avoid having the user write as much Boilerplate code.
    /// </summary>
    /// <typeparam name="T">Singleton Component Data type.</typeparam>
    public abstract class SingletonLoadedJobComponentSystem<T> : JobComponentSystem
        where T : struct, IComponentData
    {
        protected override void OnCreate()
        {
            RequireForUpdate(
                GetEntityQuery(
                    ComponentType.ReadOnly<SingletonConverter<T>>(),
                    ComponentType.ReadOnly<SingletonConverted>()));

            RequireSingletonForUpdate<T>();
        }
    }

    /// <summary>
    /// Base class for implementing Singleton Blob Loader Job Component Systems that react
    /// to Blob Singletons being created or changed.
    /// Used to avoid having the user write as much Boilerplate code.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class SingletonBlobLoadedJobComponentSystem<T> : SingletonLoadedJobComponentSystem<BlobRefData<T>>
        where T : struct { }
}

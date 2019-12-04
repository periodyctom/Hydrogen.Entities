using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.Assertions;

namespace Hydrogen.Entities
{
    /// <summary>
    /// The system group that all SingletonConvertSystems should run in.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(SceneSystemGroup))] 
    // TODO: Find a better way of handling this where we don't rely on any hybrid entity assemblies.
    // SceneSystemGroup lives in Unity.Scenes.Hybrid currently.
    public sealed class SingletonConvertGroup : ComponentSystemGroup {}
    
    /// <summary>
    /// The system group that systems reactive to Singleton Conversion should run in.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(SingletonConvertGroup))]
    public sealed class SingletonPostConvertGroup : ComponentSystemGroup {}
    
    /// <summary>
    /// A generic system for handling the actual transformation from
    /// <see cref="SingletonConverter{T}"/> to a Singleton of <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">Singleton Component Data type.</typeparam>
    [UpdateInGroup(typeof(SingletonConvertGroup))]
    public class SingletonConvertSystem<T> : ComponentSystem
        where T : struct, IComponentData
    {
        private EntityQuery m_preConvertedQuery;
        private EntityQuery m_postConvertedQuery;
        private EntityQuery m_singletonQuery;

        private readonly EntityQueryBuilder.F_ED<SingletonConverter<T>> m_process;

        private NativeList<(Entity, T, bool)> m_candidates;
        private EntityArchetype m_singletonArchetype;
        private ComponentType m_convertedType;
        private ComponentType m_unchangedType;

        private bool m_hasPreviousValue;

        private static readonly string sm_typeName = typeof(T).Name;

        public SingletonConvertSystem() => m_process = Process;

        protected override void OnCreate()
        {
            m_candidates = new NativeList<(Entity, T, bool)>(4, Allocator.Persistent);

            // ReSharper disable InconsistentNaming

            ComponentType singletonTypeRW = ComponentType.ReadWrite<T>();
            ComponentType singletonTypeRO = ComponentType.ReadOnly<SingletonConverter<T>>();

            // ReSharper restore InconsistentNaming

            m_singletonArchetype = EntityManager.CreateArchetype(singletonTypeRW);
            m_convertedType = ComponentType.ReadWrite<SingletonConverted>();
            m_unchangedType = ComponentType.ReadWrite<SingletonUnchanged>();

            m_preConvertedQuery = GetEntityQuery(singletonTypeRO, ComponentType.Exclude<SingletonConverted>());
            m_postConvertedQuery = GetEntityQuery(singletonTypeRO, ComponentType.ReadOnly<SingletonConverted>());
            m_singletonQuery = GetEntityQuery(singletonTypeRW);
        }

        protected override void OnDestroy() => m_candidates.Dispose();

        protected override void OnUpdate()
        {
            int postConvertChunksLen = m_postConvertedQuery.CalculateChunkCountWithoutFiltering();

            if (postConvertChunksLen > 0)
                PostUpdateCommands.DestroyEntity(m_postConvertedQuery);

            int preConvertChunksLen = m_preConvertedQuery.CalculateChunkCountWithoutFiltering();

            if (preConvertChunksLen <= 0)
                return;

            bool wasChanged = false;

            m_hasPreviousValue = m_singletonQuery.CalculateChunkCountWithoutFiltering() == 1;

            Entities.ForEach(m_process);
            PostUpdateCommands.AddComponent(m_preConvertedQuery, m_convertedType);

            int candidatesLength = m_candidates.Length;
            Assert.IsTrue(candidatesLength > 0);

            if (candidatesLength == 1)
            {
                (Entity entity, T data, bool dontReplace) = m_candidates[0];
                wasChanged = FinalizeCandidate(entity, data, dontReplace);
            }
            else
            {
                Debug.LogWarningFormat(
                    "There are {0} singleton conversion candidates for {1}! Resolving in the order acquired!",
                    candidatesLength.ToString(),
                    sm_typeName);

                if (!m_hasPreviousValue)
                {
                    (Entity entity, T data, bool dontReplace) = m_candidates[0];

                    for (int i = 1; i < candidatesLength; i++)
                    {
                        (Entity nextEntity, T nextData, bool nextDontReplace) = m_candidates[i];

                        if (nextDontReplace)
                            continue;

                        data = nextData;
                        dontReplace = false;
                        entity = nextEntity;
                    }

                    wasChanged = FinalizeCandidate(entity, data, dontReplace);
                }
                else
                {
                    (Entity entity, var data, bool dontReplace) =
                        (Entity.Null, m_singletonQuery.GetSingleton<T>(), false);

                    for (int i = 0; i < candidatesLength; i++)
                    {
                        (Entity nextEntity, T nextData, bool nextDontReplace) = m_candidates[i];

                        if (nextDontReplace)
                            continue;

                        data = nextData;
                        entity = nextEntity;
                    }

                    if (entity != Entity.Null)
                        wasChanged = FinalizeCandidate(entity, data, dontReplace);
                }
            }

            m_candidates.Clear();

            if (!wasChanged)
            {
                PostUpdateCommands.AddComponent(m_preConvertedQuery, m_unchangedType);
            }
        }

        private bool FinalizeCandidate(Entity entity, T data, bool dontReplace)
        {
            if (m_hasPreviousValue && dontReplace)
                return false;

            data = Prepare(data);

            if (!m_hasPreviousValue)
                EntityManager.CreateEntity(m_singletonArchetype);

            m_singletonQuery.SetSingleton(data);
            PostUpdateCommands.AddComponent<SingletonChanged>(entity);

            return true;
        }

        private void Process(Entity entity, [ReadOnly] ref SingletonConverter<T> converter) =>
            m_candidates.Add((entity, converter.Value, converter.DontReplace));

        protected virtual T Prepare(T data) => data;
    }

    /// <summary>
    /// A generic system for handling the actual transformation from
    /// SingletonConverter&lt;<see cref="BlobRefData{T}"/>&gt; to a Singleton of BlobRefData&lt;<typeparamref name="T"/>&gt;
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
    /// Base class for implementing Component Systems that react to Singletons being created or changed.
    /// Used to avoid having the user write as much boilerplate code.
    /// </summary>
    /// <typeparam name="T">Singleton Component Data type.</typeparam>
    [UpdateInGroup(typeof(SingletonPostConvertGroup))]
    public abstract class SingletonChangedComponentSystem<T> : ComponentSystem
        where T : struct, IComponentData
    {
        protected EntityQuery ChangedQuery;
        protected override void OnCreate() => ChangedQuery = this.SetupChangedQuery<T>(GetEntityQuery);
    }

    /// <summary>
    /// Base class for implementing Component Systems that react to Blob Singletons being created or changed.
    /// Used to avoid having the user write as much boilerplate code.
    /// </summary>
    /// <typeparam name="T">Asset Blob struct type.</typeparam>
    public abstract class SingletonBlobChangedComponentSystem<T> : SingletonChangedComponentSystem<BlobRefData<T>>
        where T : struct { }

    /// <summary>
    /// Base class for implementing Job Component Systems that react to Singletons being created or changed.
    /// Used to avoid having the user write as much boilerplate code.
    /// </summary>
    /// <typeparam name="T">Singleton Component Data type.</typeparam>
    [UpdateInGroup(typeof(SingletonPostConvertGroup))]
    public abstract class SingletonChangedJobComponentSystem<T> : JobComponentSystem
        where T : struct, IComponentData
    {
        protected EntityQuery ChangedQuery;

        protected override void OnCreate() => ChangedQuery = this.SetupChangedQuery<T>(GetEntityQuery);
    }

    /// <summary>
    /// Base class for implementing Job Component Systems that react to Blob Singletons being created or changed.
    /// Used to avoid having the user write as much boilerplate code.
    /// </summary>
    /// <typeparam name="T">Asset Blob struct type.</typeparam>
    public abstract class SingletonBlobChangedJobComponentSystem<T> : SingletonChangedJobComponentSystem<BlobRefData<T>>
        where T : struct { }

    /// <summary>
    /// Base class for implementing Component Systems that react to a conversion attempt that didn't succeed.
    /// Used to avoid having the user write as much boilerplate code.
    /// </summary>
    /// <typeparam name="T">Singleton Component Data type.</typeparam>
    [UpdateInGroup(typeof(SingletonPostConvertGroup))]
    public abstract class SingletonUnchangedComponentSystem<T> : ComponentSystem
        where T : struct, IComponentData
    {
        protected EntityQuery UnchangedQuery;

        protected override void OnCreate() => UnchangedQuery = this.SetupUnChangedQuery<T>(GetEntityQuery);
    }

    /// <summary>
    /// Base class for implementing Job Component Systems that react to a conversion attempt that didn't succeed.
    /// Used to avoid having the user write as much boilerplate code.
    /// </summary>
    /// <typeparam name="T">Singleton Component Data type.</typeparam>
    [UpdateInGroup(typeof(SingletonPostConvertGroup))]
    public abstract class SingletonUnchangedJobComponentSystem<T> : JobComponentSystem
        where T : struct, IComponentData
    {
        protected EntityQuery UnchangedQuery;

        protected override void OnCreate() => UnchangedQuery = this.SetupUnChangedQuery<T>(GetEntityQuery);
    }

    /// <summary>
    /// /// Base class for implementing Component Systems that react to a blob conversion attempt that didn't succeed.
    /// Used to avoid having the user write as much boilerplate code.
    /// </summary>
    /// <typeparam name="T">Asset Blob struct type.</typeparam>
    public abstract class SingletonBlobUnchangedComponentSystem<T> : SingletonUnchangedComponentSystem<BlobRefData<T>>
        where T : struct { }

    /// <summary>
    /// /// Base class for implementing Job Component Systems that react to a blob conversion attempt that didn't succeed.
    /// Used to avoid having the user write as much boilerplate code.
    /// </summary>
    /// <typeparam name="T">Asset Blob struct type.</typeparam>
    public abstract class
        SingletonBlobUnchangedJobComponentSystem<T> : SingletonUnchangedJobComponentSystem<BlobRefData<T>>
        where T : struct { }

    internal static class SingletonSystemEx
    {
        public delegate EntityQuery GetQuery(params ComponentType[] types);

        private static EntityQuery SetupQuery<T>(
            ComponentSystemBase system,
            GetQuery getQuery,
            params ComponentType[] types)
            where T : struct, IComponentData
        {
            EntityQuery setQuery = getQuery(types);
            system.RequireForUpdate(setQuery);
            system.RequireSingletonForUpdate<T>();

            return setQuery;
        }

        public static EntityQuery SetupChangedQuery<T>(this ComponentSystemBase system, GetQuery getQuery)
            where T : struct, IComponentData
        {
            return SetupQuery<T>(
                system,
                getQuery,
                ComponentType.ReadOnly<SingletonConverter<T>>(),
                ComponentType.ReadOnly<SingletonConverted>(),
                ComponentType.ReadOnly<SingletonChanged>());
        }

        public static EntityQuery SetupUnChangedQuery<T>(this ComponentSystemBase system, GetQuery getQuery)
            where T : struct, IComponentData
        {
            return SetupQuery<T>(
                system,
                getQuery,
                ComponentType.ReadOnly<SingletonConverter<T>>(),
                ComponentType.ReadOnly<SingletonConverted>(),
                ComponentType.ReadOnly<SingletonUnchanged>(),
                ComponentType.Exclude<SingletonChanged>());
        }
    }
}

using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.CodeGeneratedJobForEach;
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
    /// <see cref="SingletonConverter{T}"/> to a Singleton of <typeparamref name="T0"/>
    /// </summary>
    /// <typeparam name="T0">Singleton Component Data type.</typeparam>
    [UpdateInGroup(typeof(SingletonConvertGroup))]
    public abstract class SingletonConvertSystem<T0, T1> : SystemBase
        where T0 : struct, IComponentData
        where T1 : struct, ISingletonConverter<T0>
    {
        EntityQuery m_PreConvertedQuery;
        EntityQuery m_PostConvertedQuery;
        EntityQuery m_SingletonQuery;
        
        protected struct Candidate
        {
            public Entity Entity;
            public T1 Converter;
            public bool DontReplace;

            public Candidate(Entity entity, in T1 converter)
            {
                Entity = entity;
                Converter = converter;
                DontReplace = converter.DontReplace;
            }
        }

        NativeList<Candidate> m_Candidates;
        EntityArchetype m_SingletonArchetype;
        ComponentType m_ConvertedType;
        ComponentType m_UnchangedType;

        bool m_HasPreviousValue;
        bool m_WasChanged;

        static readonly string k_SmTypeName = typeof(T0).Name;

        protected override void OnCreate()
        {
            m_Candidates = new NativeList<Candidate>(4, Allocator.Persistent);

            // ReSharper disable InconsistentNaming

            var singletonTypeRW = ComponentType.ReadWrite<T0>();
            var converterTypeRO = ComponentType.ReadOnly<T1>();

            // ReSharper restore InconsistentNaming

            m_SingletonArchetype = EntityManager.CreateArchetype(singletonTypeRW);
            m_ConvertedType = ComponentType.ReadWrite<SingletonConverted>();
            m_UnchangedType = ComponentType.ReadWrite<SingletonUnchanged>();

            m_PreConvertedQuery = GetEntityQuery(converterTypeRO, ComponentType.Exclude<SingletonConverted>());
            m_PostConvertedQuery = GetEntityQuery(converterTypeRO, ComponentType.ReadOnly<SingletonConverted>());
            m_SingletonQuery = GetEntityQuery(singletonTypeRW);
        }

        protected override void OnDestroy() => m_Candidates.Dispose();

        [BurstCompile]
        struct CollectCandidates : IJobChunk
        {
            public NativeList<Candidate> Candidates;
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            [ReadOnly] public ArchetypeChunkComponentType<T1> ConverterType;
            
            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var chunkLen = chunk.Count;
                var entities = chunk.GetNativeArray(EntityType);
                var converters = chunk.GetNativeArray(ConverterType);

                for (var i = 0; i < chunkLen; i++)
                    Candidates.Add(new Candidate(entities[i], converters[i]));
            }
        }

        protected override void OnUpdate()
        {
            CompleteDependency();
            var cmdBuffer = new EntityCommandBuffer(Allocator.Temp, PlaybackPolicy.SinglePlayback);
            
            var postConvertChunksLen = m_PostConvertedQuery.CalculateChunkCountWithoutFiltering();

            if (postConvertChunksLen > 0)
                cmdBuffer.DestroyEntity(m_PostConvertedQuery);

            var preConvertChunksLen = m_PreConvertedQuery.CalculateChunkCountWithoutFiltering();

            if (preConvertChunksLen <= 0)
            {
                cmdBuffer.Playback(EntityManager);
                cmdBuffer.Dispose();
                return;
            }
            
            m_HasPreviousValue = m_SingletonQuery.CalculateChunkCountWithoutFiltering() == 1;

            new CollectCandidates
            {
                Candidates = m_Candidates,
                EntityType = GetArchetypeChunkEntityType(),
                ConverterType = GetArchetypeChunkComponentType<T1>(true)
            }.Run(m_PreConvertedQuery);
            
            var wasChanged = false;
            
            cmdBuffer.AddComponent(m_PreConvertedQuery, m_ConvertedType);

            var candidatesLength = m_Candidates.Length;
            Assert.IsTrue(candidatesLength > 0);

            if (candidatesLength == 1)
            {
                var candidate = m_Candidates[0];
                wasChanged = FinalizeCandidate(cmdBuffer, candidate);
            }
            else
            {
                Debug.LogWarningFormat(
                    "There are {0} singleton conversion candidates for {1}! Resolving in the order acquired!",
                    candidatesLength.ToString(),
                    k_SmTypeName);

                if (!m_HasPreviousValue)
                {
                    var candidate = m_Candidates[0];

                    for (var i = 1; i < candidatesLength; i++)
                    {
                        var next = m_Candidates[i];

                        if (next.DontReplace)
                            continue;

                        candidate = next;
                    }

                    wasChanged = FinalizeCandidate(cmdBuffer, candidate);
                }
                else
                {
                    var candidate = new Candidate
                    {
                       Entity = Entity.Null,
                       Converter = new T1 {Singleton = m_SingletonQuery.GetSingleton<T0>()},
                       DontReplace = false
                    };

                    for (var i = 0; i < candidatesLength; i++)
                    {
                        var next = m_Candidates[i];

                        if (next.DontReplace)
                            continue;

                        candidate.Entity = next.Entity;
                        candidate.Converter = next.Converter;
                    }

                    if (candidate.Entity != Entity.Null)
                        wasChanged = FinalizeCandidate(cmdBuffer, candidate);
                }
            }

            m_Candidates.Clear();

            if (!wasChanged)
                cmdBuffer.AddComponent(m_PreConvertedQuery, m_UnchangedType);

            cmdBuffer.Playback(EntityManager);
        }

        bool FinalizeCandidate(EntityCommandBuffer cmdBuffer, Candidate candidate)
        {
            if (m_HasPreviousValue && candidate.DontReplace)
                return false;

            candidate.Converter.Singleton = Prepare(candidate.Converter.Singleton);

            if (!m_HasPreviousValue)
                EntityManager.CreateEntity(m_SingletonArchetype);

            m_SingletonQuery.SetSingleton(candidate.Converter.Singleton);
            cmdBuffer.AddComponent<SingletonChanged>(candidate.Entity);

            return true;
        }

        protected virtual T0 Prepare(T0 data) => data;
    }

    /// <summary>
    /// A generic system for handling the actual transformation from
    /// SingletonConverter&lt;<see cref="BlobRefData{T}"/>&gt; to a Singleton of BlobRefData&lt;<typeparamref name="T0"/>&gt;
    /// </summary>
    /// <typeparam name="T0">Singleton Blob Reference struct Type</typeparam>
    public abstract unsafe class SingletonBlobConvertSystem<T0, T1> : SingletonConvertSystem<BlobRefData<T0>, T1>
        where T0 : struct
        where T1 : struct, ISingletonConverter<BlobRefData<T0>>
    {
        protected sealed override BlobRefData<T0> Prepare(BlobRefData<T0> data)
        {
            // yea olde in-place copy-paste trick
            var writer = new MemoryBinaryWriter();
            writer.Write(data.Value);

            var reader = new MemoryBinaryReader(writer.Data);
            var copy = reader.Read<T0>();

            writer.Dispose();
            reader.Dispose();

            return new BlobRefData<T0>(copy);
        }
    }

    /// <summary>
    /// Used as a base for systems that react to changed (or unchanged) singleton conversions.
    /// </summary>
    /// <typeparam name="T0">The Singleton Component Data Type</typeparam>
    [UpdateInGroup(typeof(SingletonPostConvertGroup))]
    public abstract class SingletonPostConvertSystem<T0, T1> : SystemBase
        where T0 : struct, IComponentData
        where T1 : struct, ISingletonConverter<T0>
    {
        protected abstract EntityQuery Query
        {
            get;
            set;
        }
        
        protected enum QuerySetup
        {
            OnCreateAsChanged,
            OnCreateAsUnchanged,
            CustomOrForeach
        }

        protected virtual QuerySetup ConvertKind => QuerySetup.CustomOrForeach;

        protected override void OnCreate()
        {
            switch (ConvertKind)
            {
                case QuerySetup.OnCreateAsChanged:
                    Setup<SingletonChanged, SingletonUnchanged>();
                    break;
                case QuerySetup.OnCreateAsUnchanged:
                    Setup<SingletonUnchanged, SingletonChanged>();
                    break;
                case QuerySetup.CustomOrForeach:
                    break;
            }
            
            RequireForUpdate(Query);
            RequireSingletonForUpdate<T0>();
        }

        void Setup<T2, T3>()
            where T2 : struct, IComponentData
            where T3 : struct, IComponentData
        {
            Query = GetEntityQuery(
                ComponentType.ReadOnly<T1>(),
                ComponentType.ReadOnly<SingletonConverted>(),
                ComponentType.ReadOnly<T2>(),
                ComponentType.Exclude<T3>());
        }
    }

    /// <summary>
    /// Used as a base for systems that react to changed (or unchanged) blob singleton conversions.
    /// </summary>
    /// <typeparam name="T0">The struct type the BlobAssetReference manages</typeparam>
    public abstract class SingletonBlobPostConvertSystem<T0, T1> : SingletonPostConvertSystem<BlobRefData<T0>, T1> 
        where T0 : struct
        where T1 : struct, ISingletonConverter<BlobRefData<T0>>
    {
    }
}

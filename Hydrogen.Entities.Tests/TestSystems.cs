using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Hydrogen.Entities.Tests
{
    public sealed class TimeConfigConvertSystem : SingletonConvertSystem<TimeConfig> { }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(TimeConfigConvertSystem))]
    public sealed class TimeConfigChangedSystem : SingletonChangedComponentSystem<TimeConfig>
    {
        protected override void OnUpdate()
        {
            var config = GetSingleton<TimeConfig>();

            Time.fixedDeltaTime = config.FixedDeltaTime;
            Application.targetFrameRate = (int) config.AppTargetFrameRate;

            Debug.Log("Updated Time Config!");
        }
    }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(TimeConfigConvertSystem))]
    public sealed class TimeConfigUnchangedSystem : SingletonUnchangedComponentSystem<TimeConfig>
    {
        protected override void OnUpdate()
        {
            Entities.With(UnchangedQuery).ForEach(
                (Entity e, ref SingletonConverter<TimeConfig> d0) =>
                {
                    Debug.Log(
                        $"Wasn't set {e.ToString()}: {d0.Value.AppTargetFrameRate:D}|{d0.DontReplace.ToString()}");
                });
        }
    }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(TimeConfigConvertSystem))]
    public sealed class TimeConfigUnchangedJobSystem : SingletonUnchangedJobComponentSystem<TimeConfig>
    {
        private struct Entry
        {
            public Entity Entity;
            public SingletonConverter<TimeConfig> Converter;
        }
        
        [BurstCompile]
        private struct CollectJob : IJobForEachWithEntity_EC<SingletonConverter<TimeConfig>>
        {
            public NativeList<Entry> Entries;
            public void Execute(Entity entity, int index, [ReadOnly] ref SingletonConverter<TimeConfig> c0)
            {
                Entries.Add(new Entry
                {
                    Entity = entity,
                    Converter = c0,
                });
            }
        }
        
        private struct LogJob : IJob
        {
            [ReadOnly] public NativeList<Entry> Entries;

            public void Execute()
            {
                int len = Entries.Length;

                for (int i = 0; i < len; i++)
                {
                    Entry e = Entries[i];
                    Debug.Log($"Job UnchangedTimeConfig {e.Entity.ToString()}|{e.Converter.DontReplace.ToString()}");
                }
            }
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var entries = new NativeList<Entry>(
                UnchangedQuery.CalculateEntityCountWithoutFiltering(),
                Allocator.TempJob);

            inputDeps = new CollectJob
            {
                Entries = entries,
            }.ScheduleSingle(UnchangedQuery, inputDeps);

            inputDeps = new LogJob
            {
                Entries = entries,
            }.Schedule(inputDeps);

            inputDeps = entries.Dispose(inputDeps);

            return inputDeps;
        }
    }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(TimeConfigConvertSystem))]
    public sealed class TimeConfigChangedJobSystem : SingletonChangedJobComponentSystem<TimeConfig>
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new TestJob
            {
                Config = GetSingleton<TimeConfig>(),
            }.Schedule(inputDeps);
        }

        [BurstCompile]
        private struct TestJob : IJob
        {
            [ReadOnly] public TimeConfig Config;

            public void Execute()
            {
                TimeConfig modified = Config;
                modified.FixedDeltaTime *= 2.0f;
                modified.AppTargetFrameRate *= 2u;

                Log(modified);
            }

            [BurstDiscard]
            private void Log(TimeConfig timeConfig) =>
                Debug.Log($"Modified config in Job: {timeConfig.FixedDeltaTime:N8}|{timeConfig.AppTargetFrameRate:D}");
        }
    }

    public sealed class LocalesConvertSystem : SingletonBlobConvertSystem<Locales> { }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(LocalesConvertSystem))]
    public sealed class LocalesChangedSystem : SingletonBlobChangedComponentSystem<Locales>
    {
        private readonly StringBuilder m_localeListBuilder = new StringBuilder(1024);

        protected override void OnUpdate()
        {
            ref Locales supportedLocales = ref GetSingleton<BlobRefData<Locales>>().Resolve;

            ref BlobString name = ref supportedLocales.Name;

            m_localeListBuilder.AppendLine(name.ToString());

            int availableCount = supportedLocales.Available.Length;

            if (availableCount <= 1)
                return;

            m_localeListBuilder.AppendLine("Available Locales:");

            for (int i = 0; i < availableCount; i++)
            {
                m_localeListBuilder.AppendLine($"  {supportedLocales.Available[i].ToString()}");
            }

            Debug.Log(m_localeListBuilder.ToString());
        }
    }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(LocalesConvertSystem))]
    public sealed class LocalesChangedJobSystem : SingletonBlobChangedJobComponentSystem<Locales>
    {
        private static readonly StringBuilder sm_builder = new StringBuilder(1024);

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps = new CollectLocales
            {
                RefData = GetSingleton<BlobRefData<Locales>>(),
            }.Schedule(inputDeps);

            inputDeps = new LogLocales().Schedule(inputDeps);

            return inputDeps;
        }
        
        private struct CollectLocales : IJob
        {
            [ReadOnly] public BlobRefData<Locales> RefData;

            public void Execute()
            {
                ref Locales locales = ref RefData.Resolve;
                ref BlobString name = ref locales.Name; 
                sm_builder.AppendLine(name.ToString());
                
                int len = locales.Available.Length;
                for (int i = 0; i < len; i++)
                {
                    ref BlobString str = ref locales.Available[i];
                    sm_builder.AppendLine(str.ToString());
                }
            }
        }
        
        private struct LogLocales : IJob
        {
            public void Execute()
            {
                Debug.Log(sm_builder.ToString());
                sm_builder.Clear();
            }
        }
    }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(LocalesConvertSystem))]
    public sealed class LocalesUnchangedSystem : SingletonBlobUnchangedComponentSystem<Locales>
    {
        private static readonly StringBuilder sm_stringBuilder = new StringBuilder(1024);
        private static readonly EntityQueryBuilder.F_ED<SingletonConverter<BlobRefData<Locales>>> sm_logUnchanged =
            LogUnchanged;
        
        protected override void OnUpdate()
        {
            Entities.With(UnchangedQuery).ForEach(sm_logUnchanged);
            Debug.Log(sm_stringBuilder.ToString());
            sm_stringBuilder.Clear();
        }

        private static void LogUnchanged(Entity entity, [ReadOnly] ref SingletonConverter<BlobRefData<Locales>> d0)
        {
            ref Locales locales = ref d0.Value.Resolve;
            ref BlobString name = ref locales.Name;
            sm_stringBuilder.AppendLine($"Unchanged {name.ToString()}");
            int len = locales.Available.Length;

            for (int i = 0; i < len; i++)
            {
                ref BlobString str = ref locales.Available[i];
                sm_stringBuilder.AppendLine(str.ToString());
            }
        }
    }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(LocalesConvertSystem))]
    public sealed class LocalesUnchangedJobSystem : SingletonBlobUnchangedJobComponentSystem<Locales>
    {
        private static readonly StringBuilder sm_stringBuilder = new StringBuilder(1024);

        private struct CollectUnchanged : IJobForEachWithEntity_EC<SingletonConverter<BlobRefData<Locales>>>
        {
            public void Execute(Entity entity, int index, [ReadOnly] ref SingletonConverter<BlobRefData<Locales>> c0)
            {
                ref Locales locales = ref c0.Value.Resolve;
                ref BlobString name = ref locales.Name;

                sm_stringBuilder.AppendLine("Unchanged Locales In Job");
                sm_stringBuilder.AppendLine(name.ToString());
                
                int len = locales.Available.Length;

                for (int i = 0; i < len; i++)
                {
                    ref BlobString str = ref locales.Available[i];
                    sm_stringBuilder.AppendLine(str.ToString());
                }
            }
        }
        
        private struct Log : IJob
        {
            public void Execute()
            {
                Debug.Log(sm_stringBuilder.ToString());
                sm_stringBuilder.Clear();
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps = new CollectUnchanged().ScheduleSingle(UnchangedQuery, inputDeps);

            inputDeps = new Log().Schedule(inputDeps);

            return inputDeps;
        }
    }
}

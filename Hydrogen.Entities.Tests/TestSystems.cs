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
    public sealed class TimeConfigLoadedSystem : SingletonLoadedComponentSystem<TimeConfig>
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
    public sealed class TimeConfigLoadedJobSystem : SingletonLoadedJobComponentSystem<TimeConfig>
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
    public sealed class LocalesLoadedSystem : SingletonBlobLoadedComponentSystem<Locales>
    {
        private readonly StringBuilder m_localeListBuilder = new StringBuilder(1024);

        protected override void OnUpdate()
        {
            ref Locales supportedLocales = ref GetSingleton<BlobRefData<Locales>>().Resolve;

            Debug.LogFormat("Current default locale is: {0}", supportedLocales.Default.Value.ToString());

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

    public sealed class LocalesLoadedJobSystem : SingletonBlobLoadedJobComponentSystem<Locales>
    {
        private static readonly StringBuilder sm_builder = new StringBuilder(1024);

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var temp = new NativeList<NativeString64>(8, Allocator.TempJob);

            JobHandle collectLocalesHandle = new CollectLocales
            {
                Locales = temp,
                RefData = GetSingleton<BlobRefData<Locales>>(),
            }.Schedule(inputDeps);

            JobHandle reportHandle = new LogLocales
            {
                Locales = temp,
            }.Schedule(collectLocalesHandle);
            
            return temp.Dispose(reportHandle);
        }

        [BurstCompile]
        private unsafe struct CollectLocales : IJob
        {
            public NativeList<NativeString64> Locales;
            [ReadOnly] public BlobRefData<Locales> RefData;

            public void Execute()
            {
                Locales.Clear();
                ref Locales locales = ref RefData.Resolve;
                int len = locales.Available.Length;

                var ptr = (NativeString64*) locales.Available.GetUnsafePtr();
                Locales.AddRange(ptr, len);
            }
        }
        
        private struct LogLocales : IJob
        {
            [ReadOnly] public NativeList<NativeString64> Locales;

            public void Execute()
            {
                int len = Locales.Length;
                for (int i = 0; i < len; i++)
                {
                    sm_builder.AppendLine($"Job logged Locale: {i:D} {Locales[i].ToString()}");
                }
                
                Debug.Log(sm_builder.ToString());
                sm_builder.Clear();
            }
        }
    }
}

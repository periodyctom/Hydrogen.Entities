using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Hydrogen.Entities.Tests
{
    public sealed class TimeConfigConvertSystem : SingletonConvertSystem<TimeConfig, TimeConfigConverter>
    {
    }

    public sealed class TimeConfigChangedSystem : SingletonPostConvertSystem<TimeConfig, TimeConfigConverter>
    {
        protected override EntityQuery Query { get; set; }

        protected override QuerySetup ConvertKind => QuerySetup.OnCreateAsChanged;

        protected override void OnUpdate()
        {
            var config = GetSingleton<TimeConfig>();

            UnityEngine.Time.fixedDeltaTime = config.FixedDeltaTime;
            Application.targetFrameRate = (int) config.AppTargetFrameRate;

            Debug.Log("Updated Time Config!");
        }
    }

    public sealed class TimeConfigUnchangedSystem : SingletonPostConvertSystem<TimeConfig, TimeConfigConverter>
    {
        EntityQuery m_Query;

        protected override EntityQuery Query
        {
            get => m_Query;
            set => m_Query = value;
        }

        protected override QuerySetup ConvertKind => QuerySetup.CustomOrForeach;

        protected override void OnUpdate()
        {
            Entities.ForEach(
                    (Entity entity, in TimeConfigConverter converter) =>
                    {
                        Debug.Log($"Wasn't set {entity.ToString()}: {converter.Singleton.AppTargetFrameRate:D}|{converter.DontReplace.ToString()}");
                    })
               .WithoutBurst()
               .WithAll<SingletonChanged>()
               .WithStoreEntityQueryInField(ref m_Query)
               .Run();
        }
    }

    public sealed class TimeConfigUnchangedJobSystem : SingletonPostConvertSystem<TimeConfig, TimeConfigConverter>
    {
        struct Entry
        {
            public Entity Entity;
            public TimeConfigConverter Converter;
        }

        EntityQuery m_Query;

        protected override EntityQuery Query
        {
            get => m_Query;
            set => m_Query = value;
        }

        protected override void OnUpdate()
        {
            var entries = new NativeList<Entry>(
                m_Query.CalculateEntityCountWithoutFiltering(),
                Allocator.TempJob);

            var handle = Entities.ForEach(
                    (Entity entity, in TimeConfigConverter timeConfig) =>
                    {
                        entries.Add(
                            new Entry
                            {
                                Entity = entity,
                                Converter = timeConfig
                            });
                    })
               .WithAll<SingletonUnchanged>()
               .WithStoreEntityQueryInField(ref m_Query)
               .Schedule(Dependency);

            handle = Job.WithCode(
                () =>
                {
                    var len = entries.Length;

                    for (var i = 0; i < len; i++)
                    {
                        var e = entries[i];
                        Debug.Log($"Job UnchangedTimeConfig {e.Entity.ToString()}|{e.Converter.DontReplace.ToString()}");
                    }
                })
               .WithoutBurst()
               .Schedule(handle);

            Dependency = entries.Dispose(handle);
        }
    }

    public sealed class TimeConfigChangedJobSystem : SingletonPostConvertSystem<TimeConfig, TimeConfigConverter>
    {
        protected override EntityQuery Query { get; set; }

        protected override QuerySetup ConvertKind => QuerySetup.OnCreateAsChanged;

        protected override void OnUpdate()
        {
            var timeConfig = GetSingleton<TimeConfig>();
            
            Job.WithCode(
                () =>
                {
                    var modified = timeConfig;
                    modified.FixedDeltaTime *= 2.0f;
                    modified.AppTargetFrameRate *= 2u;

                    Log(modified);
                }).Schedule();
        }

        [BurstDiscard]
        static void Log(in TimeConfig timeConfig)
        {
            Debug.Log($"Modified config in Job: {timeConfig.FixedDeltaTime:N8}|{timeConfig.AppTargetFrameRate:D}");
        }
    }

    public sealed class LocalesConvertSystem : SingletonBlobConvertSystem<Locales, LocalesConverter>
    {
    }

    public sealed class LocalesChangedSystem : SingletonBlobPostConvertSystem<Locales, LocalesConverter>
    {
        protected override EntityQuery Query { get; set; }

        readonly StringBuilder m_LocaleListBuilder = new StringBuilder(1024);

        protected override QuerySetup ConvertKind => QuerySetup.OnCreateAsChanged;

        protected override void OnUpdate()
        {
            ref var supportedLocales = ref GetSingleton<BlobRefData<Locales>>().Resolve;

            ref var name = ref supportedLocales.Name;

            m_LocaleListBuilder.AppendLine(name.ToString());

            var availableCount = supportedLocales.Available.Length;

            if (availableCount <= 1)
                return;

            m_LocaleListBuilder.AppendLine("Available Locales:");

            for (var i = 0; i < availableCount; i++)
                m_LocaleListBuilder.AppendLine($"  {supportedLocales.Available[i].ToString()}");

            Debug.Log(m_LocaleListBuilder.ToString());
        }
    }

    public sealed class LocalesChangedJobSystem : SingletonBlobPostConvertSystem<Locales, LocalesConverter>
    {
        static readonly StringBuilder k_Builder = new StringBuilder(1024);

        protected override EntityQuery Query { get; set; }

        protected override QuerySetup ConvertKind => QuerySetup.OnCreateAsChanged;

        protected override void OnUpdate()
        {
            var refData = GetSingleton<BlobRefData<Locales>>();

            Job.WithCode(
                    () =>
                    {
                        ref var locales = ref refData.Resolve;
                        ref var name = ref locales.Name;
                        k_Builder.AppendLine(name.ToString());

                        var len = locales.Available.Length;

                        for (var i = 0; i < len; i++)
                        {
                            ref var str = ref locales.Available[i];
                            k_Builder.AppendLine(str.ToString());
                        }
                    })
               .WithoutBurst()
               .Schedule();

            Job.WithCode(
                    () =>
                    {
                        Debug.Log(k_Builder.ToString());
                        k_Builder.Clear();
                    })
               .WithoutBurst()
               .Schedule();
        }
    }

    public sealed class LocalesUnchangedSystem : SingletonBlobPostConvertSystem<Locales, LocalesConverter>
    {
        static readonly StringBuilder k_StringBuilder = new StringBuilder(1024);

        EntityQuery m_Query;

        protected override EntityQuery Query
        {
            get => m_Query;
            set => m_Query = value;
        }

        protected override void OnUpdate()
        {
            Entities.ForEach(
                    (in LocalesConverter d0) =>
                    {
                        ref var locales = ref d0.Singleton.Resolve;
                        ref var name = ref locales.Name;
                        k_StringBuilder.AppendLine($"Unchanged {name.ToString()}");
                        var len = locales.Available.Length;

                        for (var i = 0; i < len; i++)
                        {
                            ref var str = ref locales.Available[i];
                            k_StringBuilder.AppendLine(str.ToString());
                        }
                    })
               .WithoutBurst()
               .WithAll<SingletonUnchanged>()
               .WithStoreEntityQueryInField(ref m_Query)
               .Run();
            
            Debug.Log(k_StringBuilder.ToString());
            k_StringBuilder.Clear();
        }
    }

    public sealed class LocalesUnchangedJobSystem : SingletonBlobPostConvertSystem<Locales, LocalesConverter>
    {
        static readonly StringBuilder k_StringBuilder = new StringBuilder(1024);

        EntityQuery m_Query;

        protected override EntityQuery Query
        {
            get => m_Query;
            set => m_Query = value;
        }

        protected override void OnUpdate()
        {
            Entities.ForEach(
                    (in LocalesConverter l) =>
                    {
                        ref var locales = ref l.Singleton.Resolve;
                        ref var name = ref locales.Name;

                        k_StringBuilder.AppendLine("Unchanged Locales In Job");
                        k_StringBuilder.AppendLine(name.ToString());

                        var len = locales.Available.Length;

                        for (var j = 0; j < len; j++)
                        {
                            ref var str = ref locales.Available[j];
                            k_StringBuilder.AppendLine(str.ToString());
                        }
                    })
               .WithAll<SingletonUnchanged>()
               .WithStoreEntityQueryInField(ref m_Query)
               .WithoutBurst()
               .Schedule();

            Job.WithCode(
                    () =>
                    {
                        Debug.Log(k_StringBuilder.ToString());
                        k_StringBuilder.Clear();
                    })
               .WithoutBurst()
               .Schedule();
        }
    }
}

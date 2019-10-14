using System.Text;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;


namespace Hydrogen.Entities.Tests
{
    public sealed class LocalesConvertSystem : SingletonBlobConvertSystem<Locales>
    {
        protected override BlobRefData<Locales> Prepare(BlobRefData<Locales> data)
        {
            var b = new BlobBuilder(Allocator.Persistent);
            ref Locales src = ref data.Resolve;

            ref Locales dst = ref b.ConstructRoot<Locales>();

            int availLen = src.Available.Length;
            BlobBuilderArray<NativeString64> dstAvailable = b.Allocate(ref dst.Available, availLen);

            for (int i = 0; i < availLen; i++)
            {
                ref NativeString64 srcStr = ref src.Available[i];
                ref NativeString64 dstStr = ref dstAvailable[i];
                dstStr = srcStr;
            }

            ref NativeString64 srcDefault = ref src.Default.Value;
            ref NativeString64 dstDefault = ref b.Allocate(ref dst.Default);
            dstDefault = srcDefault;

            BlobAssetReference<Locales> reference = b.CreateBlobAssetReference<Locales>(Allocator.Persistent);

            data.Value = reference;
            
            b.Dispose();

            return data;
        }
    }

    public sealed class TimeConfigConvertSystem : SingletonConvertSystem<TimeConfig> { }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(LocalesConvertSystem))]
    public sealed class LocalesRefreshSystem : ComponentSystem
    {
        private readonly StringBuilder m_localeListBuilder = new StringBuilder(1024);

        protected override void OnCreate()
        {
            RequireForUpdate(
                GetEntityQuery(
                    ComponentType.ReadOnly<SingletonConverter<BlobRefData<Locales>>>(),
                    ComponentType.ReadOnly<SingletonConverted>()));

            RequireSingletonForUpdate<BlobRefData<Locales>>();
        }

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

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(TimeConfigConvertSystem))]
    public sealed class TimeConfigRefreshSystem : ComponentSystem
    {
        protected override void OnCreate()
        {
            RequireForUpdate(
                GetEntityQuery(
                    ComponentType.ReadOnly<SingletonConverter<TimeConfig>>(),
                    ComponentType.ReadOnly<SingletonConverted>()));

            RequireSingletonForUpdate<TimeConfig>();
        }

        protected override void OnUpdate()
        {
            var config = GetSingleton<TimeConfig>();

            Time.fixedDeltaTime = config.FixedDeltaTime;
            Application.targetFrameRate = (int) config.AppTargetFrameRate;

            Debug.Log("Updated Time Config!");
        }
    }
}
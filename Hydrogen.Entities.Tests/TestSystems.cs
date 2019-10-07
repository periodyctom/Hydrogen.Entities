

// ReSharper disable CheckNamespace

using System.Text;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Hydrogen.Entities.Tests
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(TestSupportedLocalesConvertSystem))]
    public class TestSupportedLocalesRefreshSystem : ComponentSystem
    {
        private readonly StringBuilder localeListBuilder = new StringBuilder(1024);

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

            localeListBuilder.AppendLine("Available Locales:");

            for (int i = 0; i < availableCount; i++)
            {
                localeListBuilder.AppendLine($"  {supportedLocales.Available[i].ToString()}");
            }

            Debug.Log(localeListBuilder.ToString());
        }
    }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(TestTimeConfigConvertSystem))]
    public class TestTimeConfigRefreshSystem : ComponentSystem
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

    public class TestSupportedLocalesConvertSystem : SingletonConvertSystem<BlobRefData<Locales>>
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

    public class TestTimeConfigConvertSystem : SingletonConvertSystem<TimeConfig> { }
}
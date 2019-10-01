

// ReSharper disable CheckNamespace

using System.Text;
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

    public class TestSupportedLocalesConvertSystem : SingletonConvertSystem<BlobRefData<Locales>> { }

    public class TestTimeConfigConvertSystem : SingletonConvertSystem<TimeConfig> { }
}
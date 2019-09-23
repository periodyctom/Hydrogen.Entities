using System;
using System.Runtime.CompilerServices;
using System.Text;
using NUnit.Framework;
using Unity.Entities;
using Unity.Entities.Tests;
using UnityEngine;



// ReSharper disable CheckNamespace

namespace Hydrogen.Entities.Tests
{
    public class SingletonTests : ECSTestsFixture
    {
        internal struct TestSupportedLocales
        {
            public BlobString Default;
            public BlobArray<NativeString64> Available;
        }

        internal struct TestTimeConfig : IComponentData
        {
            public uint AppTargetFrameRate;
            public float FixedDeltaTime;
        }
        
        private class TestTimeConfigConvertSystem : SingletonConvertSystem<TestTimeConfig> { }

        private class TestSupportedLocalesConvertSystem : SingletonConvertSystem<BlobRefData<TestSupportedLocales>> { }

        [UpdateInGroup(typeof(InitializationSystemGroup))]
        [UpdateAfter(typeof(TestTimeConfigConvertSystem))]
        private class TestTimeConfigRefreshSystem : ComponentSystem
        {
            protected override void OnCreate()
            {
                RequireForUpdate(
                    GetEntityQuery(
                        ComponentType.ReadOnly<SingletonConverter<TestTimeConfig>>(),
                        ComponentType.ReadOnly<SingletonConverted>()));

                RequireSingletonForUpdate<TestTimeConfig>();
            }

            protected override void OnUpdate()
            {
                var config = GetSingleton<TestTimeConfig>();

                Time.fixedDeltaTime = config.FixedDeltaTime;
                Application.targetFrameRate = (int) config.AppTargetFrameRate;

                Debug.Log("Updated Time Config!");
            }
        }

        [UpdateInGroup(typeof(InitializationSystemGroup))]
        [UpdateAfter(typeof(TestSupportedLocalesConvertSystem))]
        private class TestSupportedLocalesRefreshSystem : ComponentSystem
        {
            private readonly StringBuilder localeListBuilder = new StringBuilder(1024);

            protected override void OnCreate()
            {
                RequireForUpdate(
                    GetEntityQuery(
                        ComponentType.ReadOnly<SingletonConverter<BlobRefData<TestSupportedLocales>>>(),
                        ComponentType.ReadOnly<SingletonConverted>()));

                RequireSingletonForUpdate<BlobRefData<TestSupportedLocales>>();
            }

            protected override void OnUpdate()
            {
                ref TestSupportedLocales supportedLocales =
                    ref GetSingleton<BlobRefData<TestSupportedLocales>>().Resolve;

                Debug.LogFormat("Current default locale is: {0}", supportedLocales.Default.ToString());

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

        private struct SingletonQueries : IDisposable
        {
            public EntityQuery PreConverted;
            public EntityQuery PostConverted;
            public EntityQuery Singleton;

            private static readonly Type sm_convertedType = typeof(SingletonConverted);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private SingletonQueries(EntityQuery preConverted, EntityQuery postConverted, EntityQuery singleton)
            {
                PreConverted = preConverted;
                PostConverted = postConverted;
                Singleton = singleton;
            }

            public static SingletonQueries CreateQueries<T1>(EntityManager manager)
                where T1 : struct
            {
                ComponentType converterTypeRO = ComponentType.ReadOnly<SingletonConverter<T1>>();

                EntityQuery preConverted = manager.CreateEntityQuery(
                    converterTypeRO,
                    ComponentType.Exclude(sm_convertedType));

                EntityQuery postConverted = manager.CreateEntityQuery(
                    converterTypeRO,
                    ComponentType.ReadOnly(sm_convertedType));

                EntityQuery singleton = manager.CreateEntityQuery(typeof(T1));

                return new SingletonQueries(preConverted, postConverted, singleton);
            }

            public void Dispose()
            {
                PreConverted.Dispose();
                PostConverted.Dispose();
                Singleton.Dispose();
            }
        }

        private SingletonQueries m_testTimeConfigs;
        private SingletonQueries m_testSupportedLocales;

        // TODO: Data Converter produces correct Singleton
        // TODO: BlobConverter produces correct Singleton

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            m_testTimeConfigs = SingletonQueries.CreateQueries<TestTimeConfig>(m_Manager);
            m_testSupportedLocales = SingletonQueries.CreateQueries<BlobRefData<TestSupportedLocales>>(m_Manager);

            World world = m_Manager.World;

            var initGroup = world.GetOrCreateSystem<InitializationSystemGroup>();
            initGroup.AddSystemToUpdateList(world.CreateSystem<TestTimeConfigConvertSystem>());
            initGroup.AddSystemToUpdateList(world.CreateSystem<TestSupportedLocalesConvertSystem>());
            initGroup.AddSystemToUpdateList(world.CreateSystem<TestTimeConfigRefreshSystem>());
            initGroup.AddSystemToUpdateList(world.CreateSystem<TestSupportedLocalesRefreshSystem>());
            initGroup.SortSystemUpdateList();
        }

        [TearDown]
        public override void TearDown()
        {
            m_testTimeConfigs.Dispose();
            m_testSupportedLocales.Dispose();

            base.TearDown();
        }

        #region Data Converter

        [Test, Ignore("Not implemented")]
        public void DataConverter_SetsSingleton_WithOneConverter()
        {
            EntityArchetype testTimeConverter =
                m_Manager.CreateArchetype(ComponentType.ReadWrite<SingletonConverter<TestTimeConfig>>());

            EntityArchetype testTimeConverted = m_Manager.CreateArchetype(
                ComponentType.ReadOnly<SingletonConverter<TestTimeConfig>>(),
                ComponentType.ReadOnly<SingletonConverted>());
            
            // Check initial set
            SingletonConverter<TestTimeConfig> timeConfig = new TestTimeConfig
            {
                AppTargetFrameRate = 60,
                FixedDeltaTime = 1 / 60.0f,
            };

            Entity converterEntity = m_Manager.CreateEntity(testTimeConverter);
            m_Manager.SetComponentData(converterEntity, timeConfig);

            Assert.IsTrue(m_testTimeConfigs.PreConverted.CalculateEntityCount() == 1);
            //
            // World.Update();
            //
            //
            //
            // World.Update();

            // Check Replace


            // Check DontReplace Flag


            // Check destroy
        }

        [Test, Ignore("Not implemented")]
        public void DataConverter_SetsSingleton_WithMultipleConverters() { }

        #endregion

        #region Blob Converter

        [Test, Ignore("Not implemented")]
        public void BlobConverter_SetsSingleton_WithOneConverter() { }

        [Test, Ignore("Not implemented")]
        public void BlobConverter_SetsSingleton_WithMultipleConverters() { }

        #endregion
    }
}

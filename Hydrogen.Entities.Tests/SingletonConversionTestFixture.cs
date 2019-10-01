using System;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Tests;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace Hydrogen.Entities.Tests
{
    using TimeConfigConverter = SingletonConverter<TimeConfig>;
    using LocalesRef = BlobRefData<Locales>;
    using LocalesConverter = SingletonConverter<BlobRefData<Locales>>;

    public class SingletonConversionTestFixture : ECSTestsFixture
    {
        protected struct SingletonQueries : IDisposable
        {
            public readonly EntityQuery PreConverted;
            public readonly EntityQuery PostConverted;
            public readonly EntityQuery Singleton;

            private static readonly Type sm_convertedType = typeof(SingletonConverted);

            public static SingletonQueries CreateQueries<T>(EntityManager manager)
                where T : struct, IComponentData
            {
                ComponentType converterTypeRO = ComponentType.ReadOnly<SingletonConverter<T>>();

                EntityQuery preConverted = manager.CreateEntityQuery(
                    converterTypeRO,
                    ComponentType.Exclude(sm_convertedType));

                EntityQuery postConverted = manager.CreateEntityQuery(
                    converterTypeRO,
                    ComponentType.ReadOnly(sm_convertedType));

                EntityQuery singleton = manager.CreateEntityQuery(typeof(T));

                return new SingletonQueries(preConverted, postConverted, singleton);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private SingletonQueries(EntityQuery preConverted, EntityQuery postConverted, EntityQuery singleton)
            {
                PreConverted = preConverted;
                PostConverted = postConverted;
                Singleton = singleton;
            }

            public void AssertCounts(int preCount, int postCount, int singletonCount)
            {
                Assert.IsTrue(PreConverted.CalculateEntityCount() == preCount);
                Assert.IsTrue(PostConverted.CalculateEntityCount() == postCount);
                Assert.IsTrue(Singleton.CalculateEntityCount() == singletonCount);
            }

            public void Dispose()
            {
                PreConverted.Dispose();
                PostConverted.Dispose();
                Singleton.Dispose();
            }
        }

        protected SingletonQueries m_timeConfigs;
        protected SingletonQueries m_locales;

        protected readonly Action<TimeConfig, TimeConfigConverter> m_assertTimeConfigs = AssertTimeConfig;
        protected readonly Action<LocalesRef, LocalesConverter> m_assertSupportedLocales = AssertSupportedLocales;

        protected static void AssertTimeConfig(TimeConfig current, TimeConfigConverter converter)
        {
            Assert.IsTrue(current.FixedDeltaTime == converter.Value.FixedDeltaTime);
            Assert.IsTrue(current.AppTargetFrameRate == converter.Value.AppTargetFrameRate);

            Assert.IsTrue(current.FixedDeltaTime == Time.fixedDeltaTime);
            Assert.IsTrue(current.AppTargetFrameRate == (uint) Application.targetFrameRate);
        }

        protected static void AssertSupportedLocales(LocalesRef current, LocalesConverter converter)
        {
            Assert.IsTrue(current.IsCreated && converter.Value.IsCreated);

            ref Locales a = ref current.Resolve;
            ref Locales b = ref converter.Value.Resolve;

            ref NativeString64 aDefault = ref a.Default.Value;
            ref NativeString64 bDefault = ref b.Default.Value;

            Assert.IsTrue(aDefault.Equals(bDefault));
            Assert.IsTrue(a.Available.Length == b.Available.Length);

            int availableLen = a.Available.Length;

            for (int i = 0; i < availableLen; i++)
            {
                ref NativeString64 aAvailable = ref a.Available[i];
                ref NativeString64 bAvailable = ref b.Available[i];
                Assert.IsTrue(aAvailable.Equals(bAvailable));
            }
        }

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            m_timeConfigs = SingletonQueries.CreateQueries<TimeConfig>(m_Manager);
            m_locales = SingletonQueries.CreateQueries<LocalesRef>(m_Manager);

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
            m_timeConfigs.Dispose();
            m_locales.Dispose();

            base.TearDown();
        }

        public static BlobAssetReference<Locales> CreateLocaleData(params string[] available)
        {
            Assert.IsNotNull(available);
            Assert.IsTrue(available.Length > 0);

            var builder = new BlobBuilder(Allocator.Temp);

            ref Locales root = ref builder.ConstructRoot<Locales>();

            ref NativeString64 defaultStr = ref builder.Allocate(ref root.Default);
            defaultStr = new NativeString64(available[0]);

            int availableLen = available.Length;
            BlobBuilderArray<NativeString64> builderArray = builder.Allocate(ref root.Available, available.Length);

            for (int i = 0; i < availableLen; i++)
            {
                ref NativeString64 element = ref builderArray[i];
                element = new NativeString64(available[i]);
            }

            BlobAssetReference<Locales> refData = builder.CreateBlobAssetReference<Locales>(Allocator.Persistent);

            builder.Dispose();

            return refData;
        }

        public static LocalesRef CreateLocaleRefData(params string[] available) =>
            new LocalesRef(CreateLocaleData(available));
    }
}

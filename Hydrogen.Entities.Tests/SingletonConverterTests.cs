using System;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Tests;
using UnityEngine;
using UnityEngine.TestTools;

// ReSharper disable CompareOfFloatsByEqualityOperator
// ReSharper disable CheckNamespace

namespace Hydrogen.Entities.Tests
{
    using TimeConfigConverter = SingletonConverter<TestTimeConfig>;
    using SupportedLocalesRef = BlobRefData<TestSupportedLocales>;
    using SupportedLocalesConverter = SingletonConverter<BlobRefData<TestSupportedLocales>>;
    
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

        protected SingletonQueries m_testTimeConfigs;
        protected SingletonQueries m_testSupportedLocales;
        
        protected readonly Action<TestTimeConfig, TimeConfigConverter> m_assertTimeConfigs = AssertTimeConfig;

        protected readonly Action<SupportedLocalesRef, SupportedLocalesConverter> m_assertSupportedLocales =
            AssertSupportedLocales;
        
        protected static void AssertTimeConfig(TestTimeConfig current, TimeConfigConverter converter)
        {
            Assert.IsTrue(current.FixedDeltaTime == converter.Value.FixedDeltaTime);
            Assert.IsTrue(current.AppTargetFrameRate == converter.Value.AppTargetFrameRate);

            Assert.IsTrue(current.FixedDeltaTime == Time.fixedDeltaTime);
            Assert.IsTrue(current.AppTargetFrameRate == (uint) Application.targetFrameRate);
        }
        
        protected static void AssertSupportedLocales(SupportedLocalesRef current, SupportedLocalesConverter converter)
        {
            Assert.IsTrue(current.IsCreated && converter.Value.IsCreated);

            ref TestSupportedLocales a = ref current.Resolve;
            ref TestSupportedLocales b = ref converter.Value.Resolve;

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

            m_testTimeConfigs = SingletonQueries.CreateQueries<TestTimeConfig>(m_Manager);
            m_testSupportedLocales = SingletonQueries.CreateQueries<SupportedLocalesRef>(m_Manager);

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
    }
    
    public class SingletonConverterTests : SingletonConversionTestFixture
    {
        private void AssertSerialConversion<T>(
            in SingletonQueries queries,
            EntityArchetype archetype,
            Action<T, SingletonConverter<T>> assertSame,
            SingletonConverter<T> initialData,
            SingletonConverter<T> finalData,
            int startingCount)
            where T : struct, IComponentData
        {
            Assert.IsNotNull(assertSame);

            SingletonConverter<T> initialConverter = initialData;
            Entity converterEntity = m_Manager.CreateEntity(archetype);
            m_Manager.SetComponentData(converterEntity, initialConverter);

            queries.AssertCounts(1, 0, startingCount);

            World.Update();

            queries.AssertCounts(0, 1, 1);

            var singletonData = queries.Singleton.GetSingleton<T>();
            assertSame(singletonData, finalData);

            World.Update();

            queries.AssertCounts(0, 0, 1);
        }

        private void AssertFirstConversion<T>(
            in SingletonQueries queries,
            EntityArchetype archetype,
            Action<T, SingletonConverter<T>> assertSame,
            SingletonConverter<T> data)
            where T : struct, IComponentData
        {
            AssertSerialConversion(
                queries,
                archetype,
                assertSame,
                data,
                data,
                0);
        }

        private void AssertReplaceConversion<T>(
            in SingletonQueries queries,
            EntityArchetype archetype,
            Action<T, SingletonConverter<T>> assertSame,
            SingletonConverter<T> data)
            where T : struct, IComponentData
        {
            AssertSerialConversion(
                queries,
                archetype,
                assertSame,
                data,
                data,
                1);
        }

        private void AssertDontReplaceConversion<T>(
            in SingletonQueries queries,
            EntityArchetype archetype,
            Action<T, SingletonConverter<T>> assertSame,
            SingletonConverter<T> ignoredData,
            SingletonConverter<T> actualData)
            where T : struct, IComponentData
        {
            AssertSerialConversion(
                queries,
                archetype,
                assertSame,
                ignoredData,
                actualData,
                1);
        }

        private void TestSimpleConversion<T>(
            in SingletonQueries queries,
            Action<T, SingletonConverter<T>> assertSame,
            T initial,
            T replace,
            T dontReplace)
            where T : struct, IComponentData
        {
            EntityArchetype archetype = m_Manager.CreateArchetype(ComponentType.ReadWrite<SingletonConverter<T>>());

            // Check initial set
            SingletonConverter<T> initConverter = initial;
            AssertFirstConversion(queries, archetype, assertSame, initConverter);

            // Check Replace
            SingletonConverter<T> replaceConverter = replace;
            AssertReplaceConversion(queries, archetype, assertSame, replaceConverter);

            // Check Don't Replace
            var dontReplaceConverter = new SingletonConverter<T>(dontReplace, true);
            AssertDontReplaceConversion(queries, archetype, assertSame, dontReplaceConverter, replaceConverter);

            // Check Destroy
            Entity singletonEntity = queries.Singleton.GetSingletonEntity();
            m_Manager.DestroyEntity(singletonEntity);

            queries.AssertCounts(0, 0, 0);
        }

        private void TestMultipleConversion<T>(
            in SingletonQueries queries,
            EntityArchetype archetype,
            Action<T, SingletonConverter<T>> assertSame,
            NativeArray<SingletonConverter<T>> converters,
            int expectedFinalIndex)
            where T : struct, IComponentData
        {
            int len = converters.Length;

            for (int i = 0; i < len; i++)
            {
                SingletonConverter<T> converter = converters[i];
                Entity converterEntity = m_Manager.CreateEntity(archetype);
                m_Manager.SetComponentData(converterEntity, converter);
            }
            
            queries.AssertCounts(len, 0, 0);

            LogAssert.Expect(
                LogType.Warning,
                $"There are {len.ToString()} singleton conversion candidates for {typeof(T).Name}! Resolving in the order acquired!");
            
            World.Update();
            
            queries.AssertCounts(0, len, 1);

            var singleton = queries.Singleton.GetSingleton<T>();
            assertSame(singleton, converters[expectedFinalIndex]);
        }
        
        #region Data Converter

        

        [Test]
        public void DataConverter_SetsAndDestroysSingleton_WithSerialConverters()
        {
            TestSimpleConversion(
                m_testTimeConfigs,
                m_assertTimeConfigs,
                new TestTimeConfig(60, 1.0f / 60.0f),
                new TestTimeConfig(30, 1.0f / 30.0f),
                new TestTimeConfig(144, 1.0f / 144.0f));
        }

        [Test]
        public void DataConverter_SetsSingleton_WithMultipleConverters()
        {
            EntityArchetype archetype = m_Manager.CreateArchetype(typeof(TimeConfigConverter));

            var converters = new NativeArray<TimeConfigConverter>(4, Allocator.Temp)
            {
                [0] = new TestTimeConfig(15, 1.0f / 15.0f),
                [1] = new TimeConfigConverter(new TestTimeConfig(30, 1.0f / 30.0f), true),
                [2] = new TestTimeConfig(60, 1.0f / 60.0f),
                [3] = new TimeConfigConverter(new TestTimeConfig(120, 1.0f / 120.0f), true)
            };

            try
            {
                TestMultipleConversion(m_testTimeConfigs, archetype, m_assertTimeConfigs, converters, 2);
            }
            finally
            {
                converters.Dispose();
            }
        }

        #endregion

        #region Blob Converter

        private SupportedLocalesRef CreateLocaleData(params string[] available)
        {
            Assert.IsNotNull(available);
            Assert.IsTrue(available.Length > 0);

            var builder = new BlobBuilder(Allocator.Temp);

            ref TestSupportedLocales root = ref builder.ConstructRoot<TestSupportedLocales>();

            ref NativeString64 defaultStr = ref builder.Allocate(ref root.Default);
            defaultStr = new NativeString64(available[0]);

            int availableLen = available.Length;
            BlobBuilderArray<NativeString64> builderArray = builder.Allocate(ref root.Available, available.Length);

            for (int i = 0; i < availableLen; i++)
            {
                ref NativeString64 element = ref builderArray[i];
                element = new NativeString64(available[i]);
            }

            var refData = new SupportedLocalesRef(
                builder.CreateBlobAssetReference<TestSupportedLocales>(Allocator.Persistent));

            builder.Dispose();

            return refData;
        }

        [Test]
        public void BlobConverter_SetsAndDestroysSingleton_WithSerialConverters()
        {
            SupportedLocalesRef initial = CreateLocaleData("en", "fr", "it", "de", "es");
            SupportedLocalesRef replace = CreateLocaleData("zh", "ja", "ko");
            SupportedLocalesRef dontReplace = CreateLocaleData("en-us", "en-gb", "la");

            try
            {
                TestSimpleConversion(m_testSupportedLocales, m_assertSupportedLocales, initial, replace, dontReplace);
            }
            finally
            {
                initial.Value.Dispose();
                replace.Value.Dispose();
                dontReplace.Value.Dispose();
            }
        }

        [Test]
        public void BlobConverter_SetsSingleton_WithMultipleConverters()
        {
            EntityArchetype archetype = m_Manager.CreateArchetype(typeof(SupportedLocalesConverter));

            var converters =
                new NativeArray<SupportedLocalesConverter>(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory)
                {
                    [0] = new SupportedLocalesConverter(CreateLocaleData("zh", "ja", "ko")),
                    [1] = new SupportedLocalesConverter(CreateLocaleData("la"), true),
                    [2] = new SupportedLocalesConverter(CreateLocaleData("en", "fr", "it", "de", "es")),
                    [3] = new SupportedLocalesConverter(CreateLocaleData("en-us", "en-gb"), true)
                };
            
            try
            {
                TestMultipleConversion(m_testSupportedLocales, archetype, m_assertSupportedLocales, converters, 2);
            }
            finally
            {
                for (int i = 0; i < converters.Length; i++)
                {
                    SupportedLocalesRef blobRefData = converters[i].Value;
                    blobRefData.Value.Dispose();
                }

                converters.Dispose();
            }
        }

        #endregion
    }
}

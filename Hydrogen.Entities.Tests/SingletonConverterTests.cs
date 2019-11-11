using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.TestTools;


namespace Hydrogen.Entities.Tests
{
    using TimeConfigConverter = SingletonConverter<TimeConfig>;
    using LocalesRef = BlobRefData<Locales>;
    using LocalesConverter = SingletonConverter<BlobRefData<Locales>>;

    public class SingletonConverterTests : SingletonConversionTestFixture
    {
        private void AssertSerialConversion<T>(
            SingletonQueries queries,
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
            SingletonQueries queries,
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
            SingletonQueries queries,
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

            AssertDontReplaceConversion(queries, archetype, assertSame, dontReplaceConverter, replace);

            // Check Destroy
            Entity singletonEntity = queries.Singleton.GetSingletonEntity();
            m_Manager.DestroyEntity(singletonEntity);

            queries.AssertCounts(0, 0, 0);
        }

        private void TestMultipleConversion<T>(
            SingletonQueries queries,
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
            
            World.Update();
            
            queries.AssertCounts(0, 0, 1);
        }

        #region Data Converter

        [Test]
        public void DataConverter_SetsAndDestroysSingleton_WithSerialConverters()
        {
            TestSimpleConversion(
                TimeConfigQueries,
                CachedAssertTimeConfigs,
                new TimeConfig(60, 1.0f / 60.0f),
                new TimeConfig(30, 1.0f / 30.0f),
                new TimeConfig(144, 1.0f / 144.0f));
        }

        [Test]
        public void DataConverter_SetsSingleton_WithMultipleConverters()
        {
            EntityArchetype archetype = m_Manager.CreateArchetype(typeof(TimeConfigConverter));

            var converters = new NativeArray<TimeConfigConverter>(4, Allocator.Temp)
            {
                [0] = new TimeConfig(15, 1.0f / 15.0f),
                [1] = new TimeConfigConverter(new TimeConfig(30, 1.0f / 30.0f), true),
                [2] = new TimeConfig(60, 1.0f / 60.0f),
                [3] = new TimeConfigConverter(new TimeConfig(120, 1.0f / 120.0f), true)
            };

            try
            {
                TestMultipleConversion(TimeConfigQueries, archetype, CachedAssertTimeConfigs, converters, 2);
            }
            finally
            {
                converters.Dispose();
            }
        }

        #endregion

        #region Blob Converter

        private static void TryDispose(LocalesRef refData)
        {
            if (refData.IsCreated)
                refData.Value.Dispose();
        }

        [Test]
        public void BlobConverter_SetsAndDestroysSingleton_WithSerialConverters()
        {
            
            LocalesConverter initial = CreateLocaleRefData("initial", "en", "fr", "it", "de", "es");
            LocalesConverter replace = CreateLocaleRefData("replace", "zh", "ja", "ko");
            LocalesConverter dontReplace = CreateLocaleRefData("dontReplace", "en-us", "en-gb", "la");
            
            try
            {
                TestSimpleConversion(LocalesQueries, CachedAssertSupportedLocales, initial, replace, dontReplace);
            }
            finally
            {
                TryDispose(initial);
                TryDispose(replace);
                TryDispose(dontReplace);
            }
        }

        [Test]
        public void BlobConverter_SetsSingleton_WithMultipleConverters()
        {
            EntityArchetype archetype = m_Manager.CreateArchetype(typeof(LocalesConverter));

            var converters =
                new NativeArray<LocalesConverter>(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory)
                {
                    [0] = new LocalesConverter(CreateLocaleRefData("0","zh", "ja", "ko")),
                    [1] = new LocalesConverter(CreateLocaleRefData("1","la"), true),
                    [2] = new LocalesConverter(CreateLocaleRefData("2","en", "fr", "it", "de", "es")),
                    [3] = new LocalesConverter(CreateLocaleRefData("3","en-us", "en-gb"), true)
                };

            try
            {
                TestMultipleConversion(LocalesQueries, archetype, CachedAssertSupportedLocales, converters, 2);
            }
            finally
            {
                for (int i = 0; i < converters.Length; i++)
                    TryDispose(converters[i].Value);

                converters.Dispose();
            }
        }

        #endregion
    }
}

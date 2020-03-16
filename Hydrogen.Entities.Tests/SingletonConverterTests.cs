using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hydrogen.Entities.Tests
{
    public class SingletonConverterTests : SingletonConversionTestFixture
    {
        void AssertSerialConversion<T0, T1>(
            SingletonQueries queries,
            EntityArchetype archetype,
            Action<T0, T1> assertSame,
            T1 initialConverter,
            T0 finalData,
            int startingCount)
            where T0 : struct, IComponentData
            where T1 : struct, ISingletonConverter<T0>
        {
            Assert.IsNotNull(assertSame);

            var converterEntity = m_Manager.CreateEntity(archetype);
            m_Manager.SetComponentData(converterEntity, initialConverter);

            queries.AssertCounts(1, 0, startingCount);

            World.Update();

            queries.AssertCounts(0, 1, 1);

            var singletonData = queries.Singleton.GetSingleton<T0>();
            assertSame(singletonData, new T1 {Singleton = finalData});

            World.Update();

            queries.AssertCounts(0, 0, 1);
        }

        void AssertFirstConversion<T0, T1>(
            in SingletonQueries queries,
            EntityArchetype archetype,
            Action<T0, T1> assertSame,
            T1 converter)
            where T0 : struct, IComponentData
            where T1 : struct, ISingletonConverter<T0>
        {
            AssertSerialConversion(queries, archetype, assertSame, converter, converter.Singleton, 0);
        }

        void AssertReplaceConversion<T0, T1>(
            in SingletonQueries queries,
            EntityArchetype archetype,
            Action<T0, T1> assertSame,
            T1 converter)
            where T0 : struct, IComponentData
            where T1 : struct, ISingletonConverter<T0>
        {
            AssertSerialConversion(queries, archetype, assertSame, converter, converter.Singleton, 1);
        }

        void AssertDontReplaceConversion<T0, T1>(
            SingletonQueries queries,
            EntityArchetype archetype,
            Action<T0, T1> assertSame,
            T1 ignoredData,
            T0 actualData)
            where T0 : struct, IComponentData
            where T1 : struct, ISingletonConverter<T0>
        {
            AssertSerialConversion(queries, archetype, assertSame, ignoredData, actualData, 1);
        }

        void TestSimpleConversion<T0, T1>(
            SingletonQueries queries,
            Action<T0, T1> assertSame,
            T0 initial,
            T0 replace,
            T0 dontReplace)
            where T0 : struct, IComponentData
            where T1 : struct, ISingletonConverter<T0>
        {
            var archetype = m_Manager.CreateArchetype(ComponentType.ReadWrite<T1>());

            // Check initial set
            var initialConverter = new T1
            {
                Singleton = initial,
            };
            
            AssertFirstConversion(queries, archetype, assertSame, initialConverter);

            // Check Replace
            var replaceConverter = new T1
            {
                Singleton = replace
            };
            AssertReplaceConversion(queries, archetype, assertSame, replaceConverter);

            // Check Don't Replace
            var dontReplaceConverter = new T1
            {
                Singleton = dontReplace,
                DontReplace = true,
            };

            AssertDontReplaceConversion(queries, archetype, assertSame, dontReplaceConverter, replace);

            // Check Destroy
            var singletonEntity = queries.Singleton.GetSingletonEntity();
            m_Manager.DestroyEntity(singletonEntity);

            queries.AssertCounts(0, 0, 0);
        }

        void TestMultipleConversion<T0, T1>(
            SingletonQueries queries,
            EntityArchetype archetype,
            Action<T0, T1> assertSame,
            NativeArray<T1> converters,
            int expectedFinalIndex)
            where T0 : struct, IComponentData
            where T1 : struct, ISingletonConverter<T0>
        {
            var len = converters.Length;

            for (var i = 0; i < len; i++)
            {
                var converter = converters[i];
                var converterEntity = m_Manager.CreateEntity(archetype);
                m_Manager.SetComponentData(converterEntity, converter);
            }

            queries.AssertCounts(len, 0, 0);

            LogAssert.Expect(
                LogType.Warning,
                $"There are {len.ToString()} singleton conversion candidates for {typeof(T0).Name}! Resolving in the order acquired!");

            World.Update();

            queries.AssertCounts(0, len, 1);
            
            var singleton = queries.Singleton.GetSingleton<T0>();
            assertSame(singleton, converters[expectedFinalIndex]);
            
            World.Update();
            
            queries.AssertCounts(0, 0, 1);
        }

        #region Data Converter

        [Test]
        public void DataConverter_SetsAndDestroysSingleton_WithSerialConverters()
        {
            TestSimpleConversion<TimeConfig, TimeConfigConverter>(
                TimeConfigQueries,
                CachedAssertTimeConfigs,
                new TimeConfig(60, 1.0f / 60.0f),
                new TimeConfig(30, 1.0f / 30.0f),
                new TimeConfig(144, 1.0f / 144.0f));
        }

        [Test]
        public void DataConverter_SetsSingleton_WithMultipleConverters()
        {
            var archetype = m_Manager.CreateArchetype(typeof(TimeConfigConverter));

            var converters = new NativeArray<TimeConfigConverter>(4, Allocator.Temp)
            {
                [0] = new TimeConfigConverter
                {
                    Singleton = new TimeConfig(15, 1.0f / 15.0f),
                },
                [1] = new TimeConfigConverter
                {
                    Singleton = new TimeConfig(30, 1.0f / 30.0f),
                    DontReplace = true,
                },
                [2] = new TimeConfigConverter
                {
                    Singleton = new TimeConfig(60, 1.0f / 60.0f),
                },
                [3] = new TimeConfigConverter
                {
                    Singleton = new TimeConfig(120, 1.0f / 120.0f),
                    DontReplace = true,
                },
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

        static void TryDispose(BlobRefData<Locales> refData)
        {
            if (refData.IsCreated)
                refData.Value.Dispose();
        }

        [Test]
        public void BlobConverter_SetsAndDestroysSingleton_WithSerialConverters()
        {
            var initial = CreateLocaleRefData("initial", "en", "fr", "it", "de", "es");
            var replace = CreateLocaleRefData("replace", "zh", "ja", "ko");
            var dontReplace = CreateLocaleRefData("dontReplace", "en-us", "en-gb", "la");
            
            try
            {
                TestSimpleConversion<BlobRefData<Locales>, LocalesConverter>(LocalesQueries, CachedAssertSupportedLocales, initial, replace, dontReplace);
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
            var archetype = m_Manager.CreateArchetype(typeof(LocalesConverter));

            var converters =
                new NativeArray<LocalesConverter>(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory)
                {
                    [0] = new LocalesConverter{Singleton = CreateLocaleRefData("0", "zh", "ja", "ko")},
                    [1] = new LocalesConverter{Singleton = CreateLocaleRefData("1", "la"), DontReplace = true},
                    [2] = new LocalesConverter{Singleton = CreateLocaleRefData("2","en", "fr", "it", "de", "es")},
                    [3] = new LocalesConverter{Singleton = CreateLocaleRefData("3","en-us", "en-gb"), DontReplace = true},
                };

            try
            {
                TestMultipleConversion(LocalesQueries, archetype, CachedAssertSupportedLocales, converters, 2);
            }
            finally
            {
                for (var i = 0; i < converters.Length; i++)
                    TryDispose(converters[i].Singleton);

                converters.Dispose();
            }
        }

        #endregion
    }
}

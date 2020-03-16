using System;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace
namespace Hydrogen.Entities.Tests
{
    public abstract class SingletonConverterHybridTestFixture : SingletonConversionTestFixture
    {
        protected static readonly BlobCreateAndAdd<LocalesInterfaceAuthoring, Locales, LocalesDefinition, LocalesConverter>
            k_CachedCreateInterfaceAuthoring =
                CreateInterfaceAuthoring<LocalesInterfaceAuthoring, Locales, LocalesDefinition, LocalesConverter>;

        protected static readonly BlobCreateAndAdd<LocalesCustomAuthoring, Locales, LocalesDefinition, LocalesConverter>
            k_CachedCreateCustomAuthoring = CreateCustomAuthoring<LocalesCustomAuthoring, Locales, LocalesDefinition, LocalesConverter>;

        protected static readonly Action<BlobRefData<Locales>, LocalesDefinition> k_CachedAssertMatchesLocales =
            AssertMatchesLocales;

        protected GameObjectConversionSettings MakeDefaultSettings() =>
            new GameObjectConversionSettings
            {
                DestinationWorld = World,
                ConversionFlags = GameObjectConversionUtility.ConversionFlags.AssignName
            };

        static void SetDataAuthoring<T0, T1, T2>(T0 authoring, T1 src, bool dontReplace)
            where T0 : SingletonConvertDataAuthoring<T1, T2>
            where T1 : struct, IComponentData
            where T2 : struct, ISingletonConverter<T1>
        {
            authoring.Source = src;
            authoring.DontReplaceIfLoaded = dontReplace;
        }

        protected static void SetBlobAuthoring<T0, T1, T2, T3>(T0 authoring, T2 src, bool dontReplace)
            where T0 : SingletonConvertBlobAuthoring<T1, T2, T3>
            where T1 : struct
            where T2 : ScriptableObject
            where T3 : struct, ISingletonConverter<BlobRefData<T1>>
        {
            authoring.Source = src;
            authoring.DontReplaceIfLoaded = dontReplace;
        }

        protected static T0 CreateDataAuthoring<T0, T1, T2>(string name, T1 src, bool dontReplace = false)
            where T0 : SingletonConvertDataAuthoring<T1, T2>
            where T1 : struct, IComponentData
            where T2 : struct, ISingletonConverter<T1>
        {
            var go = new GameObject(name);
            var authoring = go.AddComponent<T0>();
            SetDataAuthoring<T0, T1, T2>(authoring, src, dontReplace);

            return authoring;
        }

        protected static T0 CreateBlobAuthoring<T0, T1, T2, T3>(string name, T2 src, bool dontReplace = false)
            where T0 : SingletonConvertBlobAuthoring<T1, T2, T3>
            where T1 : struct
            where T2 : ScriptableObject
            where T3 : struct, ISingletonConverter<BlobRefData<T1>>
        {
            var go = new GameObject(name);
            var authoring = go.AddComponent<T0>();
            SetBlobAuthoring<T0, T1, T2, T3>(authoring, src, dontReplace);

            return authoring;
        }

        protected static T0 CreateInterfaceAuthoring<T0, T1, T2, T3>(string name, T2 src, bool dontReplace)
            where T0 : SingletonConvertBlobInterfaceAuthoring<T1, T2, T3>
            where T1 : struct
            where T2 : ScriptableObject, IConvertScriptableObjectToBlob<T1> 
            where T3 : struct, ISingletonConverter<BlobRefData<T1>> =>
            CreateBlobAuthoring<T0, T1, T2, T3>(name, src, dontReplace);

        protected static T0 CreateCustomAuthoring<T0, T1, T2, T3>(string name, T2 src, bool dontReplace)
            where T0 : SingletonConvertBlobCustomAuthoring<T1, T2, T3>
            where T1 : struct
            where T2 : ScriptableObject
            where T3 : struct, ISingletonConverter<BlobRefData<T1>> =>
            CreateBlobAuthoring<T0, T1, T2, T3>(name, src, dontReplace);

        protected delegate T0 BlobCreateAndAdd<out T0, T1, in T2, in T3>(string name, T2 src, bool dontReplace) 
            where T0 : SingletonConvertBlobAuthoring<T1, T2, T3> 
            where T1 : struct where T2 : ScriptableObject 
            where T3 : struct, ISingletonConverter<BlobRefData<T1>>;

        protected void AssertBlobConversion<T0, T1, T2, T3>(
            SingletonQueries query,
            string name,
            BlobCreateAndAdd<T0, T1, T2, T3> createAndAdd,
            bool dontReplace,
            Action<BlobRefData<T1>, T3> checkConverted,
            Action<BlobRefData<T1>, T2> checkMatchesSource,
            T2 expected)
            where T0 : SingletonConvertBlobAuthoring<T1, T2, T3>
            where T1 : struct
            where T2 : ScriptableObject
            where T3 : struct, ISingletonConverter<BlobRefData<T1>>
        {
            Assert.IsNotNull(createAndAdd);
            Assert.IsNotNull(checkConverted);
            Assert.IsNotNull(checkMatchesSource);

            T0 authoring = null;
            T3 converter = default;

            try
            {
                authoring = createAndAdd(name, expected, dontReplace);

                var entity = GameObjectConversionUtility.ConvertGameObjectHierarchy(authoring.gameObject, MakeDefaultSettings());

                Assert.IsTrue(m_Manager.HasComponent<T3>(entity));

                converter = m_Manager.GetComponentData<T3>(entity);

                World.Update();

                query.AssertCounts(0, 1, 1);

                var singleton = query.Singleton.GetSingleton<BlobRefData<T1>>();
                checkConverted(singleton, converter);
                checkMatchesSource(singleton, expected);
            }
            finally
            {
                Object.DestroyImmediate(expected);
                if(authoring != null)
                    Object.DestroyImmediate(authoring.gameObject);

                if (converter.Singleton.IsCreated)
                    converter.Singleton.Value.Dispose();
            }
        }

        protected static void AssertMatchesLocales(BlobRefData<Locales> data, LocalesDefinition definition)
        {
            ref Locales resolved = ref data.Resolve;
            ref BlobString name = ref resolved.Name;
            
            Assert.IsTrue(name.ToString() == definition.name);
            
            int localesLen = resolved.Available.Length;
            int definitionLen = definition.AvailableLocales.Length;

            Assert.IsTrue(localesLen == definitionLen);

            for (int i = 0; i < localesLen; i++)
            {
                ref BlobString locale = ref resolved.Available[i];
                string localeStr = locale.ToString();

                string defStr = definition.AvailableLocales[i];

                Assert.AreEqual(localeStr, defStr);
            }
        }

        protected void AssertTimeConfigAuthoring(Entity converterEntity, TimeConfig expected)
        {
            Assert.IsTrue(m_Manager.HasComponent<TimeConfigConverter>(converterEntity));

            World.Update();

            TimeConfigQueries.AssertCounts(0, 1, 1);

            var singleton = TimeConfigQueries.Singleton.GetSingleton<TimeConfig>();

            AssertTimeConfig(singleton, new TimeConfigConverter
            {
                Value = expected,
            });
        }
    }
}

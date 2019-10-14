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
        protected static readonly BlobCreateAndAdd<LocalesInterfaceAuthoring, Locales, LocalesDefinition>
            CachedCreateInterfaceAuthoring =
                CreateInterfaceAuthoring<LocalesInterfaceAuthoring, Locales, LocalesDefinition>;

        protected static readonly BlobCreateAndAdd<LocalesCustomAuthoring, Locales, LocalesDefinition>
            CachedCreateCustomAuthoring = CreateCustomAuthoring<LocalesCustomAuthoring, Locales, LocalesDefinition>;

        protected static readonly Action<BlobRefData<Locales>, LocalesDefinition> CachedAssertMatchesLocales =
            AssertMatchesLocales;
        
        private static void SetDataAuthoring<T0, T1>(T0 authoring, T1 src, bool dontReplace)
            where T0 : SingletonConvertDataAuthoring<T1>
            where T1 : struct, IComponentData
        {
            authoring.Source = src;
            authoring.DontReplaceIfLoaded = dontReplace;
        }

        protected static void SetBlobAuthoring<T0, T1, T2>(T0 authoring, T2 src, bool dontReplace)
            where T0 : SingletonConvertBlobAuthoring<T1, T2>
            where T1 : struct
            where T2 : ScriptableObject
        {
            authoring.Source = src;
            authoring.DontReplaceIfLoaded = dontReplace;
        }

        protected static T0 CreateDataAuthoring<T0, T1>(string name, T1 src, bool dontReplace = false)
            where T0 : SingletonConvertDataAuthoring<T1>
            where T1 : struct, IComponentData
        {
            var go = new GameObject(name);
            var authoring = go.AddComponent<T0>();
            SetDataAuthoring(authoring, src, dontReplace);

            return authoring;
        }

        protected static T0 CreateBlobAuthoring<T0, T1, T2>(string name, T2 src, bool dontReplace = false)
            where T0 : SingletonConvertBlobAuthoring<T1, T2>
            where T1 : struct
            where T2 : ScriptableObject
        {
            var go = new GameObject(name);
            var authoring = go.AddComponent<T0>();
            SetBlobAuthoring<T0, T1, T2>(authoring, src, dontReplace);

            return authoring;
        }

        protected static T0 CreateInterfaceAuthoring<T0, T1, T2>(string name, T2 src, bool dontReplace)
            where T0 : SingletonConvertBlobInterfaceAuthoring<T1, T2>
            where T1 : struct
            where T2 : ScriptableObject, IConvertScriptableObjectToBlob<T1> =>
            CreateBlobAuthoring<T0, T1, T2>(name, src, dontReplace);

        protected static T0 CreateCustomAuthoring<T0, T1, T2>(string name, T2 src, bool dontReplace)
            where T0 : SingletonConvertBlobCustomAuthoring<T1, T2>
            where T1 : struct
            where T2 : ScriptableObject =>
            CreateBlobAuthoring<T0, T1, T2>(name, src, dontReplace);

        protected delegate T0 BlobCreateAndAdd<out T0, T1, in T2>(string name, T2 src, bool dontReplace)
            where T0 : SingletonConvertBlobAuthoring<T1, T2>
            where T1 : struct
            where T2 : ScriptableObject;

        protected void AssertBlobConversion<T0, T1, T2>(
            SingletonQueries query,
            string name,
            BlobCreateAndAdd<T0, T1, T2> createAndAdd,
            bool dontReplace,
            Action<BlobRefData<T1>, SingletonConverter<BlobRefData<T1>>> checkConverted,
            Action<BlobRefData<T1>, T2> checkMatchesSource,
            T2 expected)
            where T0 : SingletonConvertBlobAuthoring<T1, T2>
            where T1 : struct
            where T2 : ScriptableObject
        {
            Assert.IsNotNull(createAndAdd);
            Assert.IsNotNull(checkConverted);
            Assert.IsNotNull(checkMatchesSource);

            T0 authoring = null;
            SingletonConverter<BlobRefData<T1>> converter = default;

            try
            {
                authoring = createAndAdd(name, expected, dontReplace);

                Entity entity = GameObjectConversionUtility.ConvertGameObjectHierarchy(authoring.gameObject, World);

                Assert.IsTrue(m_Manager.HasComponent<SingletonConverter<BlobRefData<T1>>>(entity));

                converter = m_Manager.GetComponentData<SingletonConverter<BlobRefData<T1>>>(entity);

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

                if (converter.Value.IsCreated)
                    converter.Value.Value.Dispose();
            }
        }

        protected static void AssertMatchesLocales(BlobRefData<Locales> data, LocalesDefinition definition)
        {
            ref Locales resolved = ref data.Resolve;
            int localesLen = resolved.Available.Length;

            int definitionLen = definition.AvailableLocales.Length;

            Assert.IsTrue(localesLen == definitionLen);

            ref NativeString64 defaultLocale = ref resolved.Default.Value;

            string defDefaultLocale = definition.AvailableLocales[0];

            Assert.AreEqual(defaultLocale.ToString(), defDefaultLocale);

            for (int i = 0; i < localesLen; i++)
            {
                ref NativeString64 locale = ref resolved.Available[i];
                string localeStr = locale.ToString();

                string defStr = definition.AvailableLocales[i];

                Assert.AreEqual(localeStr, defStr);
            }
        }

        protected void AssertTimeConfigAuthoring(Entity converterEntity, TimeConfig expected)
        {
            Assert.IsTrue(m_Manager.HasComponent<SingletonConverter<TimeConfig>>(converterEntity));

            World.Update();

            TimeConfigQueries.AssertCounts(0, 1, 1);

            var singleton = TimeConfigQueries.Singleton.GetSingleton<TimeConfig>();
            AssertTimeConfig(singleton, expected);
        }
    }
}

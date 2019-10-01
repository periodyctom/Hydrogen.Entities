using System;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;
using static Unity.Entities.GameObjectConversionUtility;
using Object = UnityEngine.Object;

// ReSharper disable CheckNamespace

namespace Hydrogen.Entities.Tests
{
    using TimeConfigConverter = SingletonConverter<TimeConfig>;
    using LocalesRef = BlobRefData<Locales>;
    using LocalesConverter = SingletonConverter<BlobRefData<Locales>>;

    [TestFixture]
    public class SingletonConverterBootstrapTests : SingletonConversionTestFixture
    {
        private static readonly BlobCreateAndAdd<LocalesInterfaceBootstrap, Locales, LocalesDefinition>
            sm_createInterfaceBootstrap =
                CreateInterfaceBootstrap<LocalesInterfaceBootstrap, Locales, LocalesDefinition>;

        private static readonly BlobCreateAndAdd<LocalesCustomBootstrap, Locales, LocalesDefinition>
            sm_createCustomBootstrap = CreateCustomBootstrap<LocalesCustomBootstrap, Locales, LocalesDefinition>;

        private static readonly Action<LocalesRef, LocalesDefinition> sm_assertMatchesLocales = AssertMatchesLocales;

        // TODO: Produce correct converter(s) from Prefabs
        // TODO: Load from pre-converted test subscene
        // TODO: Build new subscene, convert gos, close subscene, then load sub-scene, converters should be intact.

        private static void SetDataBootstrap<T0, T1>(T0 bootstrap, T1 src, bool dontReplace)
            where T0 : SingletonConverterDataBootstrap<T1>
            where T1 : struct, IComponentData
        {
            bootstrap.Source = src;
            bootstrap.DontReplaceIfLoaded = dontReplace;
        }

        private static void SetBlobBootstrap<T0, T1, T2>(T0 bootstrap, T2 src, bool dontReplace)
            where T0 : SingletonConverterBlobBootstrap<T1, T2>
            where T1 : struct
            where T2 : ScriptableObject
        {
            bootstrap.Source = src;
            bootstrap.DontReplaceIfLoaded = dontReplace;
        }

        private static T0 CreateDataBootstrap<T0, T1>(string name, T1 src, bool dontReplace = false)
            where T0 : SingletonConverterDataBootstrap<T1>
            where T1 : struct, IComponentData
        {
            var go = new GameObject(name);
            var bootstrap = go.AddComponent<T0>();
            SetDataBootstrap(bootstrap, src, dontReplace);

            return bootstrap;
        }

        private static T0 CreateBlobBootstrap<T0, T1, T2>(string name, T2 src, bool dontReplace = false)
            where T0 : SingletonConverterBlobBootstrap<T1, T2>
            where T1 : struct
            where T2 : ScriptableObject
        {
            var go = new GameObject(name);
            var bootstrap = go.AddComponent<T0>();
            SetBlobBootstrap<T0, T1, T2>(bootstrap, src, dontReplace);

            return bootstrap;
        }

        private static T0 CreateInterfaceBootstrap<T0, T1, T2>(string name, T2 src, bool dontReplace)
            where T0 : SingletonConverterBlobInterfaceBootstrap<T1, T2>
            where T1 : struct
            where T2 : ScriptableObject, IConvertScriptableObjectToBlob<T1> =>
            CreateBlobBootstrap<T0, T1, T2>(name, src, dontReplace);

        private static T0 CreateCustomBootstrap<T0, T1, T2>(string name, T2 src, bool dontReplace)
            where T0 : SingletonConverterBlobCustomBootstrap<T1, T2>
            where T1 : struct
            where T2 : ScriptableObject =>
            CreateBlobBootstrap<T0, T1, T2>(name, src, dontReplace);

        private delegate T0 BlobCreateAndAdd<out T0, T1, in T2>(string name, T2 src, bool dontReplace)
            where T0 : SingletonConverterBlobBootstrap<T1, T2>
            where T1 : struct
            where T2 : ScriptableObject;

        private void AssertBlobConversion<T0, T1, T2>(
            SingletonQueries query,
            string name,
            BlobCreateAndAdd<T0, T1, T2> createAndAdd,
            bool dontReplace,
            Action<BlobRefData<T1>, SingletonConverter<BlobRefData<T1>>> checkConverted,
            Action<BlobRefData<T1>, T2> checkMatchesSource,
            T2 expected)
            where T0 : SingletonConverterBlobBootstrap<T1, T2>
            where T1 : struct
            where T2 : ScriptableObject
        {
            Assert.IsNotNull(createAndAdd);
            Assert.IsNotNull(checkConverted);
            Assert.IsNotNull(checkMatchesSource);

            T0 bootstrap = null;
            SingletonConverter<BlobRefData<T1>> converter = default;

            try
            {
                bootstrap = createAndAdd(name, expected, dontReplace);

                Entity entity = ConvertGameObjectHierarchy(bootstrap.gameObject, World);

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
                Object.DestroyImmediate(bootstrap.gameObject);

                if (converter.Value.IsCreated)
                    converter.Value.Value.Dispose();
            }
        }

        private static void AssertMatchesLocales(LocalesRef data, LocalesDefinition definition)
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

        #region SimpleDataSingletons

        private void AssertTimeConfigBootstrap(Entity converterEntity, TimeConfig expected)
        {
            Assert.IsTrue(m_Manager.HasComponent<TimeConfigConverter>(converterEntity));

            World.Update();

            m_timeConfigs.AssertCounts(0, 1, 1);

            var singleton = m_timeConfigs.Singleton.GetSingleton<TimeConfig>();
            AssertTimeConfig(singleton, expected);
        }

        [Test]
        public void DataBootstrap_CreatesCorrectSingleton()
        {
            var expected = new TimeConfig(60, 1.0f / 60.0f);

            TimeConfigBootstrap bootstrap = CreateDataBootstrap<TimeConfigBootstrap, TimeConfig>(
                "TimeConfigBootstrap",
                expected);

            Entity convertedEntity = ConvertGameObjectHierarchy(bootstrap.gameObject, World);

            AssertTimeConfigBootstrap(convertedEntity, expected);
        }

        #endregion

        #region ScriptableObject to Blob (via Interface)

        [Test]
        public void ScriptableObjectSingleton_FromInterface_CreatesCorrectConverters()
        {
            AssertBlobConversion(
                m_locales,
                "LocalesInterfaceBootstrap",
                sm_createInterfaceBootstrap,
                false,
                m_assertSupportedLocales,
                sm_assertMatchesLocales,
                ScriptableObject.CreateInstance<LocalesDefinition>());
        }

        #endregion

        #region ScriptableObject to Blob (via function)

        [Test]
        public void ScriptableObjectSingleton_FromFunction_CreatesCorrectConverters()
        {
            AssertBlobConversion(
                m_locales,
                "LocalesInterfaceBootstrap",
                sm_createCustomBootstrap,
                false,
                m_assertSupportedLocales,
                sm_assertMatchesLocales,
                ScriptableObject.CreateInstance<LocalesDefinition>());
        }

        #endregion

        #region Load converters from prefabs

        [Test]
        public void DataSingletons_LoadConvertersFromPrefab()
        {
            GameObject prefab = TestUtilities.LoadPrefab("TimeConfigBootstrap");
            TimeConfig expected = prefab.GetComponent<TimeConfigBootstrap>().Source;

            Entity prefabEntity = ConvertGameObjectHierarchy(prefab, World);
            Entity converterEntity = m_Manager.Instantiate(prefabEntity);

            AssertTimeConfigBootstrap(converterEntity, expected);
        }

        [Test]
        public void ScriptableObjectSingleton_LoadConvertersFromPrefab()
        {
            GameObject prefab = TestUtilities.LoadPrefab("LocalesInterfaceBootstrap");

            LocalesDefinition expected = prefab.GetComponent<LocalesInterfaceBootstrap>().Source;

            Entity interfacePrefabEntity = ConvertGameObjectHierarchy(prefab, World);
            Entity interfaceInstance = m_Manager.Instantiate(interfacePrefabEntity);

            Assert.IsTrue(m_Manager.HasComponent<LocalesConverter>(interfaceInstance));

            m_locales.AssertCounts(1, 0, 0);

            World.Update();

            m_locales.AssertCounts(0, 1, 1);

            var singleton = m_locales.Singleton.GetSingleton<LocalesRef>();
            sm_assertMatchesLocales.Invoke(singleton, expected);

            prefab = TestUtilities.LoadPrefab("LocalesCustomBootstrap");
            expected = prefab.GetComponent<LocalesCustomBootstrap>().Source;

            Entity customPrefabEntity = ConvertGameObjectHierarchy(prefab, World);

            World.Update();

            m_locales.AssertCounts(0, 0, 1);

            Entity customInstance = m_Manager.Instantiate(customPrefabEntity);

            Assert.IsTrue(m_Manager.HasComponent<LocalesConverter>(customInstance));

            m_locales.AssertCounts(1, 0, 1);

            World.Update();

            m_locales.AssertCounts(0, 1, 1);

            singleton = m_locales.Singleton.GetSingleton<LocalesRef>();
            sm_assertMatchesLocales.Invoke(singleton, expected);

            World.Update();

            m_locales.AssertCounts(0, 0, 1);
        }
        
        [Test, Ignore("Not implemented!")]
        public void Singletons_CanLoadUserCreatedSubscene_AtRuntime() { }

        #endregion
        
        // TODO: Move to an editor-only test assembly.
        // #region Subscene tests
        //
        // [UnityTest]
        // public IEnumerator EndToEnd_CanCreateConvertersInSubscene_AndLoadFromSubscene()
        // {
        //     GUID guid = GUID.Generate();
        //     Scene temp = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        //     EditorSceneManager.SetActiveScene(temp);
        //
        //     var expectedTimeConfig = new TimeConfig(120, 1.0f / 120.0f);
        //
        //     TimeConfigBootstrap timeConfigBootstrap = CreateDataBootstrap<TimeConfigBootstrap, TimeConfig>(
        //         "TestTimeConfig",
        //         expectedTimeConfig);
        //
        //     Assert.IsTrue(timeConfigBootstrap.gameObject.scene == temp);
        //
        //     GameObject prefab = TestUtilities.LoadPrefab("TestSupportedLocalesCustomBootstrap");
        //
        //     LocalesDefinition expectedLocales = prefab.GetComponent<LocalesCustomBootstrap>().Source;
        //
        //     GameObject instance = Object.Instantiate(prefab);
        //     Assert.IsTrue(instance.scene == temp);
        //
        //     SceneData[] entitySceneData = EditorEntityScenes.WriteEntityScene(temp, guid);
        //     Assert.IsTrue(1 == entitySceneData.Length);
        //
        //     Entity sceneEntity = m_Manager.CreateEntity();
        //     m_Manager.AddComponentData(sceneEntity, entitySceneData[0]);
        //     m_Manager.AddComponentData(sceneEntity, new RequestSceneLoaded());
        //
        //     for (int i = 0; i < 1000; i++)
        //     {
        //         World.GetOrCreateSystem<SubSceneStreamingSystem>().Update();
        //
        //         if (m_locales.PreConverted.CalculateEntityCount() == 0
        //          || m_timeConfigs.PreConverted.CalculateEntityCount() == 0)
        //             break;
        //
        //         yield return null;
        //     }
        //
        //     m_locales.AssertCounts(1, 0, 0);
        //     m_timeConfigs.AssertCounts(1, 0, 0);
        //
        //     World.Update();
        //
        //     m_locales.AssertCounts(0, 1, 1);
        //     m_timeConfigs.AssertCounts(0, 1, 1);
        //
        //     var timeConfigSingleton = m_timeConfigs.Singleton.GetSingleton<TimeConfig>();
        //     AssertTimeConfig(timeConfigSingleton, expectedTimeConfig);
        //
        //     var testLocalesSingleton = m_locales.Singleton.GetSingleton<LocalesRef>();
        //     sm_assertMatchesLocales.Invoke(testLocalesSingleton, expectedLocales);
        //
        //     m_Manager.RemoveComponent<RequestSceneLoaded>(sceneEntity);
        //
        //     uint subSceneStreamingVersion = World.GetOrCreateSystem<SubSceneStreamingSystem>().LastSystemVersion;
        //
        //     World.Update();
        //
        //     Assert.IsTrue(
        //         subSceneStreamingVersion < World.GetOrCreateSystem<SubSceneStreamingSystem>().LastSystemVersion);
        //
        //     m_timeConfigs.AssertCounts(0, 0, 1);
        //     m_locales.AssertCounts(0, 0, 1);
        //
        //     timeConfigSingleton = m_timeConfigs.Singleton.GetSingleton<TimeConfig>();
        //     AssertTimeConfig(timeConfigSingleton, expectedTimeConfig);
        //
        //     testLocalesSingleton = m_locales.Singleton.GetSingleton<LocalesRef>();
        //     sm_assertMatchesLocales.Invoke(testLocalesSingleton, expectedLocales);
        //
        //     Entity dontReplace = m_Manager.CreateEntity(typeof(TimeConfigConverter));
        //     Entity doReplace = m_Manager.CreateEntity(typeof(TimeConfigConverter));
        //
        //     expectedTimeConfig = new TimeConfig(60, 1.0f / 60.0f);
        //     var unexpectedTimeConfig = new TimeConfig(30, 1.0f / 30.0f);
        //
        //     m_Manager.SetComponentData(dontReplace, new TimeConfigConverter(unexpectedTimeConfig, true));
        //     m_Manager.SetComponentData(doReplace, new TimeConfigConverter(expectedTimeConfig));
        //
        //     m_locales.AssertCounts(0, 0, 1);
        //     m_timeConfigs.AssertCounts(2, 0, 1);
        //
        //     World.Update();
        //
        //     m_locales.AssertCounts(0, 0, 1);
        //     m_timeConfigs.AssertCounts(0, 2, 1);
        //     timeConfigSingleton = m_timeConfigs.Singleton.GetSingleton<TimeConfig>();
        //     AssertTimeConfig(timeConfigSingleton, expectedTimeConfig);
        //
        //     World.Update();
        //
        //     m_locales.AssertCounts(0, 0, 1);
        //     m_timeConfigs.AssertCounts(0, 0, 1);
        // }
        //
        // #endregion
    }
}

using NUnit.Framework;
using Unity.Entities;
using UnityEngine;
using static Unity.Entities.GameObjectConversionUtility;
using static UnityEngine.ScriptableObject;


namespace Hydrogen.Entities.Tests
{
    using LocalesRef = BlobRefData<Locales>;
    using LocalesConverter = SingletonConverter<BlobRefData<Locales>>;

    [TestFixture]
    public class SingletonConverterAuthoringTests : SingletonConverterHybridTestFixture
    {
        [Test]
        public void DataAuthoring_CreatesCorrectSingleton()
        {
            var expected = new TimeConfig(60, 1.0f / 60.0f);

            TimeConfigAuthoring authoring = CreateDataAuthoring<TimeConfigAuthoring, TimeConfig>(
                "TimeConfigAuthoring",
                expected);

            Entity convertedEntity = ConvertGameObjectHierarchy(authoring.gameObject, MakeDefaultSettings());

            AssertTimeConfigAuthoring(convertedEntity, expected);
        }

        [Test]
        public void ScriptableObjectSingleton_FromInterface_CreatesCorrectConverters()
        {
            AssertBlobConversion(
                LocalesQueries,
                "LocalesInterfaceAuthoring",
                CachedCreateInterfaceAuthoring,
                false,
                CachedAssertSupportedLocales,
                CachedAssertMatchesLocales,
                CreateInstance<LocalesDefinition>());
        }

        [Test]
        public void ScriptableObjectSingleton_FromFunction_CreatesCorrectConverters()
        {
            AssertBlobConversion(
                LocalesQueries,
                "LocalesInterfaceAuthoring",
                CachedCreateCustomAuthoring,
                false,
                CachedAssertSupportedLocales,
                CachedAssertMatchesLocales,
                CreateInstance<LocalesDefinition>());
        }

        [Test]
        public void DataSingletons_LoadConvertersFromPrefab()
        {
            GameObject prefab = TestUtilities.LoadPrefab("TimeConfigAuthoring");
            TimeConfig expected = prefab.GetComponent<TimeConfigAuthoring>().Source;

            Entity prefabEntity = ConvertGameObjectHierarchy(prefab, MakeDefaultSettings());
            Entity converterEntity = m_Manager.Instantiate(prefabEntity);

            AssertTimeConfigAuthoring(converterEntity, expected);
        }

        [Test]
        public void ScriptableObjectSingleton_LoadConvertersFromPrefab()
        {
            GameObject prefab = TestUtilities.LoadPrefab("LocalesInterfaceAuthoring");

            LocalesDefinition expected = prefab.GetComponent<LocalesInterfaceAuthoring>().Source;

            Entity interfacePrefabEntity = ConvertGameObjectHierarchy(prefab, MakeDefaultSettings());
            Entity interfaceInstance = m_Manager.Instantiate(interfacePrefabEntity);

            Assert.IsTrue(m_Manager.HasComponent<LocalesConverter>(interfaceInstance));

            LocalesQueries.AssertCounts(1, 0, 0);

            World.Update();

            LocalesQueries.AssertCounts(0, 1, 1);

            var singleton = LocalesQueries.Singleton.GetSingleton<LocalesRef>();
            CachedAssertMatchesLocales.Invoke(singleton, expected);

            prefab = TestUtilities.LoadPrefab("LocalesCustomAuthoring");
            expected = prefab.GetComponent<LocalesCustomAuthoring>().Source;

            Entity customPrefabEntity = ConvertGameObjectHierarchy(prefab, MakeDefaultSettings());

            World.Update();

            LocalesQueries.AssertCounts(0, 0, 1);

            Entity customInstance = m_Manager.Instantiate(customPrefabEntity);

            Assert.IsTrue(m_Manager.HasComponent<LocalesConverter>(customInstance));

            LocalesQueries.AssertCounts(1, 0, 1);

            World.Update();

            LocalesQueries.AssertCounts(0, 1, 1);

            singleton = LocalesQueries.Singleton.GetSingleton<LocalesRef>();
            CachedAssertMatchesLocales.Invoke(singleton, expected);

            World.Update();

            LocalesQueries.AssertCounts(0, 0, 1);
        }
    }
}

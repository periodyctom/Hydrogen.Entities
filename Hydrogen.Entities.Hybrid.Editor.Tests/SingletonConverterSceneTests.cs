using System.Collections;
using NUnit.Framework;
using Unity.Entities;
using Unity.Scenes;
using Unity.Scenes.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hydrogen.Entities.Tests
{
    using TimeConfigConverter = SingletonConverter<TimeConfig>;
    using LocalesRef = BlobRefData<Locales>;
    using LocalesConverter = SingletonConverter<BlobRefData<Locales>>;
    
    [TestFixture]
    public class SingletonConverterSceneTests : SingletonConverterHybridTestFixture
    {
        [UnityTest]
        public IEnumerator EndToEnd_CanCreateConvertersInSubscene_AndLoadFromSubscene()
        {
            GUID guid = GUID.Generate();
            Scene temp = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            EditorSceneManager.SetActiveScene(temp);
        
            var expectedTimeConfig = new TimeConfig(120, 1.0f / 120.0f);
        
            TimeConfigAuthoring timeConfigAuthoring = CreateDataAuthoring<TimeConfigAuthoring, TimeConfig>(
                "TestTimeConfig",
                expectedTimeConfig);
        
            Assert.IsTrue(timeConfigAuthoring.gameObject.scene == temp);

            GameObject prefab = TestUtilities.LoadPrefab("LocalesCustomAuthoring");
        
            LocalesDefinition expectedLocales = prefab.GetComponent<LocalesCustomAuthoring>().Source;
        
            GameObject instance = Object.Instantiate(prefab);
            Assert.IsTrue(instance.scene == temp);
        
            SceneData[] entitySceneData = EditorEntityScenes.WriteEntityScene(temp, guid);
            Assert.IsTrue(1 == entitySceneData.Length);
        
            Entity sceneEntity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(sceneEntity, entitySceneData[0]);
            m_Manager.AddComponentData(sceneEntity, new RequestSceneLoaded());
        
            for (int i = 0; i < 1000; i++)
            {
                World.GetOrCreateSystem<SubSceneStreamingSystem>().Update();
        
                if (LocalesQueries.PreConverted.CalculateEntityCount() == 1
                 && TimeConfigQueries.PreConverted.CalculateEntityCount() == 1)
                    break;
        
                yield return null;
            }
        
            LocalesQueries.AssertCounts(1, 0, 0);
            TimeConfigQueries.AssertCounts(1, 0, 0);
        
            World.Update();
        
            LocalesQueries.AssertCounts(0, 1, 1);
            TimeConfigQueries.AssertCounts(0, 1, 1);
        
            var timeConfigSingleton = TimeConfigQueries.Singleton.GetSingleton<TimeConfig>();
            AssertTimeConfig(timeConfigSingleton, expectedTimeConfig);
        
            var testLocalesSingleton = LocalesQueries.Singleton.GetSingleton<LocalesRef>();
            CachedAssertMatchesLocales.Invoke(testLocalesSingleton, expectedLocales);

            m_Manager.RemoveComponent<RequestSceneLoaded>(sceneEntity);
            
            World.Update();
        
            TimeConfigQueries.AssertCounts(0, 0, 1);
            LocalesQueries.AssertCounts(0, 0, 1);
        
            timeConfigSingleton = TimeConfigQueries.Singleton.GetSingleton<TimeConfig>();
            AssertTimeConfig(timeConfigSingleton, expectedTimeConfig);
        
            testLocalesSingleton = LocalesQueries.Singleton.GetSingleton<LocalesRef>();
            CachedAssertMatchesLocales.Invoke(testLocalesSingleton, expectedLocales);
        
            Entity dontReplace = m_Manager.CreateEntity(typeof(TimeConfigConverter));
            Entity doReplace = m_Manager.CreateEntity(typeof(TimeConfigConverter));
        
            expectedTimeConfig = new TimeConfig(60, 1.0f / 60.0f);
            var unexpectedTimeConfig = new TimeConfig(30, 1.0f / 30.0f);
        
            m_Manager.SetComponentData(dontReplace, new TimeConfigConverter(unexpectedTimeConfig, true));
            m_Manager.SetComponentData(doReplace, new TimeConfigConverter(expectedTimeConfig));
        
            LocalesQueries.AssertCounts(0, 0, 1);
            TimeConfigQueries.AssertCounts(2, 0, 1);
        
            World.Update();
        
            LocalesQueries.AssertCounts(0, 0, 1);
            TimeConfigQueries.AssertCounts(0, 2, 1);
            timeConfigSingleton = TimeConfigQueries.Singleton.GetSingleton<TimeConfig>();
            AssertTimeConfig(timeConfigSingleton, expectedTimeConfig);
        
            World.Update();
        
            LocalesQueries.AssertCounts(0, 0, 1);
            TimeConfigQueries.AssertCounts(0, 0, 1);
        }
    }
}

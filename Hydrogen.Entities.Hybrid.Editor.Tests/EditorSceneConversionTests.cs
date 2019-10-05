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

namespace Hydrogen.Entities.Tests
{
    using TimeConfigConverter = SingletonConverter<TimeConfig>;
    using LocalesRef = BlobRefData<Locales>;
    using LocalesConverter = SingletonConverter<BlobRefData<Locales>>;
    
    [TestFixture]
    public class EditorSceneConversionTests : SingletonConverterHybridTestFixture
    {
        [UnityTest]
        public IEnumerator EndToEnd_CanCreateConvertersInSubscene_AndLoadFromSubscene()
        {
            GUID guid = GUID.Generate();
            Scene temp = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            EditorSceneManager.SetActiveScene(temp);
        
            var expectedTimeConfig = new TimeConfig(120, 1.0f / 120.0f);
        
            TimeConfigBootstrap timeConfigBootstrap = CreateDataBootstrap<TimeConfigBootstrap, TimeConfig>(
                "TestTimeConfig",
                expectedTimeConfig);
        
            Assert.IsTrue(timeConfigBootstrap.gameObject.scene == temp);

            GameObject prefab = TestUtilities.LoadPrefab("LocalesCustomBootstrap");
        
            LocalesDefinition expectedLocales = prefab.GetComponent<LocalesCustomBootstrap>().Source;
        
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
        
                if (m_locales.PreConverted.CalculateEntityCount() == 1
                 && m_timeConfigs.PreConverted.CalculateEntityCount() == 1)
                    break;
        
                yield return null;
            }
        
            m_locales.AssertCounts(1, 0, 0);
            m_timeConfigs.AssertCounts(1, 0, 0);
        
            World.Update();
        
            m_locales.AssertCounts(0, 1, 1);
            m_timeConfigs.AssertCounts(0, 1, 1);
        
            var timeConfigSingleton = m_timeConfigs.Singleton.GetSingleton<TimeConfig>();
            AssertTimeConfig(timeConfigSingleton, expectedTimeConfig);
        
            var testLocalesSingleton = m_locales.Singleton.GetSingleton<LocalesRef>();
            sm_assertMatchesLocales.Invoke(testLocalesSingleton, expectedLocales);
            
            // the blob reference will cease to be valid. Should converter copy or programmer be responsible?
            m_Manager.DestroyEntity(m_locales.Singleton);
        
            m_Manager.RemoveComponent<RequestSceneLoaded>(sceneEntity);
            
            World.Update();
        
            m_timeConfigs.AssertCounts(0, 0, 1);
            m_locales.AssertCounts(0, 0, 0);
        
            timeConfigSingleton = m_timeConfigs.Singleton.GetSingleton<TimeConfig>();
            AssertTimeConfig(timeConfigSingleton, expectedTimeConfig);
        
            // testLocalesSingleton = m_locales.Singleton.GetSingleton<LocalesRef>();
            // sm_assertMatchesLocales.Invoke(testLocalesSingleton, expectedLocales);
        
            Entity dontReplace = m_Manager.CreateEntity(typeof(TimeConfigConverter));
            Entity doReplace = m_Manager.CreateEntity(typeof(TimeConfigConverter));
        
            expectedTimeConfig = new TimeConfig(60, 1.0f / 60.0f);
            var unexpectedTimeConfig = new TimeConfig(30, 1.0f / 30.0f);
        
            m_Manager.SetComponentData(dontReplace, new TimeConfigConverter(unexpectedTimeConfig, true));
            m_Manager.SetComponentData(doReplace, new TimeConfigConverter(expectedTimeConfig));
        
            // m_locales.AssertCounts(0, 0, 1);
            m_timeConfigs.AssertCounts(2, 0, 1);
        
            World.Update();
        
            // m_locales.AssertCounts(0, 0, 1);
            m_timeConfigs.AssertCounts(0, 2, 1);
            timeConfigSingleton = m_timeConfigs.Singleton.GetSingleton<TimeConfig>();
            AssertTimeConfig(timeConfigSingleton, expectedTimeConfig);
        
            World.Update();
        
            // m_locales.AssertCounts(0, 0, 1);
            m_timeConfigs.AssertCounts(0, 0, 1);
        }
    }
}

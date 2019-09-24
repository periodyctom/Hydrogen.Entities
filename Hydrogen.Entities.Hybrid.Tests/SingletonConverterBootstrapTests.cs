using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace Hydrogen.Entities.Tests
{
    [TestFixture]
    public class SingletonConverterBootstrapTests : SingletonConversionTestFixture
    {
        // TODO: Produce correct converter from Data (no opts, dont replace)
        // TODO: Produce correct converter from SO -> Blob (interface) (no opts, dont replace)
        // TODO: Produce correct converter from SO -> Blob (function) (no opts, dont replace)
        // TODO: Produce correct converter(s) from Prefabs (no opts, dont replace)
        // TODO: Load from pre-converted test subscene
        // TODO: Build new subscene, convert gos, close subscene, then load sub-scene, converters should be intact.

        #region SimpleDataSingletons

        private static void SetDataBootstrap<T0, T1>(T0 bootstrap, T1 src, bool dontReplace)
            where T0 : SingletonConverterDataBootstrap<T1>
            where T1 : struct, IComponentData
        {
            bootstrap.Source = src;
            bootstrap.DontReplaceIfLoaded = dontReplace;
        }

        private static T0 CreateAndAddDataBootstrap<T0, T1>(string name, T1 src, bool dontReplace = false)
            where T0 : SingletonConverterDataBootstrap<T1>
            where T1 : struct, IComponentData
        {
            var go = new GameObject(name);
            var bootstrap = go.AddComponent<T0>();
            SetDataBootstrap(bootstrap, src, dontReplace);

            return bootstrap;
        }

        [Test, Ignore("Not implemented")]
        public void DataBootstrap_CreatesCorrectSingleton()
        {
            var expected = new TestTimeConfig(60, 1.0f / 60.0f);
            
            TestTimeConfigBootstrap bootstrap = CreateAndAddDataBootstrap<TestTimeConfigBootstrap, TestTimeConfig>(
                "TestTimeConfigBootstrap",
                expected);

            try
            {
                World.Update();
            
                m_testTimeConfigs.AssertCounts(0, 1, 1);

                var singleton = m_testTimeConfigs.Singleton.GetSingleton<TestTimeConfig>();
                AssertTimeConfig(singleton, expected);
            }
            finally
            {
                Object.DestroyImmediate(bootstrap.gameObject);
            }
        }

        #endregion

        #region ScriptableObject to Blob (via Interface)
        
        [Test, Ignore("Not implemented")]
        public void ScriptableObjectSingleton_FromInterface_CreatesCorrectConverters()
        {
            
        }

        #endregion

        #region ScriptableObject to Blob (via function)
        
        [Test, Ignore("Not implemented")]
        public void ScriptableObjectSingleton_FromFunction_CreatesCorrectConverters()
        {
            
        }

        #endregion

        #region Load converter from prefabs

        // Data singleton load from prefab (all kinds)
        
        // Scriptable singleton load from prefab all kinds

        [Test, Ignore("Not implemented")]
        public void DataSingletons_LoadConvertersFromPrefab()
        {
            
        }

        [Test, Ignore("Not implemented")]
        public void ScriptableObjectSingleton_LoadConvertersFromPrefab()
        {
            
        }

        #endregion
        
        #region Subscene tests

        [Test, Ignore("Not implemented")]
        public void Singletons_CanConvertToSubscene()
        {
            
        }

        [Test, Ignore("Not implemented")]
        public void Singletons_CanLoadExistingSubscene()
        {
            
        }

        [Test, Ignore("Not implemented")]
        public void Singletons_CanCreateConvertersInSubscene_AndLoadFromSubscene()
        {
            
        }
        
        #endregion
    }
}

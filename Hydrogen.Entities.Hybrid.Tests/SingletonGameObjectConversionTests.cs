using NUnit.Framework;
using Unity.Entities.Tests;
// ReSharper disable CheckNamespace

namespace Hydrogen.Entities.Tests
{
    [TestFixture]
    public class SingletonGameObjectConversionTests : ECSTestsFixture
    {
        // TODO: Produce correct converter from Data (no opts, replace only, refresh only, both)
        // TODO: Produce correct converter from SO -> Blob (interface) (no opts, replace only, refresh only, both)
        // TODO: Produce correct converter from SO -> Blob (function) (no opts, replace only, refresh only, both)
        // TODO: Produce correct converter(s) from Prefabs (no opts, replace only, refresh only, both)
        // TODO: Load from pre-converted test subscene
        // TODO: Build new subscene, convert gos, close subscene, then load sub-scene, converters should be intact.

        #region SimpleDataSingletons

        [Test, Ignore("Not implemented")]
        public void DataSingleton_CreatesCorrectConverters()
        {
            
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
        public void DataSingletons_LoadConvertersFromSubscene()
        {
            
        }

        [Test, Ignore("Not implemented")]
        public void ScriptableObjectSingletons_LoadConvertersFromSubscene()
        {
            
        }

        [Test, Ignore("Not implemented")]
        public void Singletons_CanCreateConvertersInSubscene_AndLoadFromSubscene()
        {
            
        }
        
        #endregion
    }
}

using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Assertions;

namespace Hydrogen.Entities.Tests
{
    [RequiresEntityConversion]
    [DisallowMultipleComponent]
    public class PrefabCollectionBehaviour : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
    {
        public PrefabCollection Collection;
        
        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
            if (Collection != null)
                Collection.DeclareReferencedPrefabs(referencedPrefabs);
        }

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            if(Collection == null) return;
            
            var scriptConversion = conversionSystem.World.GetOrCreateSystem<ScriptableObjectConversionSystem>();

            BlobAssetReference<PrefabCollectionBlob> prefabs =
                scriptConversion.GetBlob<PrefabCollection, PrefabCollectionBlob>(Collection);

            PrefabCollectionReference reference = default;
            reference.Value = prefabs;
            
            dstManager.AddComponentData(entity, reference);
            
            Assert.IsTrue(dstManager.HasComponent<PrefabCollectionReference>(entity));
        }
    }
}

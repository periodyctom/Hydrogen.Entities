using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Assertions;

namespace Hydrogen.Entities.Tests
{
    [RequiresEntityConversion]
    [DisallowMultipleComponent]
    [AddComponentMenu("Hidden/DontUse")]
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

            BlobRefData<PrefabCollectionBlob> @ref = default;
            @ref.Value = prefabs;
            
            dstManager.AddComponentData(entity, @ref);

            Assert.IsTrue(dstManager.HasComponent<BlobRefData<PrefabCollectionBlob>>(entity));
        }
    }
}

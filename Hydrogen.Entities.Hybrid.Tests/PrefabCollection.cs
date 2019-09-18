using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Hydrogen.Entities.Tests
{
    [CreateAssetMenu(menuName = "Hydrogen/Entities/Tests")]
    public class PrefabCollection : ScriptableObject, IConvertScriptableObjectToBlob<PrefabCollectionBlob>, 
                                    IDeclareReferencedPrefabs
    {
        public GameObject[] Prefabs;

        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
            int prefabsLen = Prefabs.Length;

            for (int i = 0; i < prefabsLen; i++)
            {
                GameObject gameObject = Prefabs[i];

                if(!referencedPrefabs.Contains(gameObject))
                    referencedPrefabs.Add(gameObject);
            }
        } 

        public BlobAssetReference<PrefabCollectionBlob> Convert(ScriptableObjectConversionSystem conversion)
        {
            var builder = new BlobBuilder(Allocator.Temp);

            ref PrefabCollectionBlob target = ref builder.ConstructRoot<PrefabCollectionBlob>();

            int prefabsLen = Prefabs.Length;

            if (prefabsLen > 0)
            {
                GameObjectConversionSystem goConversion = conversion.GoConversionSystem;
                    
                BlobBuilderArray<Entity> arrayBuilder = builder.Allocate(ref target.Prefabs, prefabsLen);

                for (int i = 0; i < prefabsLen; i++)
                {
                    GameObject prefab = Prefabs[i];
                    ref Entity e = ref arrayBuilder[i];
                    e = goConversion.GetPrimaryEntity(prefab);
                }
            }
            else
            {
                target.Prefabs = new BlobArray<Entity>();
            }
                
                
            BlobAssetReference<PrefabCollectionBlob> result =
                builder.CreateBlobAssetReference<PrefabCollectionBlob>(Allocator.Persistent);
                
            builder.Dispose();

            return result;
        }
    }
}

using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace Hydrogen.Entities
{
    [DisallowMultipleComponent]
    public class PostConversionAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        [SerializeField] 
        [FormerlySerializedAs("m_ConvertActions")] 
        PostConvertOperation[] m_Operations;

        public IEnumerable<PostConvertOperation> Operations => m_Operations;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentObject(entity, this);
        }
    }
}

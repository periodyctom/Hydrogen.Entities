using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;
using UnityEngine;

namespace Hydrogen.Entities
{
    public class PostConversionAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        [SerializeField] PostConvertOperation[] m_ConvertActions;

        public PostConvertOperation[] ConvertActions => m_ConvertActions;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentObject(entity, this);
        }
    }

    [UpdateAfter(typeof(SceneSystemGroup))]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class PostConvertOperationSystem : SystemBase
    {
        EntityQuery m_Query;
        
        protected override void OnCreate()
        {
            m_Query = GetEntityQuery(typeof(PostConversionAuthoring));
            
            RequireForUpdate(m_Query);
        }

        protected override void OnUpdate()
        {
            CompleteDependency();
            
            var entities = m_Query.ToEntityArray(Allocator.Temp);
            var entitiesLength = entities.Length;

            for (var i = 0; i < entitiesLength; i++)
            {
                var entity = entities[i];
                var postConversion = EntityManager.GetComponentObject<PostConversionAuthoring>(entity);
                var operations = postConversion.ConvertActions;
                foreach (var action in operations)
                    action.Perform(EntityManager);

                EntityManager.DestroyEntity(entity);
            }
        }
    }
}

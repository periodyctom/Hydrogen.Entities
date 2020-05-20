using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;

namespace Hydrogen.Entities
{
    [UpdateAfter(typeof(SceneSystemGroup))]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    // ReSharper disable once UnusedType.Global
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
            
            var entities = m_Query.ToEntityArray(Allocator.TempJob);
            var entitiesLength = entities.Length;

            for (var i = 0; i < entitiesLength; i++)
            {
                var entity = entities[i];
                var postConversion = EntityManager.GetComponentObject<PostConversionAuthoring>(entity);
                var operations = postConversion.Operations;
                foreach (var action in operations)
                    action.Perform(EntityManager);

                EntityManager.DestroyEntity(entity);
            }

            entities.Dispose();
        }
    }
}

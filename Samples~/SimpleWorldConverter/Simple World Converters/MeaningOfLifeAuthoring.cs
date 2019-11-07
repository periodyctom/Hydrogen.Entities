using Unity.Entities;
using Hydrogen.Entities;
using UnityEngine;

[assembly: RegisterGenericComponentType(typeof(SingletonConverter<MeaningOfLifeData>))]

namespace Hydrogen.Entities
{
    public class MeaningOfLifeAuthoring : SingletonConvertDataAuthoring<MeaningOfLifeData>
    {
    }

    public struct MeaningOfLifeData : IComponentData
    {
        public int Value;
    }

    public sealed class MeaningOfLifeConvertSystem : SingletonConvertSystem<MeaningOfLifeData> {}

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(MeaningOfLifeConvertSystem))]
    public sealed class MeaningOfLifeLoadedSystem : ComponentSystem
    {
        protected override void OnCreate()
        {
            RequireForUpdate(
                GetEntityQuery(
                    ComponentType.ReadOnly<SingletonConverter<MeaningOfLifeData>>(),
                    ComponentType.ReadOnly<SingletonConverted>()));
            
            RequireSingletonForUpdate<MeaningOfLifeData>();
        }

        protected override void OnUpdate()
        {
            var meaningOfLife = GetSingleton<MeaningOfLifeData>();
            Debug.Log($"The meaning of life is: {meaningOfLife.Value:D}");
        }
    }
}
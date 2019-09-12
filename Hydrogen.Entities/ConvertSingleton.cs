using System;
using Unity.Entities;

namespace Hydrogen.Entities
{
    [Serializable]
    public struct SingletonBlobConverter<T0, T1> : IComponentData
        where T0 : unmanaged, IBlobReferenceData<T1>
        where T1 : unmanaged
    {
        public T0 Value;
    }

    [Serializable]
    public struct SingletonDataConverter<T> : IComponentData
        where T : unmanaged, IComponentData
    {
        public T Value;
    }

    public struct SingletonRefresh<T> : IComponentData
        where T : struct, IComponentData { }

    public struct SingletonDontReplace : IComponentData { }

    public struct SingletonRequiresRefresh : IComponentData { }
}

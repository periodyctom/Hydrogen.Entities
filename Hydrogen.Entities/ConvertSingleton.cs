using Unity.Entities;

namespace Hydrogen.Entities
{
    public struct ConfigSingletonLoader<T> where T : struct, IComponentData
    {
        public T Value;
    }

    public struct ConfigRefSingletonLoader<T0, T1>
        where T0 : struct, IConfigRef<T1>
        where T1 : struct
    {
        public T0 Value;
    }
}

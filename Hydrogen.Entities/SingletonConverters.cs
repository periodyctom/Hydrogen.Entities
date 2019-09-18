using System;
using System.Runtime.CompilerServices;
using Unity.Entities;

namespace Hydrogen.Entities
{
    public interface ISingletonConverter<out T> : IComponentData
        where T : struct
    {
        T Singleton { get; }
    }
    
    [Serializable]
    public struct SingletonBlobConverter<T0, T1> : ISingletonConverter<T0>
        where T0 : struct, IBlobReferenceData<T1>
        where T1 : struct
    {
        public T0 Value;

        public T0 Singleton
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SingletonBlobConverter(T0 value) => Value = value;

        public static explicit operator T0(SingletonBlobConverter<T0, T1> @this) => @this.Value;
    }

    [Serializable]
    public struct SingletonDataConverter<T> : ISingletonConverter<T>
        where T : struct, IComponentData
    {
        public T Value;

        public T Singleton
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SingletonDataConverter(T value) => Value = value;

        public static explicit operator T(SingletonDataConverter<T> @this) => @this.Value;
    }

    public struct SingletonRefresh<T> : IComponentData
        where T : struct, IComponentData { }

    public struct SingletonDontReplace : IComponentData { }

    public struct SingletonRequiresRefresh : IComponentData { }
}

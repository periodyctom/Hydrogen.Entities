using System;
using System.Runtime.CompilerServices;
using Unity.Entities;
// ReSharper disable CheckNamespace

namespace Hydrogen.Entities
{
    [Serializable]
    public struct SingletonConverter<T> : IComponentData
    {
        public T Value;
        public bool DontReplace;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SingletonConverter(T value, bool dontReplace = false)
        {
            Value = value;
            DontReplace = dontReplace;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator T(SingletonConverter<T> @this) => @this.Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator SingletonConverter<T>(T value) => new SingletonConverter<T>(value);
    }
    
    public struct SingletonConverted : IComponentData { }
}

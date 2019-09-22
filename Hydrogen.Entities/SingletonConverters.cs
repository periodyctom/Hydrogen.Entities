using System;
using System.Runtime.CompilerServices;
using Unity.Entities;

namespace Hydrogen.Entities
{
    public interface ISingletonConverter<out T> : IComponentData
        where T : struct
    {
        T Singleton { get; }
        bool DontReplaceIfLoaded { get; }
    }
    
    [Serializable]
    public struct SingletonBlobConverter<T> : ISingletonConverter<BlobRefData<T>>
        where T : struct
    {
        public BlobRefData<T> Value;
        
        public bool DontReplace;
        
        public bool DontReplaceIfLoaded
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => DontReplace;
        }

        public BlobRefData<T> Singleton
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SingletonBlobConverter(BlobRefData<T> value, bool dontReplace)
        {
            Value = value;
            DontReplace = dontReplace;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator BlobRefData<T>(SingletonBlobConverter<T> @this) => @this.Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator SingletonBlobConverter<T>(BlobRefData<T> value) =>
            new SingletonBlobConverter<T>(value, false);
    }

    [Serializable]
    public struct SingletonDataConverter<T> : ISingletonConverter<T>
        where T : struct, IComponentData
    {
        public T Value;
        
        public bool DontReplace;

        public bool DontReplaceIfLoaded
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => DontReplace;
        }

        public T Singleton
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SingletonDataConverter(T value, bool dontReplace)
        {
            Value = value;
            DontReplace = dontReplace;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator T(SingletonDataConverter<T> @this) => @this.Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator SingletonDataConverter<T>(T value) =>
            new SingletonDataConverter<T>(value, false);
    }
    
    public struct SingletonConverted : IComponentData { }
}

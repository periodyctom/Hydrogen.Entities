using System;
using System.Runtime.CompilerServices;
using Unity.Entities;

namespace Hydrogen.Entities
{
    public interface ISingletonConverter<T> : IComponentData
        where T : struct, IComponentData
    {
        T Singleton { get; set; }

        bool DontReplace { get; set; }
    }

    /// <summary>
    /// Acts as a delivery mechanism for Singleton data that can be serialized to a sub-scene,
    /// and has some control over how the singleton component data is handled on load.
    /// </summary>
    /// <typeparam name="T">The Singleton <see cref="IComponentData"/> Type</typeparam>
    [Serializable]
    public struct SingletonConverter<T>
        where T : struct, IComponentData
    {
        /// <summary>
        /// Singleton Component Data that will become our actual singleton.
        /// </summary>
        public T Singleton;
        
        /// <summary>
        /// If the singleton is already loaded, don't replace it with this data.
        /// </summary>
        public bool DontReplace;

        /// <summary>
        /// Constructor for creating converters via code.
        /// </summary>
        /// <param name="singleton">Singleton component data value.</param>
        /// <param name="dontReplace">DontReplace setting, falls by default.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SingletonConverter(T singleton, bool dontReplace = false)
        {
            Singleton = singleton;
            DontReplace = dontReplace;
        }

        /// <summary>
        /// Implicitly casts the Converter to the Singleton Component Data.
        /// </summary>
        /// <param name="this">The current converter.</param>
        /// <returns>The original Singleton Component Data</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator T(SingletonConverter<T> @this) => @this.Singleton;

        /// <summary>
        /// Implicitly casts the Singleton Component Data payload to a converter.
        /// </summary>
        /// <param name="value">The Singleton Component Data to be set as a singleton.</param>
        /// <returns>The casted converter</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator SingletonConverter<T>(T value) => new SingletonConverter<T>(value);
    }
    
    /// <summary>
    /// Tag component that indicates the converter entity has delivered the Singleton data to the conversion system and is ready to be cleaned up.
    /// <see cref="EntityQuery.GetSingleton{T}()"/> method.
    /// </summary>
    public struct SingletonConverted : IComponentData { }
    
    /// <summary>
    /// Tag component that indicates an Entity with the SingletonConverted tag has data that was set as the current singleton
    /// </summary>
    public struct SingletonChanged : IComponentData { }
    
    /// <summary>
    /// Tag component that indicates none of the processed conversions succeeded
    /// </summary>
    public struct SingletonUnchanged : IComponentData { }
}

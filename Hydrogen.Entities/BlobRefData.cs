using System.Runtime.CompilerServices;
using Unity.Entities;

namespace Hydrogen.Entities
{
    /// <summary>
    /// Defines a component data type that has a single <see cref="BlobAssetReference{T}"/> field.
    /// Adds some convenience accessors and is used by the Singleton Conversion process for handling blob singletons.
    /// </summary>
    /// <typeparam name="T">Blob Asset struct type</typeparam>
    public struct BlobRefData<T> : IComponentData
        where T : struct
    {
        /// <summary>
        /// The <see cref="BlobAssetReference{T}"/> field.
        /// </summary>
        public BlobAssetReference<T> Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BlobRefData(BlobAssetReference<T> value) => Value = value;

        /// <summary>
        /// Convenience accessor for resolving the blob reference directly.
        /// </summary>
        public ref T Resolve
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref Value.Value;
        }

        /// <summary>
        /// Has the reference been properly created?
        /// </summary>
        public bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Value.IsCreated;
        }
    }
}

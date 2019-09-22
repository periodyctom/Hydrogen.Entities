using System.Runtime.CompilerServices;
using Unity.Entities;

namespace Hydrogen.Entities
{
    /// <summary>
    /// Defines a contract for a type that has a <see cref="BlobAssetReference{T}"/>
    /// Adds some convenience accessors and is used by the Singleton Conversion process.
    /// </summary>
    /// <typeparam name="T">Blob struct Type</typeparam>
    // public interface IBlobReferenceData<T> : IComponentData
    //     where T : struct
    // {
    //     /// <summary>
    //     /// The <see cref="BlobAssetReference{T}"/>
    //     /// </summary>
    //     BlobAssetReference<T> Reference { get; set; }
    //
    //     /// <summary>
    //     /// Convenience accessor for resolving the blob reference directly.
    //     /// </summary>
    //     ref T Resolve { get; }
    //
    //     /// <summary>
    //     /// Has the reference been properly created?
    //     /// </summary>
    //     bool IsCreated { get; }
    // }

    public struct BlobRefData<T> : IComponentData
        where T : struct
    {
        public BlobAssetReference<T> Value;

        public BlobAssetReference<T> Reference
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Value;
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Value = value;
        }
        
        public ref T Resolve
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref Value.Value;
        }

        public bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Value.IsCreated;
        }
    }
}

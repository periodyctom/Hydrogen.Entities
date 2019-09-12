using Unity.Entities;

namespace Hydrogen.Entities
{
    /// <summary>
    /// Defines a contract for a type that has a <see cref="BlobAssetReference{T}"/>
    /// Adds some convenience accessors and is used by the Singleton Conversion process.
    /// </summary>
    /// <typeparam name="T">Blob struct Type</typeparam>
    public interface IBlobReferenceData<T> : IComponentData
        where T : struct
    {
        /// <summary>
        /// The <see cref="BlobAssetReference{T}"/>
        /// </summary>
        BlobAssetReference<T> Reference { get; }

        /// <summary>
        /// Convenience accessor for resolving the blob reference directly.
        /// </summary>
        ref T Resolve { get; }

        /// <summary>
        /// Has the reference been properly created?
        /// </summary>
        bool IsCreated { get; }
    }
}

using System.Runtime.CompilerServices;
using Unity.Entities;

namespace Hydrogen.Entities.Tests 
{
    public struct PrefabCollectionBlob
    {
        public BlobArray<Entity> Prefabs;
    }

    // public struct PrefabCollectionReference : IBlobReferenceData<PrefabCollectionBlob>
    // {
    //     public BlobAssetReference<PrefabCollectionBlob> Value;
    //
    //     public BlobAssetReference<PrefabCollectionBlob> Reference
    //     {
    //         [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //         get => Value;
    //         
    //         [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //         set => Value = value;
    //     }
    //     
    //     public ref PrefabCollectionBlob Resolve
    //     {
    //         [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //         get => ref Value.Value;
    //     }
    //
    //     public bool IsCreated
    //     {
    //         [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //         get => Value.IsCreated;
    //     }
    // }
}

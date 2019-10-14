using Unity.Entities;

// ReSharper disable once CheckNamespace
namespace Hydrogen.Entities.Tests 
{
    public struct PrefabCollectionBlob
    {
        public BlobArray<Entity> Prefabs;
    }
}

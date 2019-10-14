using System.Runtime.CompilerServices;
using Hydrogen.Entities;
using Hydrogen.Entities.Tests;
using Unity.Entities;

[assembly: RegisterGenericComponentType(typeof(BlobRefData<PrefabCollectionBlob>))]
[assembly: InternalsVisibleTo("Hydrogen.Entities.Hybrid.Editor.Tests")]

using Unity.Entities;
using Hydrogen.Entities;
using Hydrogen.Entities.Tests;

[assembly: DisableAutoCreation]
[assembly: RegisterGenericComponentType(typeof(BlobRefData<TestSupportedLocales>))]
[assembly: RegisterGenericComponentType(typeof(SingletonBlobConverter<TestSupportedLocales>))]
[assembly: RegisterGenericComponentType(typeof(SingletonDataConverter<TestTimeConfig>))]


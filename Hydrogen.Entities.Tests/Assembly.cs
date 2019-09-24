
using Unity.Entities;
using Hydrogen.Entities;
using Hydrogen.Entities.Tests;

[assembly: DisableAutoCreation]
[assembly: RegisterGenericComponentType(typeof(BlobRefData<TestSupportedLocales>))]
[assembly: RegisterGenericComponentType(typeof(SingletonConverter<BlobRefData<TestSupportedLocales>>))]
[assembly: RegisterGenericComponentType(typeof(SingletonConverter<TestTimeConfig>))]
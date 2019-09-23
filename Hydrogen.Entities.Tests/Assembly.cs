
using Unity.Entities;
using Hydrogen.Entities;
using Hydrogen.Entities.Tests;

[assembly: DisableAutoCreation]
[assembly: RegisterGenericComponentType(typeof(BlobRefData<SingletonTests.TestSupportedLocales>))]
[assembly: RegisterGenericComponentType(typeof(SingletonConverter<SingletonTests.TestSupportedLocales>))]
[assembly: RegisterGenericComponentType(typeof(SingletonConverter<SingletonTests.TestTimeConfig>))]
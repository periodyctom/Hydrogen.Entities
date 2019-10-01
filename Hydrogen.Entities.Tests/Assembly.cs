
using Unity.Entities;
using Hydrogen.Entities;
using Hydrogen.Entities.Tests;

[assembly: DisableAutoCreation]
[assembly: RegisterGenericComponentType(typeof(BlobRefData<Locales>))]
[assembly: RegisterGenericComponentType(typeof(SingletonConverter<BlobRefData<Locales>>))]
[assembly: RegisterGenericComponentType(typeof(SingletonConverter<TimeConfig>))]
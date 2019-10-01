using UnityEngine;

// ReSharper disable CheckNamespace

namespace Hydrogen.Entities.Tests
{
    [DisallowMultipleComponent]
    public class
        LocalesInterfaceBootstrap : SingletonConverterBlobInterfaceBootstrap<Locales, LocalesDefinition> { }
}

using UnityEngine;


namespace Hydrogen.Entities.Tests
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Hidden/DontUse")]
    public class
        LocalesInterfaceAuthoring : SingletonConvertBlobInterfaceAuthoring<Locales, LocalesDefinition, LocalesConverter> { }
}

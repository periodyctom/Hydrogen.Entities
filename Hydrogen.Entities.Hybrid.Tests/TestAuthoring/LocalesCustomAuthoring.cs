using Unity.Entities;
using UnityEngine;


namespace Hydrogen.Entities.Tests
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Hidden/DontUse")]
    public class LocalesCustomAuthoring : SingletonConvertBlobCustomAuthoring<Locales, LocalesDefinition, LocalesConverter>
    {
        ScriptToBlobFunc<LocalesDefinition, Locales> m_ConverterFunc;

        protected override ScriptToBlobFunc<LocalesDefinition, Locales> ScriptToBlob =>
            m_ConverterFunc ?? (m_ConverterFunc = DoConvert);

        BlobAssetReference<Locales> DoConvert(
            LocalesDefinition definition,
            ScriptableObjectConversionSystem conversionSystem) =>
            SingletonConversionTestFixture.CreateLocaleData(definition.name, definition.AvailableLocales);
    }
}

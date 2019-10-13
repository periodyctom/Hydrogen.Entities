using Unity.Entities;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace Hydrogen.Entities.Tests
{
    [DisallowMultipleComponent]
    public class LocalesCustomAuthoring : SingletonConvertBlobCustomAuthoring<Locales, LocalesDefinition>
    {
        private ScriptToBlobFunc<LocalesDefinition, Locales> m_converterFunc;

        protected override ScriptToBlobFunc<LocalesDefinition, Locales> ScriptToBlob =>
            m_converterFunc ?? (m_converterFunc = DoConvert);

        private BlobAssetReference<Locales> DoConvert(
            LocalesDefinition definition,
            ScriptableObjectConversionSystem conversionSystem) =>
            SingletonConversionTestFixture.CreateLocaleData(definition.AvailableLocales);
    }
}

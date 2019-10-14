using Unity.Entities;
using UnityEngine;


namespace Hydrogen.Entities.Tests
{
    public class LocalesDefinition : ScriptableObject, IConvertScriptableObjectToBlob<Locales>
    {
        public string[] AvailableLocales =
        {
            "en",
            "fr",
            "it",
            "de",
            "es",
            "zh",
            "ja",
            "ko",
            "ru"
        };

        public BlobAssetReference<Locales> Convert(ScriptableObjectConversionSystem conversion) =>
            SingletonConversionTestFixture.CreateLocaleData(AvailableLocales);
    }
}

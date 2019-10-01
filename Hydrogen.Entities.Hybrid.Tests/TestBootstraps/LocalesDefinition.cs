using Unity.Entities;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace Hydrogen.Entities.Tests
{
    [CreateAssetMenu(menuName = "Hydrogen/Entities/Tests/Hybrid/Locales")]
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

using Unity.Entities;

namespace Hydrogen.Entities
{
    /// <summary>
    /// Interface that defines a contract for converting a <see cref="ScriptableObject"/> to a <see cref="BlobAssetReference{T0}"/>.
    /// </summary>
    /// <typeparam name="T0">The struct type our Blob Asset Reference will point to.</typeparam>
    public interface IConvertScriptableObjectToBlob<T0>
        where T0 : struct
    {
        /// <summary>
        /// Defines the Conversion function to convert this to a <see cref="BlobAssetReference{T0}"/>.
        /// </summary>
        /// <param name="conversion">The <see cref="ScriptableObjectConversionSystem"/> doing the converting. Allows us to build reference chains to other converted blobs.</param>
        /// <returns>The constructed <see cref="BlobAssetReference{T0}"/></returns>
        BlobAssetReference<T0> Convert(ScriptableObjectConversionSystem conversion);
    }
}

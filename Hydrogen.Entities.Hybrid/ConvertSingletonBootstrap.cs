using Unity.Entities;
using UnityEngine;

namespace Hydrogen.Entities
{
    /// <summary>
    /// Base class for implementing singleton conversion bootstrapping.
    /// Subclass this to handle different singleton converter and component types.
    /// </summary>
    /// <typeparam name="T0">Singleton Converter Component Data type.</typeparam>
    /// <typeparam name="T1">Singleton Component Data type</typeparam>
    [DisallowMultipleComponent]
    [RequiresEntityConversion]
    public abstract class ConvertSingletonBootstrap<T0, T1> : MonoBehaviour, IConvertGameObjectToEntity
        where T0 : struct, ISingletonConverter<T1>
        where T1 : struct, IComponentData
    {
        /// <summary>
        /// If true, when the converted singleton loads when the singleton
        /// is already set, the new value will be ignored.
        /// </summary>
        [Tooltip("If true, when the converted singleton loads when the singleton\n is already set, the new value will be ignored.")]
        public bool DontReplaceIfLoaded;

        /// <summary>
        /// Implement this to get the appropriate singleton converter
        /// component data, which will be processed during scene initialization.
        /// </summary>
        /// <param name="entity">Entity for this converted <see cref="GameObject"/></param>
        /// <param name="dstManager">Destination <see cref="EntityManager"/></param>
        /// <param name="conversionSystem">The <see cref="GameObjectConversionSystem"/> for converting any other GameObject parts.</param>
        /// <returns>The Singleton Converter type.</returns>
        protected abstract T0 GetConverter(
            Entity entity,
            EntityManager dstManager,
            GameObjectConversionSystem conversionSystem);
        
        /// <summary>
        /// Implementation of <see cref="IConvertGameObjectToEntity"/>
        /// </summary>
        /// <param name="entity">Entity for this converted <see cref="GameObject"/></param>
        /// <param name="dstManager">Destination <see cref="EntityManager"/></param>
        /// <param name="conversionSystem">The <see cref="GameObjectConversionSystem"/></param>
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, GetConverter(entity, dstManager, conversionSystem));
        }
    }

    /// <summary>
    /// Base class for implementing singleton conversions from regular <see cref="IComponentData"/> structs.
    /// </summary>
    /// <typeparam name="T">The data struct IComponentData type.</typeparam>
    public abstract class ConvertSingletonDataBootstrap<T> : ConvertSingletonBootstrap<SingletonDataConverter<T>, T>
        where T : struct, IComponentData
    {
    }

    /// <summary>
    /// Base Class for implementing singleton conversions from <see cref="BlobAssetReference{T1}"/>.
    /// </summary>
    /// <typeparam name="T">The Blob asset struct type.</typeparam>
    public abstract class ConvertSingletonBlobBootstrap<T> : 
    ConvertSingletonBootstrap<SingletonBlobConverter<T>, BlobRefData<T>>
        where T : struct
    {
    }

    /// <summary>
    /// Base Class for implementing simple conversions of serializable struct singletons.
    /// </summary>
    /// <typeparam name="T">Component Data that is also serializable in the editor.</typeparam>
    public abstract class ConvertSingletonDataSimpleBootstrap<T> : ConvertSingletonDataBootstrap<T>
        where T : struct, IComponentData
    {
        [SerializeField]
        [Tooltip("Source setting for our singleton.")]
        private T m_data;

        protected override SingletonDataConverter<T> GetConverter(
            Entity entity,
            EntityManager dstManager,
            GameObjectConversionSystem conversionSystem) =>
            new SingletonDataConverter<T>(m_data, DontReplaceIfLoaded);
    }

    /// <summary>
    /// Base class for implementing simple conversions of <see cref="ScriptableObject"/>s to blob assets of <typeparamref name="T0"/>.
    /// The <typeparamref name="T1"/> must implement <see cref="IConvertScriptableObjectToBlob{T1}"/>.
    /// </summary>
    /// <typeparam name="T0">The Blob asset struct type.</typeparam>
    /// <typeparam name="T1">ScriptableObject that implements the conversion interface.</typeparam>
    public abstract class ConvertSingletonBlobInterfaceBootstrap<T0, T1> : ConvertSingletonBlobBootstrap<T0>
        where T0 : struct
        where T1 : ScriptableObject, IConvertScriptableObjectToBlob<T0>
    {
        [SerializeField]
        [Tooltip("Source ScriptableObject setting for our singleton.")]
        private T1 m_data;

        protected override SingletonBlobConverter<T0> GetConverter(
            Entity entity,
            EntityManager dstManager,
            GameObjectConversionSystem conversionSystem)
        {
            var soConversionSystem = dstManager.World.GetOrCreateSystem<ScriptableObjectConversionSystem>();
            BlobAssetReference<T0> blob = soConversionSystem.GetBlob<T1, T0>(m_data);

            BlobRefData<T0> blobReferenceData = default;
            blobReferenceData.Reference = blob;

            return new SingletonBlobConverter<T0>(blobReferenceData, DontReplaceIfLoaded);
        }
    }

    /// <summary>
    /// Base class for implementing custom conversions of <see cref="ScriptableObject"/> to blob assets of <typeparamref name="T0"/>.
    /// The user must provide a custom conversion function.
    /// </summary>
    /// <typeparam name="T0">The Blob asset struct type.</typeparam>
    /// <typeparam name="T1">ScriptableObject that will be converted by the custom function.</typeparam>
    public abstract class ConvertSingletonBlobCustomBootstrap<T0, T1> : ConvertSingletonBlobBootstrap<T0>
        where T0 : struct
        where T1 : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Source ScriptableObject setting for our singleton.")]
        private T1 m_data;
        
        /// <summary>
        /// The <see cref="ScriptToBlobFunc{T2, T1}"/> accessor.
        /// It's setup like this to allow for the user to cache the conversion function.
        /// </summary>
        protected abstract ScriptToBlobFunc<T1, T0> Conversion { get; }

        protected override SingletonBlobConverter<T0> GetConverter(
            Entity entity,
            EntityManager dstManager,
            GameObjectConversionSystem conversionSystem)
        {
            var soConversionSystem = dstManager.World.GetOrCreateSystem<ScriptableObjectConversionSystem>();
            BlobAssetReference<T0> blob = soConversionSystem.GetBlob(m_data, Conversion);
            
            BlobRefData<T0> blobReferenceData = default;
            blobReferenceData.Reference = blob;

            return new SingletonBlobConverter<T0>(blobReferenceData, DontReplaceIfLoaded);
        }
    }
}

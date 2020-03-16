using Unity.Entities;
using UnityEngine;

namespace Hydrogen.Entities
{
    /// <summary>
    /// Base class for implementing singleton conversion bootstrapping.
    /// Subclass this to handle different singleton converter and component types.
    /// </summary>
    /// <typeparam name="T0">Singleton Component Data type</typeparam>
    [RequiresEntityConversion]
    public abstract class SingletonConvertAuthoring<T0, T1, T2> : MonoBehaviour, IConvertGameObjectToEntity
        where T0 : struct, IComponentData
        where T2 : struct, ISingletonConverter<T0>
    {
        /// <summary>
        /// Source data for our singleton.
        /// </summary>
        [SerializeField]
        [Tooltip("Source data for our singleton.")]
        public T1 Source;

        /// <summary>
        /// If true, when the converted singleton loads when the singleton
        /// is already set, the new value will be ignored.
        /// </summary>
        [Tooltip(
            "If true, when the converted singleton loads when the singleton is already set, the new value will be ignored.")]
        public bool DontReplaceIfLoaded;

        /// <summary>
        /// Implement this to get the appropriate singleton converter
        /// component data, which will be processed during scene initialization.
        /// </summary>
        /// <param name="entity">Entity for this converted <see cref="GameObject"/></param>
        /// <param name="dstManager">Destination <see cref="EntityManager"/></param>
        /// <param name="conversionSystem">The <see cref="GameObjectConversionSystem"/> for converting any other GameObject parts.</param>
        /// <returns>The <see cref="SingletonConverter{T}"/> component data, which acts as our delivery wrapper for the final singleton component data.</returns>
        protected abstract T2 GetConverter(
            Entity entity,
            EntityManager dstManager,
            GameObjectConversionSystem conversionSystem);

        /// <summary>
        /// Implementation of <see cref="IConvertGameObjectToEntity"/>
        /// </summary>
        /// <param name="entity">Entity for this converted <see cref="GameObject"/></param>
        /// <param name="dstManager">Destination <see cref="EntityManager"/></param>
        /// <param name="conversionSystem">The <see cref="GameObjectConversionSystem"/></param>
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem) =>
            dstManager.AddComponentData(entity, GetConverter(entity, dstManager, conversionSystem));
    }

    /// <summary>
    /// Implements the simplest conversion of struct singletons.
    /// </summary>
    /// <typeparam name="T0">Component Data that is also serializable in the editor.</typeparam>
    public abstract class SingletonConvertDataAuthoring<T0, T1> : SingletonConvertAuthoring<T0, T0, T1>
        where T0 : struct, IComponentData
        where T1 : struct, ISingletonConverter<T0>

    {
        /// <summary>
        /// Implements a direct conversion from <see cref="SingletonConvertAuthoring{T0,T1}.Source"/>
        /// to <see cref="SingletonConverter{T0}"/>
        /// </summary>
        /// <param name="entity"><see cref="Entity"/>, not used in this conversion.</param>
        /// <param name="dstManager"><see cref="EntityManager"/>, not used in this conversion.</param>
        /// <param name="conversionSystem"><see cref="GameObjectConversionSystem"/>, not used in this conversion.</param>
        /// <returns><see cref="SingletonConverter{T}"/> component data to be added to this Entity.</returns>
        protected sealed override T1 GetConverter(
            Entity entity,
            EntityManager dstManager,
            GameObjectConversionSystem conversionSystem)

        {
            return new T1
            {
                Singleton = Source,
                DontReplace = DontReplaceIfLoaded
            };
        }
    }

    /// <summary>
    /// Base for performing Blob Asset singleton conversions.
    /// </summary>
    /// <typeparam name="T0">Blob Asset struct type.</typeparam>
    /// <typeparam name="T1"><see cref="ScriptableObject"/> type that will be our source.</typeparam>
    public abstract class SingletonConvertBlobAuthoring<T0, T1, T2> : SingletonConvertAuthoring<BlobRefData<T0>, T1, T2>
        where T0 : struct
        where T1 : ScriptableObject
        where T2 : struct, ISingletonConverter<BlobRefData<T0>>
    {
        /// <summary>
        /// Implement this to handle the interaction between this bootstrapper
        /// and the <see cref="ScriptableObjectConversionSystem"/> 
        /// </summary>
        /// <param name="conversion">Conversion system to invoke the actual conversion on.</param>
        /// <returns>A <see cref="BlobAssetReference{T}"/> to the converted data.</returns>
        protected abstract BlobAssetReference<T0> ConvertScriptable(ScriptableObjectConversionSystem conversion);

        /// <summary>
        /// Performs most of the handling needed to invoke the <see cref="ScriptableObject"/> conversion
        /// and store the result in a <see cref="BlobRefData{T}"/> component.
        /// </summary>
        /// <param name="entity">Entity for this converted <see cref="GameObject"/></param>
        /// <param name="dstManager">Destination <see cref="EntityManager"/></param>
        /// <param name="conversionSystem">The <see cref="GameObjectConversionSystem"/> for converting any other GameObject parts.</param>
        /// <returns><see cref="SingletonConverter{BlobRefData{T0}}"/> component data to be added to this Entity.</returns>
        protected sealed override T2 GetConverter(
            Entity entity,
            EntityManager dstManager,
            GameObjectConversionSystem conversionSystem)
        {
            var blob = ConvertScriptable(dstManager.World.GetOrCreateSystem<ScriptableObjectConversionSystem>());

            BlobRefData<T0> blobReferenceData = default;
            blobReferenceData.Value = blob;

            return new T2
            {
                Singleton = blobReferenceData,
                DontReplace = DontReplaceIfLoaded
            };
        }
    }

    /// <summary>
    /// Base class for implementing simple conversions of <see cref="ScriptableObject"/>s to blob assets of <typeparamref name="T0"/>.
    /// The <typeparamref name="T1"/> must implement <see cref="IConvertScriptableObjectToBlob{T1}"/>.
    /// </summary>
    /// <typeparam name="T0">The Blob asset struct type.</typeparam>
    /// <typeparam name="T1">ScriptableObject that implements the conversion interface.</typeparam>
    public abstract class SingletonConvertBlobInterfaceAuthoring<T0, T1, T2> : SingletonConvertBlobAuthoring<T0, T1, T2>
        where T0 : struct
        where T1 : ScriptableObject, IConvertScriptableObjectToBlob<T0>
        where T2 : struct, ISingletonConverter<BlobRefData<T0>>
    {
        /// <summary>
        /// Overriden Convert function that handles calling the correct GetBlob function
        /// that utilizes the <see cref="IConvertScriptableObjectToBlob{T0}"/> interface.
        /// </summary>
        /// <param name="conversion"><see cref="ScriptableObjectConversionSystem"/> to perform the conversion.</param>
        /// <returns>A <see cref="BlobAssetReference{T0}"/> that holds the reference to our {T0} Blob.</returns>
        protected override BlobAssetReference<T0> ConvertScriptable(ScriptableObjectConversionSystem conversion) =>
            conversion.GetBlob<T1, T0>(Source);
    }

    /// <summary>
    /// Base class for implementing custom conversions of <see cref="ScriptableObject"/> to blob assets of <typeparamref name="T0"/>.
    /// The user must provide a custom conversion function.
    /// </summary>
    /// <typeparam name="T0">The Blob asset struct type.</typeparam>
    /// <typeparam name="T1">ScriptableObject that will be converted by the custom function.</typeparam>
    public abstract class SingletonConvertBlobCustomAuthoring<T0, T1, T2> : SingletonConvertBlobAuthoring<T0, T1, T2>
        where T0 : struct
        where T1 : ScriptableObject
        where T2 : struct, ISingletonConverter<BlobRefData<T0>>
    {
        /// <summary>
        /// Implement this to return a (preferably cached) delegate
        /// that matches the <see cref="ScriptToBlobFunc{T0,T1}"/> signature.
        /// </summary>
        protected abstract ScriptToBlobFunc<T1, T0> ScriptToBlob { get; }

        /// <summary>
        /// Overriden Convert function that handles calling the correct GetBlob function and passes along the custom
        /// function that handles the actual low-level conversion. 
        /// </summary>
        /// <param name="conversion"><see cref="ScriptableObjectConversionSystem"/> to perform the conversion.</param>
        /// <returns>A <see cref="BlobAssetReference{T0}"/> that holds the reference to our {T0} Blob.</returns>
        protected sealed override BlobAssetReference<T0>
            ConvertScriptable(ScriptableObjectConversionSystem conversion) =>
            conversion.GetBlob(Source, ScriptToBlob);
    }
}

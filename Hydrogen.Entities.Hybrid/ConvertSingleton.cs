using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Assertions;

namespace Hydrogen.Entities
{
    /// <summary>
    /// Defines a Conversion from a <see cref="ScriptableObject"/> to a singleton component data.
    /// </summary>
    /// <typeparam name="T0">Concrete ScriptableObject type.</typeparam>
    /// <typeparam name="T1"><see cref="IComponentData"/> data type</typeparam>
    [DisallowMultipleComponent]
    [RequiresEntityConversion]
    public abstract class ConvertSingleton<T0, T1> : MonoBehaviour, IConvertGameObjectToEntity
        where T0 : ScriptableObject
        where T1 : struct, IComponentData
    {
        [SerializeField, Tooltip("Source ScriptableObject for our converted data.")] private T0 _configDefinition;
        [SerializeField, Tooltip("If the singleton already exists, we don't convert this one. Default action is replacement.")] private bool m_DontReplaceIfPresent;

        public T0 ConfigDefinition => _configDefinition;

        protected abstract T1 ConvertDefinition(
            EntityManager dstManager,
            Entity entity,
            GameObjectConversionSystem conversionSystem,
            T0 configDefinition);

        /// <summary>
        /// Performs the ScriptableObject Conversion to a Singleton Entity.
        /// </summary>
        /// <param name="entity">Entity to become the Singleton.</param>
        /// <param name="dstManager">EntityManager that controls the Entity</param>
        /// <param name="conversionSystem">GameObjectConversionSystem we're using.</param>
        public virtual void Convert(
            Entity entity,
            EntityManager dstManager,
            GameObjectConversionSystem conversionSystem)
        {
            NativeArray<ComponentType> comps = dstManager.GetComponentTypes(entity);

            int len = comps.Length;

            // remove other components, as we can't convert to a singleton if we have more than
            // one component.
            for (int i = 0; i < len; i++)
                dstManager.RemoveComponent(entity, comps[i]);
            
            comps.Dispose();
            
            bool doesSingletonExist = Singletons.DoesSingletonExist<T1>(dstManager);

            if (doesSingletonExist && !m_DontReplaceIfPresent)
            {
                Singletons.DestroySingleton<T1>(dstManager);
            }
            else if(doesSingletonExist && m_DontReplaceIfPresent)
            {
                return;
            }

            Assert.IsNotNull(_configDefinition);
            T1 configData = ConvertDefinition(dstManager, entity, conversionSystem, _configDefinition);
            
            Singletons.MakeSingleton(dstManager, entity, configData);
            
            OnPostCreateSingleton(dstManager, entity);
        }

        /// <summary>
        /// Run any immediate post commands, such as creating helper entities.
        /// </summary>
        /// <param name="dstManager">EntityManager that controls the Entity</param>
        /// <param name="entity"></param>
        protected virtual void OnPostCreateSingleton(EntityManager dstManager, Entity entity) {}
    }

    /// <summary>
    /// Allows you to define an automatic conversion process to create a <see cref="BlobAssetReference{T}"/>
    /// singleton. The <see cref="ScriptableObject"/> must implement <see cref="IConvertScriptableObjectToBlob{T0}"/>
    /// </summary>
    /// <typeparam name="T0">Source <see cref="ScriptableObject"/> concrete type, that implements <see cref="IConvertScriptableObjectToBlob{T0}"/></typeparam>
    /// <typeparam name="T1"><see cref="IConfigRef{T}"/> to create for our singleton that will hold our blob reference.</typeparam>
    /// <typeparam name="T2">Blob asset data type.</typeparam>
    [Obsolete]
    public abstract class ConfigSingleton<T0, T1, T2> : ConvertSingleton<T0, T1>
        where T0 : ScriptableObject, IConvertScriptableObjectToBlob<T2>
        where T1 : struct, IConfigRef<T2>
        where T2 : struct
    {
        protected override T1 ConvertDefinition(
            EntityManager dstManager,
            Entity entity,
            GameObjectConversionSystem conversionSystem,
            T0 configDefinition)
        {
            var soConversionSystem = dstManager.World.GetOrCreateSystem<ScriptableObjectConversionSystem>();

            T1 refData = default;
            refData.BlobRef = soConversionSystem.GetBlob<T0, T2>(configDefinition);
            
            return refData;
        }
    }

    /// <summary>
    /// Converts and immediately initiates the first config reload. Only useful for configs that update settings
    /// that may be a slow operation or change game tuning parameters, such as application states. Meant to work with
    /// <see cref="ConfigSystem{T0,T1}"/> derivatives.
    /// </summary>
    /// <typeparam name="T0">Concrete <see cref="ScriptableObject"/> type, that implements <see cref="IConvertScriptableObjectToBlob{T0}"/></typeparam>
    /// <typeparam name="T1"><see cref="IConfigRef{T}"/> to create for our singleton that will hold our blob reference.</typeparam>
    /// <typeparam name="T2">Blob asset data type.</typeparam>
    [Obsolete]
    public abstract class ConfigSingletonWithReload<T0, T1, T2> : ConfigSingleton<T0, T1, T2>
        where T0 : ScriptableObject, IConvertScriptableObjectToBlob<T2>
        where T1 : struct, IConfigRef<T2>
        where T2 : struct
    {
        protected override void OnPostCreateSingleton(EntityManager dstManager, Entity entity)
        {
            Singletons.CreateSingleton(dstManager, new ConfigReload<T1, T2>());
        }
    }

    /// <summary>
    /// Manually converts a ScriptableObject to create a <see cref="BlobAssetReference{T}"/>.
    /// </summary>
    /// <typeparam name="T0">Concrete <see cref="ScriptableObject"/> type</typeparam>
    /// <typeparam name="T1"><see cref="IConfigRef{T}"/> to create for our singleton that will hold our blob reference.</typeparam>
    /// <typeparam name="T2">Blob asset data type.</typeparam>
    [Obsolete]
    public abstract class ManualConfigSingleton<T0, T1, T2> : ConvertSingleton<T0, T1>
        where T0 : ScriptableObject
        where T1 : struct, IConfigRef<T2>
        where T2 : struct
    {
        protected abstract ScriptToBlobFunc<T0, T2> ManualConversion { get; }
        
        private T1 ScriptToData(
            T0 src,
            ScriptableObjectConversionSystem convert)
        {
            T1 refData = default;
            refData.BlobRef = convert.GetBlob(src, ManualConversion);
            return refData;
        }
        
        protected override T1 ConvertDefinition(
            EntityManager dstManager,
            Entity entity,
            GameObjectConversionSystem conversionSystem,
            T0 configDefinition)
        {
            var convert = dstManager.World.GetOrCreateSystem<ScriptableObjectConversionSystem>();
            return ScriptToData(configDefinition, convert);
        }
    }

    /// <summary>
    /// Manually Converts and immediately initiates the first config reload. Only useful for configs that update settings
    /// that may be a slow operation or change game tuning parameters, such as application states. Meant to work with
    /// <see cref="ConfigSystem{T0,T1}"/> derivatives.
    /// </summary>
    /// <typeparam name="T0">Concrete <see cref="ScriptableObject"/> type.</typeparam>
    /// <typeparam name="T1"><see cref="IConfigRef{T}"/> to create for our singleton that will hold our blob reference.</typeparam>
    /// <typeparam name="T2">Blob asset data type.</typeparam>
    [Obsolete]
    public abstract class ManualConfigSingletonWithReload<T0, T1, T2> : ManualConfigSingleton<T0, T1, T2>
        where T0 : ScriptableObject
        where T1 : struct, IConfigRef<T2>
        where T2 : struct
    {
        protected override void OnPostCreateSingleton(EntityManager dstManager, Entity entity)
        {
            Singletons.CreateSingleton(dstManager, new ConfigReload<T1, T2>());
        }
    }
}

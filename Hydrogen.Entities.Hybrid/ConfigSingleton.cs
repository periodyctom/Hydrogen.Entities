using Hydrogen.Entities;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Assertions;

namespace Titanium.Core
{
    /// <summary>
    /// Defines a Conversion from a <see cref="ScriptableObject"/> to a singleton component data.
    /// </summary>
    /// <typeparam name="T0">Concrete ScriptableObject type.</typeparam>
    /// <typeparam name="T1"><see cref="IComponentData"/> data type</typeparam>
    [DisallowMultipleComponent]
    [RequiresEntityConversion]
    [RequireComponent(typeof(ConvertToEntity))]
    public abstract class ConfigSingleton<T0, T1> : MonoBehaviour, IConvertGameObjectToEntity
        where T0 : ScriptableObject
        where T1 : struct, IComponentData
    {
        [SerializeField] private T0 _configDefinition;

        private static bool _converted = false;

        protected abstract T1 ConvertDefinition(
            Entity entity,
            EntityManager dstManager,
            GameObjectConversionSystem conversionSystem,
            T0 configDefinition);

        public virtual void Convert(
            Entity entity,
            EntityManager dstManager,
            GameObjectConversionSystem conversionSystem)
        {
            if(_converted) return;
            
            NativeArray<ComponentType> comps = dstManager.GetComponentTypes(entity, Allocator.Temp);

            int len = comps.Length;

            for (int i = 0; i < len; i++)
                dstManager.RemoveComponent(entity, comps[i]);
            
            comps.Dispose();

            Assert.IsNotNull(_configDefinition);
            T1 configData = ConvertDefinition(entity, dstManager, conversionSystem, _configDefinition);
            
            Singletons.MakeSingleton(dstManager, entity, configData);
            
            OnPostCreateSingleton(dstManager, entity);

            _converted = true;
        }

        protected virtual void OnPostCreateSingleton(EntityManager dstManager, Entity entity) {}
    }

    /// <summary>
    /// Allows you to define an automatic conversion process to create a <see cref="BlobAssetReference{T}"/>
    /// singleton. The <see cref="ScriptableObject"/> must implement <see cref="IConvertScriptableObjectToBlob{T0}"/>
    /// </summary>
    /// <typeparam name="T0">Source <see cref="ScriptableObject"/> concrete type.</typeparam>
    /// <typeparam name="T1"><see cref="IConfigRef{T}"/> to create for our singleton that will hold our blob reference.</typeparam>
    /// <typeparam name="T2">Blob asset data type.</typeparam>
    public abstract class ConvertSingleton<T0, T1, T2> : ConfigSingleton<T0, T1>
        where T0 : ScriptableObject, IConvertScriptableObjectToBlob<T2>
        where T1 : struct, IConfigRef<T2>
        where T2 : struct
    {
        protected override T1 ConvertDefinition(
            Entity entity,
            EntityManager dstManager,
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
    public abstract class ConvertConfigSingleton<T0, T1, T2> : ConvertSingleton<T0, T1, T2>
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
    /// Allows you to define a manual conversion process to create a <see cref="BlobAssetReference{T}"/>
    /// singleton.
    /// </summary>
    /// <typeparam name="T0">Source <see cref="ScriptableObject"/> concrete type.</typeparam>
    /// <typeparam name="T1"><see cref="IConfigRef{T}"/> to create for our singleton that will hold our blob reference.</typeparam>
    /// <typeparam name="T2">Blob asset data type.</typeparam>
    public abstract class ManualConvertSingleton<T0, T1, T2> : ConfigSingleton<T0, T1>
        where T0 : ScriptableObject
        where T1 : struct, IConfigRef<T2>
        where T2 : struct
    {
        protected abstract BlobAssetReference<T2> ScriptToBlob(
            T0 src,
            ScriptableObjectConversionSystem convert,
            GameObjectConversionSystem conversionSystem);
        
        protected override T1 ConvertDefinition(
            Entity entity,
            EntityManager dstManager,
            GameObjectConversionSystem conversionSystem,
            T0 configDefinition)
        {
            var soConversionSystem = dstManager.World.GetOrCreateSystem<ScriptableObjectConversionSystem>();

            T1 refData = default;
            refData.BlobRef = soConversionSystem.GetBlob(configDefinition, ScriptToBlob);
            
            return refData;
        }
    }

    /// <summary>
    /// Manually converts a ScriptableObject to create a <see cref="BlobAssetReference{T}"/> nd immediately initiates the first config reload.
    /// Only useful for configs that update settings that may be a slow operation or change game tuning parameters, such as application states.
    /// Meant to work with <see cref="ConfigSystem{T0,T1}"/> derivatives.
    /// </summary>
    /// <typeparam name="T0">Concrete <see cref="ScriptableObject"/> type</typeparam>
    /// <typeparam name="T1"><see cref="IConfigRef{T}"/> to create for our singleton that will hold our blob reference.</typeparam>
    /// <typeparam name="T2">Blob asset data type.</typeparam>
    public abstract class ManualConvertConfigSingleton<T0, T1, T2> : ManualConvertSingleton<T0, T1, T2>
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

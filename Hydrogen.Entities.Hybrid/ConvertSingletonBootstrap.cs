using Unity.Entities;
using UnityEngine;

namespace Hydrogen.Entities
{
    /// <summary>
    /// Base class for implementing singleton conversion bootstrapping.
    /// Subclass this to handle different singleton component types.
    /// </summary>
    /// <typeparam name="T">Singleton Component Data type.</typeparam>
    [DisallowMultipleComponent]
    [RequiresEntityConversion]
    public abstract class ConvertSingletonBootstrap<T> : MonoBehaviour, IConvertGameObjectToEntity
        where T : struct, IComponentData
    {
        /// <summary>
        /// If this converted singleton loads when the singleton
        /// is already set. The new value will be ignored.
        /// </summary>
        public bool DontReplaceIfLoaded;
        
        /// <summary>
        /// If true, will create a corresponding <see cref="SingletonRefresh{T}"/> when the value changes.
        /// </summary>
        public bool RequiresRefreshAfterLoading;

        /// <summary>
        /// Implement this to get the appropriate singleton converter
        /// component data, which will be processed during scene initialization.
        /// </summary>
        /// <param name="entity">Entity for this converted <see cref="GameObject"/></param>
        /// <param name="dstManager">Destination <see cref="EntityManager"/></param>
        /// <param name="conversionSystem">The <see cref="GameObjectConversionSystem"/></param>
        /// <returns></returns>
        protected abstract T GetConverter(
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
            T convertData = GetConverter(entity, dstManager, conversionSystem);

            dstManager.AddComponentData(entity, convertData);
            
            if (DontReplaceIfLoaded)
                dstManager.AddComponentData(entity, new SingletonDontReplace());

            if (RequiresRefreshAfterLoading)
                dstManager.AddComponentData(entity, new SingletonRequiresRefresh());
        }
    }
}

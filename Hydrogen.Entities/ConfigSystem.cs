using System;
using Unity.Entities;

namespace Hydrogen.Entities
{
    /// <summary>
    /// Provides an convenience interface for components that have a primary blob asset reference.
    /// </summary>
    /// <typeparam name="T">Type of the blob asset.</typeparam>
    [Obsolete]
    public interface IResolveRef<T> : IComponentData
        where T : struct
    {
        /// <summary>
        /// Resolves blob target. Must have a valid reference.
        /// </summary>
        ref T Resolve { get; }
    }
    
    /// <summary>
    /// Used to define a component that holds a <see cref="BlobAssetReference{T}"/>
    /// </summary>
    /// <typeparam name="T">Type of the blob asset.</typeparam>
    [Obsolete]
    public interface IConfigRef<T> : IResolveRef<T>
        where T : struct
    {
        /// <summary>
        /// Blob Value accessor.
        /// </summary>
        BlobAssetReference<T> BlobRef { get; set; }
    }
    
    /// <summary>
    /// Create a singleton entity with a Config Ref type information,
    /// and you can easily add reload functionality for settings that
    /// can be set once / infrequently / on user confirm.
    /// </summary>
    /// <typeparam name="T0"></typeparam>
    /// <typeparam name="T1"></typeparam>
    [Obsolete]
    public struct ConfigReload<T0, T1> : IComponentData
        where T0 : struct, IConfigRef<T1>
        where T1 : struct { }

    [Obsolete]
    public abstract class ConfigSystem<T0, T1> : ComponentSystem
        where T0 : struct, IConfigRef<T1>
        where T1 : struct
    {
        protected override void OnCreate()
        {
            RequireSingletonForUpdate<T0>();
            RequireSingletonForUpdate<ConfigReload<T0, T1>>();
        }

        protected override void OnUpdate()
        {
            var config = GetSingleton<T0>();
            UpdateConfig(config);
            Entity reloadEntity = GetSingletonEntity<ConfigReload<T0, T1>>();
            PostUpdateCommands.DestroyEntity(reloadEntity);
        }

        protected abstract void UpdateConfig(T0 configRef);
    }
}

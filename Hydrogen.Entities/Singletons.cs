using System;
using System.Runtime.CompilerServices;
using Unity.Entities;
using UnityEngine.Assertions;

namespace Hydrogen.Entities
{
    public struct SingletonHelper<T> : IDisposable
        where T : struct, IComponentData
    {
        public static readonly ComponentType Type = typeof(T);
        
        public readonly EntityManager Manager;
        public readonly EntityArchetype Archetype;
        public readonly EntityQuery Query;
        public Entity Current;

        public SingletonHelper(EntityManager manager)
        {
            Assert.IsTrue(manager.IsCreated);
            Manager = manager;
            Archetype = Manager.CreateArchetype(Type);
            Query = Manager.CreateEntityQuery(Type);
            Current = Entity.Null;
        }

        public SingletonHelper(EntityManager manager, T initialValue)
        {
            Assert.IsTrue(manager.IsCreated);
            Manager = manager;
            Archetype = Manager.CreateArchetype(Type);
            Query = Manager.CreateEntityQuery(Type);
            Current = Manager.CreateEntity(Type);
            Query.SetSingleton(initialValue);
        }

        public void Create(T value)
        {
            if (Current != Entity.Null)
            {
                Query.SetSingleton(value);
            }
            else if(Current == Entity.Null)
            {
                Current = Manager.CreateEntity(Archetype);
                Query.SetSingleton(value);
            }
        }

        public void Destroy()
        {
            if (Current == Entity.Null) return;
            Assert.IsTrue(Manager.IsCreated);
            
            Manager.DestroyEntity(Current);
            Current = Entity.Null;
        }

        public void Dispose()
        {
            Query.Dispose();
        }
    }
    
    public static class Singletons
    {
        /// <summary>
        /// Does the boilerplate work of creating a singleton <see cref="Entity"/> for the given component type.
        /// Primarily useful in conversion/bootstrap code.
        /// Check out <seealso cref="EntityQuery.SetSingleton"/> for more details.
        /// </summary>
        /// <param name="manager">The <see cref="EntityManager"/> to create the entity in.</param>
        /// <param name="src">Source data for the component.</param>
        /// <typeparam name="T0">The <see cref="IComponentData"/> struct that will define this entity.</typeparam>
        /// <returns>The created entity</returns>
        /// <remarks>Disposes the query used to create the entity. See <see cref="CreateQueryAndSingleton{T0}"/> for an alternative that doesn't.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity CreateSingleton<T0>(EntityManager manager, T0 src)
            where T0 : struct, IComponentData
        {
            (EntityQuery query, Entity entity) = CreateQueryAndSingleton(manager, src);

            query.Dispose();

            return entity;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="manager"></param>
        /// <typeparam name="T0"></typeparam>
        /// <returns></returns>
        public static bool DoesSingletonExist<T0>(EntityManager manager)
            where T0 : struct, IComponentData
        {
            ComponentType type = typeof(T0);
            EntityQuery query = manager.CreateEntityQuery(type);

            int entityCount = query.CalculateEntityCount();
            bool result = entityCount > 0;
            
            query.Dispose();

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="manager"></param>
        /// <typeparam name="T0"></typeparam>
        public static void DestroySingleton<T0>(EntityManager manager)
            where T0 : struct, IComponentData
        {
            ComponentType type = typeof(T0);
            EntityQuery query = manager.CreateEntityQuery(type);
            
            int entityCount = query.CalculateEntityCount();
            bool result = entityCount > 0;
            
            if(result)
                manager.DestroyEntity(query.GetSingletonEntity());
        }

        /// <summary>
        /// Does the boilerplate work of creating a singleton <see cref="Entity"/> for the given type.
        /// Also returns the <see cref="EntityQuery"/> used to create it for further use.
        /// Primarily useful in conversion/boostrap code.
        /// Check out <seealso cref="EntityQuery.SetSingleton"/> for more Details.
        /// </summary>
        /// <param name="manager">The <see cref="EntityManager"/> to create the entity in.</param>
        /// <param name="src">Source data for the component.</param>
        /// <typeparam name="T0">The <see cref="IComponentData"/> struct that will define this entity.</typeparam>
        /// <returns>A tuple that contains both the query and the created Entity.</returns>
        public static (EntityQuery, Entity) CreateQueryAndSingleton<T0>(EntityManager manager, T0 src)
            where T0 : struct, IComponentData
        {
            ComponentType type = typeof(T0);
            Entity entity = manager.CreateEntity(type);

            EntityQuery query = manager.CreateEntityQuery(type);
            query.SetSingleton(src);

            return (query, entity);
        }

        /// <summary>
        /// Does the boilerplate work of making a previously-created <see cref="Entity"/> into a singleton for the given type.
        /// Primarily useful in conversion/boostrap code.
        /// Check out <seealso cref="EntityQuery.SetSingleton"/> for more Details.
        /// </summary>
        /// <param name="manager">The <see cref="EntityManager"/> to create the entity in.</param>
        /// <param name="entity">The entity we will make a singleton.</param>
        /// <param name="src">Source data for the component.</param>
        /// <typeparam name="T0">The <see cref="IComponentData"/> struct that will define this entity.</typeparam>
        public static void MakeSingleton<T0>(EntityManager manager, Entity entity, T0 src)
            where T0 : struct, IComponentData
        {
            EntityQuery query = MakeQueryAndSingleton(manager, entity, src);
            
            query.Dispose();
        }

        /// <summary>
        /// Does the boilerplate work of making a previously-created <see cref="Entity"/> into a singleton for the given type.
        /// Returns the query used to create the singleton.
        /// </summary>
        /// <param name="manager">The <see cref="EntityManager"/> to create the entity in.</param>
        /// <param name="entity">The entity we will make a singleton.</param>
        /// <param name="src">Source data for the component.</param>
        /// <typeparam name="T0">The <see cref="IComponentData"/> struct that will define this entity.</typeparam>
        /// <returns>The query used to make the singleton.</returns>
        /// <exception cref="InvalidOperationException">Thrown if more than one component exists on this Entity, rendering it invalid as a singleton.</exception>
        public static EntityQuery MakeQueryAndSingleton<T0>(EntityManager manager, Entity entity, T0 src)
            where T0 : struct, IComponentData
        {
            EntityQuery query = manager.CreateEntityQuery(typeof(T0));
            if(!manager.HasComponent<T0>(entity))
                manager.AddComponentData(entity, default(T0));
            
            if(manager.GetComponentCount(entity) > 1) throw new InvalidOperationException();
            
            query.SetSingleton(src);

            return query;
        }
    }
}

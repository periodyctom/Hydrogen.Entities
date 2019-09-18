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
        
        public bool Exists
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Current != Entity.Null;
        }

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
            if (Exists)
            {
                Query.SetSingleton(value);
            }
            else
            {
                Current = Manager.CreateEntity(Archetype);
                Query.SetSingleton(value);
            }
        }

        public void Destroy()
        {
            if (!Exists) return;
            Assert.IsTrue(Manager.IsCreated);
            
            Manager.DestroyEntity(Current);
            Current = Entity.Null;
        }

        public void Dispose()
        {
            Destroy();
            Query.Dispose();
        }
    }
}

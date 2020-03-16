using System;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Tests;
using UnityEngine;
using UnityEngine.TestTools.Utils;

namespace Hydrogen.Entities.Tests
{
    using LocalesRef = BlobRefData<Locales>;

    public class SingletonConversionTestFixture : ECSTestsFixture
    {
        protected struct SingletonQueries : IDisposable
        {
            public readonly EntityQuery PreConverted;
            public readonly EntityQuery PostConverted;
            public readonly EntityQuery Singleton;

            static readonly Type sm_convertedType = typeof(SingletonConverted);

            public static SingletonQueries CreateQueries<T0, T1>(EntityManager manager)
                where T0 : struct, IComponentData
                where T1 : struct, ISingletonConverter<T0>
            {
                var converterTypeRO = ComponentType.ReadOnly<T1>();

                var preConverted = manager.CreateEntityQuery(
                    converterTypeRO,
                    ComponentType.Exclude(sm_convertedType));

                var postConverted = manager.CreateEntityQuery(
                    converterTypeRO,
                    ComponentType.ReadOnly(sm_convertedType));

                var singleton = manager.CreateEntityQuery(typeof(T0));

                return new SingletonQueries(preConverted, postConverted, singleton);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            SingletonQueries(EntityQuery preConverted, EntityQuery postConverted, EntityQuery singleton)
            {
                PreConverted = preConverted;
                PostConverted = postConverted;
                Singleton = singleton;
            }

            public void AssertCounts(int preCount, int postCount, int singletonCount)
            {
                Assert.IsTrue(PreConverted.CalculateEntityCount() == preCount);
                Assert.IsTrue(PostConverted.CalculateEntityCount() == postCount);
                Assert.IsTrue(Singleton.CalculateEntityCount() == singletonCount);
            }

            public void Dispose()
            {
                PreConverted.Dispose();
                PostConverted.Dispose();
                Singleton.Dispose();
            }
        }

        protected SingletonQueries TimeConfigQueries;
        protected SingletonQueries LocalesQueries;

        protected readonly Action<TimeConfig, TimeConfigConverter> CachedAssertTimeConfigs = AssertTimeConfig;
        protected readonly Action<LocalesRef, LocalesConverter> CachedAssertSupportedLocales = AssertSupportedLocales;

        protected static void AssertTimeConfig(TimeConfig current, TimeConfigConverter other)
        {
            Assert.IsTrue(Utils.AreFloatsEqual(current.FixedDeltaTime, other.Singleton.FixedDeltaTime, float.Epsilon));
            Assert.IsTrue(current.AppTargetFrameRate == other.Singleton.AppTargetFrameRate);

            Assert.IsTrue(Utils.AreFloatsEqual(current.FixedDeltaTime, Time.fixedDeltaTime, float.Epsilon));
            Assert.IsTrue(current.AppTargetFrameRate == (uint) Application.targetFrameRate);
        }

        protected static void AssertSupportedLocales(LocalesRef current, LocalesConverter other)
        {
            Assert.IsTrue(current.IsCreated && other.Singleton.IsCreated);

            ref var a = ref current.Resolve;
            ref var b = ref other.Singleton.Resolve;

            var availableLen = a.Available.Length;

            for (var i = 0; i < availableLen; i++)
            {
                ref var aAvailable = ref a.Available[i];
                ref var bAvailable = ref b.Available[i];
                Assert.IsTrue(aAvailable.ToString().Equals(bAvailable.ToString()));
            }
        }

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            TimeConfigQueries = SingletonQueries.CreateQueries<TimeConfig, TimeConfigConverter>(m_Manager);
            LocalesQueries = SingletonQueries.CreateQueries<LocalesRef, LocalesConverter>(m_Manager);

            var world = m_Manager.World;

            var initGroup = world.GetOrCreateSystem<InitializationSystemGroup>();
            var convertGroup = world.GetOrCreateSystem<SingletonConvertGroup>();
            var postConvertGroup = world.GetOrCreateSystem<SingletonPostConvertGroup>();
            
            initGroup.AddSystemToUpdateList(convertGroup);
            initGroup.AddSystemToUpdateList(postConvertGroup);
            
            convertGroup.AddSystemToUpdateList(world.CreateSystem<LocalesConvertSystem>());
            convertGroup.AddSystemToUpdateList(world.CreateSystem<TimeConfigConvertSystem>());
            
            postConvertGroup.AddSystemToUpdateList(world.CreateSystem<LocalesChangedSystem>());
            postConvertGroup.AddSystemToUpdateList(world.CreateSystem<LocalesChangedJobSystem>());
            postConvertGroup.AddSystemToUpdateList(world.CreateSystem<LocalesUnchangedSystem>());
            postConvertGroup.AddSystemToUpdateList(world.CreateSystem<LocalesUnchangedJobSystem>());
            postConvertGroup.AddSystemToUpdateList(world.CreateSystem<TimeConfigChangedSystem>());
            postConvertGroup.AddSystemToUpdateList(world.CreateSystem<TimeConfigChangedJobSystem>());
            postConvertGroup.AddSystemToUpdateList(world.CreateSystem<TimeConfigUnchangedSystem>());
            postConvertGroup.AddSystemToUpdateList(world.CreateSystem<TimeConfigUnchangedJobSystem>());
            
            initGroup.SortSystemUpdateList();
            convertGroup.SortSystemUpdateList();
            postConvertGroup.SortSystemUpdateList();
        }

        [TearDown]
        public override void TearDown()
        {
            TimeConfigQueries.Dispose();
            LocalesQueries.Dispose();

            base.TearDown();
        }

        public static BlobAssetReference<Locales> CreateLocaleData(string name, params string[] available)
        {
            Assert.IsNotNull(available);
            Assert.IsTrue(available.Length > 0);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<Locales>();
            
            if(!string.IsNullOrEmpty(name) && name.Length > 0)
                builder.AllocateString(ref root.Name, name);
            else
                root.Name = new BlobString();

            var availableLen = available.Length;
            var builderArray = builder.Allocate(ref root.Available, available.Length);

            for (var i = 0; i < availableLen; i++)
            {
                ref var element = ref builderArray[i];
                builder.AllocateString(ref element, available[i]);
            }

            var refData = builder.CreateBlobAssetReference<Locales>(Allocator.Persistent);

            builder.Dispose();

            return refData;
        }

        protected static LocalesRef CreateLocaleRefData(string name, params string[] available)
        {
            return new LocalesRef(CreateLocaleData(name, available));
        }
    }
}

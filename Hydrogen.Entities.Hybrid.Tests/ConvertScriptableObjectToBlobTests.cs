using System;
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Tests;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hydrogen.Entities.Tests
{
    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
    public class ConvertScriptableObjectToBlobTests : ECSTestsFixture
    {
        private static GameObject LoadPrefab(string name)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Packages/com.periodyc.hydrogen.entities/Hydrogen.Entities.Hybrid.Tests/{name}.prefab");
        }
        
        private const float kMeaningOfLifeFloat = 42.0f;
        private const uint kMeaningOfLifeUInt = 42u;
        private const string kFooText = "Lorem Ipsum";
        private static readonly int[] sm_integers = {0, 1, 2, 3};
        private static readonly BazData sm_testBaz = new BazData(1,2,3,4);
        
        private struct TestBlob01
        {
            public BlobString Name;
            public BlobPtr<float> AFloat;
            public BlobArray<int> Ints;
        }

        private class TestScriptableInterface01 : ScriptableObject, IConvertScriptableObjectToBlob<TestBlob01>
        {
            public float AFloat = kMeaningOfLifeFloat;
            public int[] Integers = sm_integers;

            public BlobAssetReference<TestBlob01> Convert(ScriptableObjectConversionSystem conversion)
            {
                var builder = new BlobBuilder(Allocator.Temp);

                ref TestBlob01 target = ref builder.ConstructRoot<TestBlob01>();

                if(!string.IsNullOrEmpty(name))
                    builder.AllocateString(ref target.Name, name);
                else
                    target.Name = new BlobString();

                ref float afloat = ref builder.Allocate(ref target.AFloat);

                afloat = AFloat;

                int intsLen = Integers.Length;

                if (intsLen > 0)
                {
                    BlobBuilderArray<int> intsBuilder = builder.Allocate(ref target.Ints, intsLen);

                    for (int i = 0; i < intsLen; i++)
                    {
                        ref int val = ref intsBuilder[i];
                        val = Integers[i];
                    }
                }
                else
                    target.Ints = new BlobArray<int>();

                BlobAssetReference<TestBlob01> assetRef =
                    builder.CreateBlobAssetReference<TestBlob01>(Allocator.Persistent);

                builder.Dispose();

                return assetRef;
            }
        }

        private class TestScriptableCustomFunc : ScriptableObject
        {
            public string Foo = kFooText;
            public uint Bar = kMeaningOfLifeUInt;
            public BazData Baz = sm_testBaz;
        }

        private struct TestBlob02
        {
            public BlobString Foo;
            public uint Bar;
            public BlobPtr<BazData> Baz;
        }
        
        private class NodeDefinition : ScriptableObject, IConvertScriptableObjectToBlob<NodeBlob>
        {
            public int Value = 0;
            
            public NodeDefinition Left;
            public NodeDefinition Right;

            public BlobAssetReference<NodeBlob> Convert(ScriptableObjectConversionSystem conversion)
            {
                var builder = new BlobBuilder(Allocator.Temp);

                ref NodeBlob dst = ref builder.ConstructRoot<NodeBlob>();

                BlobAssetReference<NodeBlob> left = Left != null
                    ? conversion.GetBlob<NodeDefinition, NodeBlob>(Left)
                    : BlobAssetReference<NodeBlob>.Null;

                BlobAssetReference<NodeBlob> right = Right != null
                    ? conversion.GetBlob<NodeDefinition, NodeBlob>(Right)
                    : BlobAssetReference<NodeBlob>.Null;

                dst.Left = left;
                dst.Right = right;
                dst.Value = Value;

                BlobAssetReference<NodeBlob> result = builder.CreateBlobAssetReference<NodeBlob>(Allocator.Persistent);
                
                builder.Dispose();

                return result;
            }
        }

        private struct NodeBlob
        {
            public BlobAssetReference<NodeBlob> Left;
            public BlobAssetReference<NodeBlob> Right;
            public int Value;
        }

        [Serializable]
        private struct BazData : IEquatable<BazData>
        {
            public uint A;
            public uint B;
            public uint C;
            public uint D;

            public BazData(uint a, uint b, uint c, uint d)
            {
                A = a;
                B = b;
                C = c;
                D = d;
            }

            public bool Equals(BazData other) => A == other.A && B == other.B && C == other.C && D == other.D;

            public override bool Equals(object obj) => obj is BazData other && Equals(other);

            [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = (int) A;
                    hashCode = (hashCode * 397) ^ (int) B;
                    hashCode = (hashCode * 397) ^ (int) C;
                    hashCode = (hashCode * 397) ^ (int) D;

                    return hashCode;
                }
            }
        }

        private static BlobAssetReference<TestBlob02> ConvertCustomToBlob02(
            TestScriptableCustomFunc src,
            ScriptableObjectConversionSystem conversion)
        {
            var builder = new BlobBuilder(Allocator.Temp);

            ref TestBlob02 dst = ref builder.ConstructRoot<TestBlob02>();

            builder.AllocateString(ref dst.Foo, src.Foo);

            dst.Bar = src.Bar;

            ref BazData bazData = ref builder.Allocate(ref dst.Baz);

            bazData = src.Baz;

            BlobAssetReference<TestBlob02> result = builder.CreateBlobAssetReference<TestBlob02>(Allocator.Persistent);
            
            builder.Dispose();

            return result;
        }

        private static BlobAssetReference<TestBlob01> ConvertInterfaceToBlob01(
            TestScriptableInterface01 src,
            ScriptableObjectConversionSystem conversion)
        {
            var builder = new BlobBuilder(Allocator.Temp);

            ref TestBlob01 target = ref builder.ConstructRoot<TestBlob01>();

            if(!string.IsNullOrEmpty(src.name))
                builder.AllocateString(ref target.Name, src.name);
            else
                target.Name = new BlobString();

            ref float afloat = ref builder.Allocate(ref target.AFloat);

            afloat = src.AFloat;

            int intsLen = src.Integers.Length;

            if (intsLen > 0)
            {
                BlobBuilderArray<int> intsBuilder = builder.Allocate(ref target.Ints, intsLen);

                for (int i = 0; i < intsLen; i++)
                    intsBuilder[i] = src.Integers[i];
            }
            else
                target.Ints = new BlobArray<int>();

            BlobAssetReference<TestBlob01> result =
                builder.CreateBlobAssetReference<TestBlob01>(Allocator.Persistent);
            
            builder.Dispose();

            return result;
        }

        private static readonly ScriptToBlobFunc<TestScriptableCustomFunc, TestBlob02> sm_convertCustomToBlob02 =
            ConvertCustomToBlob02;

        private static readonly ScriptToBlobFunc<TestScriptableInterface01, TestBlob01> sm_convertInterfaceToBlob01 =
            ConvertInterfaceToBlob01;

        private static void TryDisposeBlob<T>(in BlobAssetReference<T> reference)
            where T : struct
        {
            // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
            if(reference.IsCreated) reference.Dispose();
        }

        private void AssertPrefabCollection(EntityQuery query, int expectedCount, bool expectNoPrefabCollectionRef = false)
        {
            Assert.IsTrue(query.CalculateEntityCount() == expectedCount);
            NativeArray<Entity> entities = query.ToEntityArray(Allocator.TempJob);

            try
            {
                int len = entities.Length;
                for (int i = 0; i < len; i++)
                {
                    Entity entity = entities[i];
                    AssertPrefabCollection(entity, expectNoPrefabCollectionRef);
                }
            }
            finally
            {
                entities.Dispose();
            }
        }

        private void AssertPrefabCollection(Entity entity, bool assertNoPrefabCollection = false)
        {
            var collectionReference = m_Manager.GetComponentData<PrefabCollectionReference>(entity);

            Assert.IsTrue(collectionReference.IsCreated);

            ref PrefabCollectionBlob blob = ref collectionReference.Resolve;

            int len1 = blob.Prefabs.Length;
            Assert.IsTrue(len1 == 2);

            for (int j = 0; j < len1; j++)
            {
                ref Entity e = ref blob.Prefabs[j];
                Assert.IsTrue(m_Manager.Exists(e));
                Assert.IsTrue(m_Manager.GetComponentCount(e) > 1);
                Assert.IsTrue(!assertNoPrefabCollection || !m_Manager.HasComponent<PrefabCollectionReference>(e));
            }
        }

        private ScriptableObjectConversionSystem m_conversion;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            m_conversion = World.GetOrCreateSystem<ScriptableObjectConversionSystem>();
        }
        
        [Test]
        public void ConvertScriptableObjectToBlob_WithInterface()
        {
            var src = ScriptableObject.CreateInstance<TestScriptableInterface01>();
            BlobAssetReference<TestBlob01> reference = BlobAssetReference<TestBlob01>.Null;

            try
            {
                Assert.NotNull(src);

                // ReSharper disable once CompareOfFloatsByEqualityOperator
                Assert.IsTrue(src.AFloat == kMeaningOfLifeFloat);
                Assert.IsTrue(src.Integers.Length == 4);
                Assert.IsTrue(src.Integers[0] == 0 && src.Integers[1] == 1 && src.Integers[2] == 2 && src.Integers[3] == 3);
            
                reference = m_conversion.GetBlob<TestScriptableInterface01, TestBlob01>(src);
                Assert.IsTrue(reference.IsCreated);

                ref TestBlob01 dst = ref reference.Value;
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                Assert.IsTrue(src.AFloat == dst.AFloat.Value);
                Assert.IsTrue(src.name == dst.Name.ToString());
                Assert.IsTrue(src.Integers.Length == dst.Ints.Length);
                
                for (int i = 0; i < 4; i++)
                    Assert.IsTrue(src.Integers[i] == dst.Ints[i]);
            }
            finally
            {
                Object.DestroyImmediate(src);
                TryDisposeBlob(reference);
            }
        }
        
        [Test]
        public void ConvertScriptableObjectToBlob_WithFunction()
        {
            var src = ScriptableObject.CreateInstance<TestScriptableCustomFunc>();
            BlobAssetReference<TestBlob02> reference = BlobAssetReference<TestBlob02>.Null;

            try
            {
                Assert.NotNull(src);

                // ReSharper disable once CompareOfFloatsByEqualityOperator
                Assert.IsTrue(src.Foo == kFooText);
                Assert.IsTrue(src.Bar == kMeaningOfLifeUInt);
                Assert.IsTrue(src.Baz.Equals(sm_testBaz));
            
                reference = m_conversion.GetBlob(src, sm_convertCustomToBlob02);
                Assert.IsTrue(reference.IsCreated);

                ref TestBlob02 dst = ref reference.Value;
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                Assert.IsTrue(src.Foo == dst.Foo.ToString());
                Assert.IsTrue(src.Bar == dst.Bar);
                Assert.IsTrue(src.Baz.Equals(dst.Baz.Value));
            }
            finally
            {
                Object.DestroyImmediate(src);
                TryDisposeBlob(reference);
            }
        }

        [Test]
        public void ConvertScriptableObjectToBlob_WithBoth()
        {
            var original = ScriptableObject.CreateInstance<TestScriptableInterface01>();
            original.name = "Test";
            
            BlobAssetReference<TestBlob01> interfaceTest = BlobAssetReference<TestBlob01>.Null;
            BlobAssetReference<TestBlob01> functionTest = BlobAssetReference<TestBlob01>.Null;
            
            try
            {
                Assert.IsTrue(original.name == "Test");
                Assert.IsTrue(original.AFloat == kMeaningOfLifeFloat);
                Assert.IsTrue(
                    original.Integers[0] == sm_integers[0]
                 && original.Integers[1] == sm_integers[1]
                 && original.Integers[2] == sm_integers[2]
                 && original.Integers[3] == sm_integers[3]);

                interfaceTest = m_conversion.GetBlob<TestScriptableInterface01, TestBlob01>(original);
                functionTest = m_conversion.GetBlob(original, sm_convertInterfaceToBlob01);

                ref TestBlob01 interfaceBlob = ref interfaceTest.Value;
                ref TestBlob01 functionBlob = ref functionTest.Value;
                
                Assert.IsTrue(interfaceBlob.Name.ToString() == functionBlob.Name.ToString());
                Assert.IsTrue(interfaceBlob.AFloat.Value == functionBlob.AFloat.Value);
                Assert.IsTrue(interfaceBlob.Ints.Length == functionBlob.Ints.Length);
                
                for (int i = 0; i < 4; i++)
                    Assert.IsTrue(interfaceBlob.Ints[i] == functionBlob.Ints[i]);
                
                Assert.IsTrue(interfaceBlob.Name.ToString() == original.name);
                Assert.IsTrue(interfaceBlob.AFloat.Value == original.AFloat);
                Assert.IsTrue(interfaceBlob.Ints.Length == original.Integers.Length);
                
                for (int i = 0; i < 4; i++)
                    Assert.IsTrue(interfaceBlob.Ints[i] == original.Integers[i]);
            }
            finally
            {
                Object.DestroyImmediate(original);
                TryDisposeBlob(interfaceTest);
                TryDisposeBlob(functionTest); 
            }
        }

        [Test]
        public void ConvertScriptableObjectToBlob_WithChainedBlobReferences()
        {
            var a = ScriptableObject.CreateInstance<NodeDefinition>();
            var b = ScriptableObject.CreateInstance<NodeDefinition>();
            var c = ScriptableObject.CreateInstance<NodeDefinition>();
            var d = ScriptableObject.CreateInstance<NodeDefinition>();
            var e = ScriptableObject.CreateInstance<NodeDefinition>();

            a.Value = 0;
            a.Left = b;
            a.Right = c;

            b.Value = 1;
            b.Left = d;
            b.Right = null;

            c.Value = 2;
            c.Left = null;
            c.Right = e;

            d.Value = 3;
            e.Value = 4;
            
            BlobAssetReference<NodeBlob> aBlob = BlobAssetReference<NodeBlob>.Null;
            BlobAssetReference<NodeBlob> bBlob = BlobAssetReference<NodeBlob>.Null;
            BlobAssetReference<NodeBlob> cBlob = BlobAssetReference<NodeBlob>.Null;
            BlobAssetReference<NodeBlob> dBlob = BlobAssetReference<NodeBlob>.Null;
            BlobAssetReference<NodeBlob> eBlob = BlobAssetReference<NodeBlob>.Null;

            try
            {
                aBlob = m_conversion.GetBlob<NodeDefinition, NodeBlob>(a);
                bBlob = m_conversion.GetBlob<NodeDefinition, NodeBlob>(b);
                cBlob = m_conversion.GetBlob<NodeDefinition, NodeBlob>(c);
                dBlob = m_conversion.GetBlob<NodeDefinition, NodeBlob>(d);
                eBlob = m_conversion.GetBlob<NodeDefinition, NodeBlob>(e);
                
                Assert.IsTrue(aBlob.Value.Value == 0);
                Assert.IsTrue(bBlob.Value.Value == 1);
                Assert.IsTrue(cBlob.Value.Value == 2);
                Assert.IsTrue(dBlob.Value.Value == 3);
                Assert.IsTrue(eBlob.Value.Value == 4);
                
                Assert.IsTrue(aBlob.Value.Left == bBlob);
                Assert.IsTrue(aBlob.Value.Right == cBlob);
                Assert.IsTrue(bBlob.Value.Left == dBlob);
                Assert.IsTrue(bBlob.Value.Right == BlobAssetReference<NodeBlob>.Null);
                Assert.IsTrue(cBlob.Value.Left == BlobAssetReference<NodeBlob>.Null);
                Assert.IsTrue(cBlob.Value.Right == eBlob);
                Assert.IsTrue(dBlob.Value.Left == BlobAssetReference<NodeBlob>.Null);
                Assert.IsTrue(dBlob.Value.Right == BlobAssetReference<NodeBlob>.Null);
                Assert.IsTrue(eBlob.Value.Left == BlobAssetReference<NodeBlob>.Null);
                Assert.IsTrue(eBlob.Value.Right == BlobAssetReference<NodeBlob>.Null);
            }
            finally
            {
                Object.DestroyImmediate(a);
                Object.DestroyImmediate(b);
                Object.DestroyImmediate(c);
                Object.DestroyImmediate(d);
                Object.DestroyImmediate(e);
                
                TryDisposeBlob(aBlob);
                TryDisposeBlob(bBlob);
                TryDisposeBlob(cBlob);
                TryDisposeBlob(dBlob);
                TryDisposeBlob(eBlob);
            }
        }

        [Test]
        public void ConvertScriptableObjectToBlob_WithPrefabReferences()
        {
            GameObject prefab = LoadPrefab("LeafPrefabCollection_00");
            
            Assert.IsNotNull(prefab);

            GameObject instance = Object.Instantiate(prefab);

            try
            {
                Assert.IsNotNull(instance);
            }
            catch (Exception)
            {
                Object.DestroyImmediate(instance);
                throw;
            }
            
            GameObjectConversionUtility.ConvertGameObjectHierarchy(instance.gameObject, World);

            EntityQuery query = m_Manager.CreateEntityQuery(ComponentType.ReadOnly<PrefabCollectionReference>());
            
            try
            {
                AssertPrefabCollection(query, 1, true);
            }
            finally
            {
                query.Dispose();
            }
        }

        [Test]
        public void ConvertScriptableObjectToBlob_WithPrefabsThatAlsoReferenceBlobs()
        {
            GameObject prefab = LoadPrefab("RootPrefabCollection");
            
            Assert.IsNotNull(prefab);

            GameObject instance = Object.Instantiate(prefab);

            try
            {
                Assert.IsNotNull(instance);
            }
            catch (Exception)
            {
                Object.DestroyImmediate(instance);
                throw;
            }
            
            GameObjectConversionUtility.ConvertGameObjectHierarchy(instance.gameObject, World);

            ComponentType prefabRefTypeRO = ComponentType.ReadOnly<PrefabCollectionReference>();
            
            EntityQuery liveQuery = m_Manager.CreateEntityQuery(prefabRefTypeRO);
            EntityQuery prefabQuery = m_Manager.CreateEntityQuery(prefabRefTypeRO, ComponentType.ReadOnly<Prefab>());
            
            try
            {
                AssertPrefabCollection(liveQuery, 1);
                AssertPrefabCollection(prefabQuery, 4);
            }
            finally
            {
                liveQuery.Dispose();
                prefabQuery.Dispose();
            }
        }
    }
}

using System;
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Tests;
using UnityEngine;
using UnityEngine.TestTools.Utils;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace
namespace Hydrogen.Entities.Tests
{
    public class ConvertScriptableObjectToBlobTests : ECSTestsFixture
    {
        const float kMeaningOfLifeFloat = 42.0f;
        const uint kMeaningOfLifeUInt = 42u;
        const string kFooText = "Lorem Ipsum";
        static readonly int[] sm_integers = {0, 1, 2, 3};
        static readonly BazData sm_testBaz = new BazData(1,2,3,4);

        GameObjectConversionSettings MakeDefaultSettings() =>
            new GameObjectConversionSettings()
            {
                DestinationWorld = World,
                ConversionFlags = GameObjectConversionUtility.ConversionFlags.AssignName
            };

        struct TestBlob01
        {
            public BlobString Name;
            public BlobPtr<float> AFloat;
            public BlobArray<int> Ints;
        }

        class TestScriptableInterface01 : ScriptableObject, IConvertScriptableObjectToBlob<TestBlob01>
        {
            public float AFloat = kMeaningOfLifeFloat;
            public int[] Integers = sm_integers;

            public BlobAssetReference<TestBlob01> Convert(ScriptableObjectConversionSystem conversion)
            {
                var builder = new BlobBuilder(Allocator.Temp);

                ref var target = ref builder.ConstructRoot<TestBlob01>();

                if(!string.IsNullOrEmpty(name) && name.Length > 0)
                    builder.AllocateString(ref target.Name, name);
                else
                    target.Name = new BlobString();

                ref var afloat = ref builder.Allocate(ref target.AFloat);

                afloat = AFloat;

                var intsLen = Integers.Length;

                if (intsLen > 0)
                    builder.Construct(ref target.Ints, Integers);
                else
                    target.Ints = new BlobArray<int>();

                var assetRef =
                    builder.CreateBlobAssetReference<TestBlob01>(Allocator.Persistent);

                builder.Dispose();

                return assetRef;
            }
        }

        class TestScriptableCustomFunc : ScriptableObject
        {
            public string Foo = kFooText;
            public uint Bar = kMeaningOfLifeUInt;
            public BazData Baz = sm_testBaz;
        }

        struct TestBlob02
        {
            public BlobString Foo;
            public uint Bar;
            public BlobPtr<BazData> Baz;
        }

        class NodeDefinition : ScriptableObject, IConvertScriptableObjectToBlob<NodeBlob>
        {
            public int Value;
            
            public NodeDefinition Left;
            public NodeDefinition Right;

            public BlobAssetReference<NodeBlob> Convert(ScriptableObjectConversionSystem conversion)
            {
                var builder = new BlobBuilder(Allocator.Temp);

                ref var dst = ref builder.ConstructRoot<NodeBlob>();

                var left = Left != null
                    ? conversion.GetBlob<NodeDefinition, NodeBlob>(Left)
                    : BlobAssetReference<NodeBlob>.Null;

                var right = Right != null
                    ? conversion.GetBlob<NodeDefinition, NodeBlob>(Right)
                    : BlobAssetReference<NodeBlob>.Null;

                dst.Left = left;
                dst.Right = right;
                dst.Value = Value;

                var result = builder.CreateBlobAssetReference<NodeBlob>(Allocator.Persistent);
                
                builder.Dispose();

                return result;
            }
        }

        struct NodeBlob
        {
            public BlobAssetReference<NodeBlob> Left;
            public BlobAssetReference<NodeBlob> Right;
            public int Value;
        }

        [Serializable]
        struct BazData : IEquatable<BazData>
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
                    var hashCode = (int) A;
                    hashCode = (hashCode * 397) ^ (int) B;
                    hashCode = (hashCode * 397) ^ (int) C;
                    hashCode = (hashCode * 397) ^ (int) D;

                    return hashCode;
                }
            }
        }

        static BlobAssetReference<TestBlob02> ConvertCustomToBlob02(
            TestScriptableCustomFunc src,
            ScriptableObjectConversionSystem conversion)
        {
            var builder = new BlobBuilder(Allocator.Temp);

            ref var dst = ref builder.ConstructRoot<TestBlob02>();

            builder.AllocateString(ref dst.Foo, src.Foo);

            dst.Bar = src.Bar;

            ref var bazData = ref builder.Allocate(ref dst.Baz);

            bazData = src.Baz;

            var result = builder.CreateBlobAssetReference<TestBlob02>(Allocator.Persistent);
            
            builder.Dispose();

            return result;
        }

        static BlobAssetReference<TestBlob01> ConvertInterfaceToBlob01(
            TestScriptableInterface01 src,
            ScriptableObjectConversionSystem conversion)
        {
            var builder = new BlobBuilder(Allocator.Temp);

            ref var target = ref builder.ConstructRoot<TestBlob01>();

            if(!string.IsNullOrEmpty(src.name))
                builder.AllocateString(ref target.Name, src.name);
            else
                target.Name = new BlobString();

            ref var afloat = ref builder.Allocate(ref target.AFloat);

            afloat = src.AFloat;

            var intsLen = src.Integers.Length;

            if (intsLen > 0)
                builder.Construct(ref target.Ints, src.Integers);
            else
                target.Ints = new BlobArray<int>();

            var result =
                builder.CreateBlobAssetReference<TestBlob01>(Allocator.Persistent);
            
            builder.Dispose();

            return result;
        }

        static readonly ScriptToBlobFunc<TestScriptableCustomFunc, TestBlob02> sm_convertCustomToBlob02 =
            ConvertCustomToBlob02;

        static readonly ScriptToBlobFunc<TestScriptableInterface01, TestBlob01> sm_convertInterfaceToBlob01 =
            ConvertInterfaceToBlob01;

        static void TryDisposeBlob<T>(in BlobAssetReference<T> reference)
            where T : struct
        {
            // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
            if(reference.IsCreated) reference.Dispose();
        }

        ScriptableObjectConversionSystem m_conversion;

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
            var reference = BlobAssetReference<TestBlob01>.Null;

            try
            {
                Assert.NotNull(src);

                // ReSharper disable once CompareOfFloatsByEqualityOperator
                Assert.IsTrue(src.AFloat == kMeaningOfLifeFloat);
                Assert.IsTrue(src.Integers.Length == 4);
                Assert.IsTrue(src.Integers[0] == 0 && src.Integers[1] == 1 && src.Integers[2] == 2 && src.Integers[3] == 3);
            
                reference = m_conversion.GetBlob<TestScriptableInterface01, TestBlob01>(src);
                Assert.IsTrue(reference.IsCreated);

                ref var dst = ref reference.Value;
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                Assert.IsTrue(src.AFloat == dst.AFloat.Value);
                Assert.IsTrue(src.name == dst.Name.ToString());
                Assert.IsTrue(src.Integers.Length == dst.Ints.Length);
                
                for (var i = 0; i < 4; i++)
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
            var reference = BlobAssetReference<TestBlob02>.Null;

            try
            {
                Assert.NotNull(src);

                // ReSharper disable once CompareOfFloatsByEqualityOperator
                Assert.IsTrue(src.Foo == kFooText);
                Assert.IsTrue(src.Bar == kMeaningOfLifeUInt);
                Assert.IsTrue(src.Baz.Equals(sm_testBaz));
            
                reference = m_conversion.GetBlob(src, sm_convertCustomToBlob02);
                Assert.IsTrue(reference.IsCreated);

                ref var dst = ref reference.Value;
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
            
            var interfaceTest = BlobAssetReference<TestBlob01>.Null;
            var functionTest = BlobAssetReference<TestBlob01>.Null;
            
            try
            {
                Assert.IsTrue(original.name == "Test");
                Assert.IsTrue(Utils.AreFloatsEqual(original.AFloat, kMeaningOfLifeFloat, float.Epsilon));
                Assert.IsTrue(
                    original.Integers[0] == sm_integers[0]
                 && original.Integers[1] == sm_integers[1]
                 && original.Integers[2] == sm_integers[2]
                 && original.Integers[3] == sm_integers[3]);

                interfaceTest = m_conversion.GetBlob<TestScriptableInterface01, TestBlob01>(original);
                functionTest = m_conversion.GetBlob(original, sm_convertInterfaceToBlob01);

                ref var interfaceBlob = ref interfaceTest.Value;
                ref var functionBlob = ref functionTest.Value;
                
                Assert.IsTrue(interfaceBlob.Name.ToString() == functionBlob.Name.ToString());
                Assert.IsTrue(
                    Utils.AreFloatsEqual(interfaceBlob.AFloat.Value, functionBlob.AFloat.Value, float.Epsilon));
                Assert.IsTrue(interfaceBlob.Ints.Length == functionBlob.Ints.Length);
                
                for (var i = 0; i < 4; i++)
                    Assert.IsTrue(interfaceBlob.Ints[i] == functionBlob.Ints[i]);
                
                Assert.IsTrue(interfaceBlob.Name.ToString() == original.name);
                Assert.IsTrue(Utils.AreFloatsEqual(interfaceBlob.AFloat.Value, original.AFloat, float.Epsilon));
                Assert.IsTrue(interfaceBlob.Ints.Length == original.Integers.Length);
                
                for (var i = 0; i < 4; i++)
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
            
            var aBlob = BlobAssetReference<NodeBlob>.Null;
            var bBlob = BlobAssetReference<NodeBlob>.Null;
            var cBlob = BlobAssetReference<NodeBlob>.Null;
            var dBlob = BlobAssetReference<NodeBlob>.Null;
            var eBlob = BlobAssetReference<NodeBlob>.Null;

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
    }
}

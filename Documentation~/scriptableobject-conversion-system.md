# **ScriptableObjectConversionSystem**

Similar to and piggybacking on the [GameObject](https://docs.unity3d.com/ScriptReference/GameObject.html) conversion pipeline. The ScriptableObjectConversionSystems has functions for creating Blob assets and [BlobAssetReference&lt;T&gt;](https://docs.unity3d.com/Packages/com.unity.entities@0.1/api/Unity.Entities.BlobAssetReference-1.html) to them from [ScriptableObjects](https://docs.unity3d.com/ScriptReference/ScriptableObject.html).
The conversion system caches created BlobAssetReferences&lt;T&gt; so given the same inputs, you should get the same result. This allows a converted SO to be referenced in more than one place.

The conversion flow is also given access to the GameObjectConversionSystem used in converting GOs, so you can create SO conversions that reference converted prefabs, given the originating prefab references!

There are 2 main conversion routes:

## **IConvertScriptableObjectToBlob&lt;T&gt;**
An interface that should be used on a ScriptableObject that defines it's own conversion into a Blob Asset. 
This is used to create a **BlobAssetReference&lt;T&gt;** that points to the converted SO blob and can either be assigned in one place, or shared between multiple entities or blobs.

For example:
```cs
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
```


## **ScriptToBlobFunc&lt;in T0, T1&gt;**
A delegate for manual conversions that do not utilize the IConvertScriptableObjectToBlob&lt;T&gt; workflow, primarily intended for scenarios where access to the original SO code is limited or non-existant. Such as Unity-developed config data, or 3rd-party assets/packages where code changes would be overwritten.
It's recommended you use a static method as this will ensure a deterministic result if you intend to get the same SO conversion from multiple places.

For example:
```cs
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
```
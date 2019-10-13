# ScriptableObjectConversionSystem
Similar to and piggybacking on the GameObject conversion pipeline. The ScriptableObjectConversionSystems has functions for creating ```BlobAsset<T>``` and ```BlobAssetReference<T>``` to them from ```ScriptableObjects```.

The conversion system caches created BlobAssetReferences<T> so given the same inputs, you should get the same result. This allows a converted SO to be referenced in more than one place.

The conversion flow is also given access to the GameObjectConversionSystem used in converting GOs, so you can create SO conversions that reference converted prefabs, given the originating prefab references!

There are 2 main conversion routes:

## IConvertScriptableObjectToBlob<T>
An interface that should be used on a ScriptableObject that defines it's own conversion into a Blob Asset. 
This is used to create a BlobAssetReference<T> that points to the converted SO blob and can either be assigned in one place, or shared between multiple entities or blobs.


## ScriptToBlobFunc<in T0, T1>
A delegate for manual conversions that do not utilize the IConvertScriptableObjectToBlob<T> workflow, primarily intended for scenarios where access to the original SO code is limited or non-existant. Such as Unity-developed config data, or 3rd-party assets/packages where code changes would be overwritten.
It's recommended you use a static method as this will ensure a deterministic result if you intend to get the same SO conversion from multiple places. 
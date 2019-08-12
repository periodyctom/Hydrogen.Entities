#define DETAIL_MARKERS
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Assertions;

namespace Hydrogen.Entities
{
    /// <summary>
    /// Defines a delegate that takes a <see cref="ScriptableObject"/> and returns a <see cref="BlobAssetReference{T}"/>
    /// </summary>
    /// <param name="src">The SO to convert.</param>
    /// <param name="convert">The <see cref="ScriptableObjectConversionSystem"/>, allowing us to get references to other converted blobs.</param>
    /// <param name="goConvert">The <see cref="GameObjectConversionSystem"/>, allowing us to get prefab references if they've been declared.</param>
    /// <typeparam name="T0">The concrete type of the SO to be converted.</typeparam>
    /// <typeparam name="T1">The struct type our Blob asset reference points to.</typeparam>
    public delegate BlobAssetReference<T1> ScriptToBlobFunc<in T0, T1>(
        T0 src,
        ScriptableObjectConversionSystem convert,
        GameObjectConversionSystem goConvert)
        where T0 : ScriptableObject
        where T1 : struct;

    // TODO: Handle multi-type conversions IE SO T0 -> T1 and T2. Hash the types for the conversion entry.
    // TODO: Handle multi-type conversions with Delegates. Hash the type and delegate type for the conversion entry.
    // TODO: Handle Acyclic graphs? Someone will try to do that eventually. Probably me?
    // TODO: Test interactions with the GO system for prefabs.

    /// <summary>
    /// A System similar to the <see cref="GameObjectConversionSystem"/>, but helps with converting <see cref="ScriptableObjects"/> to <see cref="BlobAssetReference{T0}"/>.
    /// </summary>
    [DisableAutoCreation]
    public class ScriptableObjectConversionSystem : ComponentSystem
    {
        private unsafe struct BlobData
        {
            [NativeDisableUnsafePtrRestriction] private byte* _blobRef;
            private int _blobTypeHashCode;

            public bool IsValid => _blobRef != null;

            public int BlobTypeHashCode => _blobTypeHashCode;

            public static BlobData Create<T0>(BlobAssetReference<T0> reference)
                where T0 : struct
            {
                BlobData blobData = default;

                UnsafeUtility.CopyStructureToPtr(ref reference, &blobData._blobRef);
                blobData._blobTypeHashCode = typeof(T0).GetHashCode();

                return blobData;
            }

            public BlobAssetReference<T0> AsReference<T0>()
                where T0 : struct
            {
                Assert.AreEqual(_blobTypeHashCode, typeof(T0).GetHashCode());
                Assert.IsTrue(_blobRef != null);

                fixed (void* data = &_blobRef)
                {
                    UnsafeUtility.CopyPtrToStructure(data, out BlobAssetReference<T0> reference);

                    return reference;
                }
            }
        }

        private NativeHashMap<int, BlobData> _scriptableToBlob;

        private GameObjectConversionSystem _goConversionSystem;

        protected override void OnCreate()
        {
            _goConversionSystem = World.GetExistingSystem<GameObjectConversionSystem>();
            _scriptableToBlob = new NativeHashMap<int, BlobData>(100 * 1000, Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            _scriptableToBlob.Dispose();
        }

#if DETAIL_MARKERS
        private ProfilerMarker _createBlob = new ProfilerMarker("ScriptableObjectConversion.CreateBlob");

        private ProfilerMarker _createBlobWithFunc =
            new ProfilerMarker("ScriptableObjectConversion.CreateBlobWithFunc");
#endif


        private BlobData ConvertBlob<T0>(IConvertScriptableObjectToBlob<T0> src)
            where T0 : struct
        {
#if DETAIL_MARKERS
            using (_createBlob.Auto())
#endif
            {
                BlobAssetReference<T0> assetReference = src.Convert(this, _goConversionSystem);

                return BlobData.Create(assetReference);
            }
        }

        private BlobData ConvertBlob<T0, T1>(T0 obj, ScriptToBlobFunc<T0, T1> func)
            where T0 : ScriptableObject
            where T1 : struct
        {
#if DETAIL_MARKERS
            using (_createBlobWithFunc.Auto())
#endif
            {
                BlobAssetReference<T1> assetReference = func.Invoke(obj, this, _goConversionSystem);

                return BlobData.Create(assetReference);
            }
        }

        protected override void OnUpdate() { }

        /// <summary>
        /// Converts a <see cref="ScriptableObject"/> of <see cref="T0"/> to a blob reference of <see cref="T1"/>.
        /// The SO must implement <see cref="IConvertScriptableObjectToBlob{T1}"/>
        /// </summary>
        /// <param name="obj">The ScriptableObject to convert</param>
        /// <typeparam name="T0">Concrete type of ScriptableObject</typeparam>
        /// <typeparam name="T1">Type of the struct our Blob asset will reference</typeparam>
        /// <returns>The constructed <see cref="BlobAssetReference{T1}"/></returns>
        public BlobAssetReference<T1> GetBlob<T0, T1>(T0 obj)
            where T0 : ScriptableObject, IConvertScriptableObjectToBlob<T1>
            where T1 : struct
        {
            if (PreCheck(obj, out int instanceId, out BlobAssetReference<T1> blob))
                return blob;

            BlobData data = ConvertBlob(obj);

            return PostCheck<T1>(instanceId, data);
        }

        /// <summary>
        /// Converts a <see cref="ScriptableObject"/> of <see cref="T0"/> to a blob reference of <see cref="T1"/>.
        /// The user must provide a manual conversion function.
        /// The SO need not implement <see cref="IConvertScriptableObjectToBlob{T1}"/>, making it useful
        /// for converting pre-existing SO that are out of your control.
        /// </summary>
        /// <param name="obj">The SO to convert.</param>
        /// <param name="func">A delegate of <see cref="ScriptToBlobFunc{T0,T1}"/>. The concrete types must match.</param>
        /// <typeparam name="T0">The concrete type of the SO.</typeparam>
        /// <typeparam name="T1">Type of the struct our Blob asset will reference.</typeparam>
        /// <returns>The constructed <see cref="BlobAssetReference{T1}"/></returns>
        public BlobAssetReference<T1> GetBlob<T0, T1>(T0 obj, ScriptToBlobFunc<T0, T1> func)
            where T0 : ScriptableObject
            where T1 : struct
        {
            if (PreCheck(obj, out int instanceId, out BlobAssetReference<T1> blob))
                return blob;

            BlobData data = ConvertBlob(obj, func);

            return PostCheck<T1>(instanceId, data);
        }

        private BlobAssetReference<T0> PostCheck<T0>(int instanceId, BlobData data)
            where T0 : struct
        {
            if (_scriptableToBlob.TryAdd(instanceId, data))
                return data.AsReference<T0>();

            data.AsReference<T0>().Release();

            throw new InvalidOperationException();
        }

        private bool PreCheck<T0, T1>(T0 obj, out int instanceId, out BlobAssetReference<T1> blobAssetReference)
            where T0 : ScriptableObject
            where T1 : struct
        {
            blobAssetReference = default;

            if (obj == null)
                throw new NullReferenceException();

            instanceId = obj.GetInstanceID();

            if (!_scriptableToBlob.TryGetValue(instanceId, out BlobData data))
                return false;

            blobAssetReference = data.AsReference<T1>();

            return true;
        }

        /// <summary>
        /// Checks if a <see cref="T1"/> blob reference for the <see cref="ScriptableObject"/> of <see cref="T0"/> exits.
        /// </summary>
        /// <param name="obj">SO to Check.</param>
        /// <typeparam name="T0">Concrete type of the SO.</typeparam>
        /// <typeparam name="T1">Type of the struct our Blob asset will reference.</typeparam>
        /// <returns>True if the blob already exists and is of the correct type.</returns>
        public bool HasBlob<T0, T1>(T0 obj)
            where T0 : ScriptableObject
            where T1 : struct
        {
            return obj != null
                && _scriptableToBlob.TryGetValue(obj.GetInstanceID(), out BlobData data)
                && data.BlobTypeHashCode == typeof(T1).GetHashCode();
        }
    }
}

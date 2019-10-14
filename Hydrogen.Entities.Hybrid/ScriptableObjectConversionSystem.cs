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
    /// <typeparam name="T0">The concrete type of the SO to be converted.</typeparam>
    /// <typeparam name="T1">The struct type our Blob asset reference points to.</typeparam>
    public delegate BlobAssetReference<T1> ScriptToBlobFunc<in T0, T1>(
        T0 src,
        ScriptableObjectConversionSystem convert)
        where T0 : ScriptableObject
        where T1 : struct;

    // TODO: Handle Acyclic graphs? Someone will try to do that eventually. Probably me...

    /// <summary>
    /// A System similar to the <see cref="GameObjectConversionSystem"/>, but helps with converting <see cref="ScriptableObjects"/> to <see cref="BlobAssetReference{T0}"/>.
    /// </summary>
    [DisableAutoCreation]
    public class ScriptableObjectConversionSystem : ComponentSystem
    {
        private unsafe struct BlobData
        {
            [NativeDisableUnsafePtrRestriction] private byte* m_blobRef;

            public static BlobData Create<T0>(BlobAssetReference<T0> reference, int identifier)
                where T0 : struct
            {
                BlobData blobData = default;

                UnsafeUtility.CopyStructureToPtr(ref reference, &blobData.m_blobRef);

                return blobData;
            }

            public BlobAssetReference<T0> AsReference<T0>()
                where T0 : struct
            {
                Assert.IsTrue(m_blobRef != null);

                fixed (void* data = &m_blobRef)
                {
                    UnsafeUtility.CopyPtrToStructure(data, out BlobAssetReference<T0> reference);

                    return reference;
                }
            }
        }

        private NativeHashMap<int, BlobData> m_scriptableToBlob;

        private GameObjectConversionSystem m_goConversionSystem;

        public GameObjectConversionSystem GoConversionSystem
        {
            get
            {
                if (m_goConversionSystem != null)
                    return m_goConversionSystem;
                
                m_goConversionSystem = World.GetExistingSystem<GameObjectConversionSystem>();
                Assert.IsNotNull(
                    m_goConversionSystem,
                    "Null GO Conversion system, did you mean to Get this System from the GameObject conversion world instead of the Destination World?");

                return m_goConversionSystem;
            }
        }

        protected override void OnCreate() =>
            m_scriptableToBlob = new NativeHashMap<int, BlobData>(100 * 1000, Allocator.Persistent);

        protected override void OnDestroy()
        {
            m_scriptableToBlob.Dispose();
        }

#if DETAIL_MARKERS
        private ProfilerMarker m_createBlob = new ProfilerMarker("ScriptableObjectConversion.CreateBlob");

        private ProfilerMarker m_createBlobWithFunc =
            new ProfilerMarker("ScriptableObjectConversion.CreateBlobWithFunc");
#endif
        
        private BlobData ConvertBlob<T0>(IConvertScriptableObjectToBlob<T0> src, int identifier)
            where T0 : struct
        {
#if DETAIL_MARKERS
            using (m_createBlob.Auto())
#endif
            {
                BlobAssetReference<T0> assetReference = src.Convert(this);

                return BlobData.Create(assetReference, identifier);
            }
        }

        private BlobData ConvertBlob<T0, T1>(T0 obj, ScriptToBlobFunc<T0, T1> func, int identifier)
            where T0 : ScriptableObject
            where T1 : struct
        {
#if DETAIL_MARKERS
            using (m_createBlobWithFunc.Auto())
#endif
            {
                BlobAssetReference<T1> assetReference = func.Invoke(obj, this);

                return BlobData.Create(assetReference, identifier);
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
            int identifier = new Vector3Int(obj.GetInstanceID(), typeof(T0).GetHashCode(), typeof(T1).GetHashCode())
               .GetHashCode();
            
            if (PreCheck(obj, identifier, out BlobAssetReference<T1> blob))
                return blob;
            
            BlobData data = ConvertBlob(obj, identifier);

            return PostCheck<T1>(identifier, data);
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
            int identifier = new Vector2Int(obj.GetInstanceID(), func.GetHashCode()).GetHashCode();
            
            if (PreCheck(obj, identifier, out BlobAssetReference<T1> blob))
                return blob;
            
            BlobData data = ConvertBlob(obj, func, identifier);

            return PostCheck<T1>(identifier, data);
        }

        private BlobAssetReference<T0> PostCheck<T0>(int instanceId, BlobData data)
            where T0 : struct
        {
            if (m_scriptableToBlob.TryAdd(instanceId, data))
                return data.AsReference<T0>();

            data.AsReference<T0>().Release();

            throw new InvalidOperationException();
        }

        private bool PreCheck<T0, T1>(T0 obj, int identifier, out BlobAssetReference<T1> blobAssetReference)
            where T0 : ScriptableObject
            where T1 : struct
        {
            blobAssetReference = default;

            if (obj == null)
                throw new NullReferenceException();

            if (!m_scriptableToBlob.TryGetValue(identifier, out BlobData data))
                return false;

            blobAssetReference = data.AsReference<T1>();

            return true;
        }
    }
}

using System;
using Unity.Entities;

// ReSharper disable CheckNamespace

namespace Hydrogen.Entities.Tests
{
    [Serializable]
    public struct TestTimeConfig : IComponentData
    {
        public uint AppTargetFrameRate;
        public float FixedDeltaTime;

        public TestTimeConfig(uint appTargetFrameRate, float fixedDeltaTime)
        {
            AppTargetFrameRate = appTargetFrameRate;
            FixedDeltaTime = fixedDeltaTime;
        }
    }

    [Serializable]
    public struct TestSupportedLocales
    {
        public BlobPtr<NativeString64> Default;
        public BlobArray<NativeString64> Available;
    }
}
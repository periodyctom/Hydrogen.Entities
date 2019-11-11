using System;
using Unity.Entities;

namespace Hydrogen.Entities.Tests
{
    [Serializable]
    public struct TimeConfig : IComponentData
    {
        public uint AppTargetFrameRate;
        public float FixedDeltaTime;

        public TimeConfig(uint appTargetFrameRate, float fixedDeltaTime)
        {
            AppTargetFrameRate = appTargetFrameRate;
            FixedDeltaTime = fixedDeltaTime;
        }
    }

    [Serializable]
    public struct Locales
    {
        public BlobString Name;
        public BlobArray<BlobString> Available;
    }
}
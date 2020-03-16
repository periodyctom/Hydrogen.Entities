using System;
using System.Runtime.CompilerServices;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;

namespace Hydrogen.Entities.Tests
{
    [Serializable]
    public struct TimeConfigConverter : ISingletonConverter<TimeConfig>
    {
        public SingletonConverter<TimeConfig> Value;

        public TimeConfig Singleton
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Value.Singleton;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Value.Singleton = value;
        }

        public bool DontReplace
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Value.DontReplace;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Value.DontReplace = value;
        }
    }
    
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
    public struct LocalesConverter : ISingletonConverter<BlobRefData<Locales>>
    {
        public SingletonConverter<BlobRefData<Locales>> Value;

        public BlobRefData<Locales> Singleton
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Value.Singleton;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Value.Singleton = value;
        }

        public bool DontReplace
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Value.DontReplace;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Value.DontReplace = value;
        }
    }

    [Serializable]
    public struct Locales
    {
        public BlobString Name;
        public BlobArray<BlobString> Available;
    }
}
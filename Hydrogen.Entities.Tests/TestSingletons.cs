
using Unity.Entities;

namespace Hydrogen.Entities.Tests
{
    public struct TestTimeConfig : IComponentData
    {
        public uint AppTargetFrameRate;
        public float FixedDeltaTime;
    }

    public struct TestSupportedLocales
    {
        public BlobString Default;
        public BlobArray<NativeString64> Available;
    }
}
using Unity.Mathematics;
using UnityEngine;

namespace Hydrogen.Entities 
{
    [CreateAssetMenu(fileName = "FooBarBaz", menuName = "Samples/FooBarBaz Definition", order = 0)]
    public sealed class FooBarBazDefinition : ScriptableObject
    {
        public int4[] Foo = { new int4(0),  new int4(1), new int4(2), new int4(3), };
        public float4x4 Bar = float4x4.identity;
        public int Baz = 4096;
    }
}

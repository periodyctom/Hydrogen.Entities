using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Hydrogen.Entities
{
    public class FooBarBazAuthoring : SingletonConvertBlobCustomAuthoring<FooBarBaz, FooBarBazDefinition>
    {
        private static readonly ScriptToBlobFunc<FooBarBazDefinition, FooBarBaz> sm_convert = FooBarBazUtility.Convert;

        protected override ScriptToBlobFunc<FooBarBazDefinition, FooBarBaz> ScriptToBlob => sm_convert;
    }
}

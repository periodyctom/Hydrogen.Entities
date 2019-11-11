using System.Text;
using Hydrogen.Entities;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[assembly: RegisterGenericComponentType(typeof(BlobRefData<FooBarBaz>))]
[assembly: RegisterGenericComponentType(typeof(SingletonConverter<BlobRefData<FooBarBaz>>))]

namespace Hydrogen.Entities 
{
    public struct FooBarBaz
    {
        public BlobString Name;
        public BlobArray<int4> Foo;
        public BlobPtr<float4x4> Bar;
        public int Baz;
    }
    
    public static class FooBarBazUtility
    {
        public static BlobAssetReference<FooBarBaz> Convert(FooBarBazDefinition definition, ScriptableObjectConversionSystem
                                                                system)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref FooBarBaz root = ref builder.ConstructRoot<FooBarBaz>();
            
            if(!string.IsNullOrEmpty(definition.name))
                builder.AllocateString(ref root.Name, definition.name);
            else
                root.Name = new BlobString();

            if(definition.Foo != null && definition.Foo.Length > 0)
                builder.Construct(ref root.Foo, definition.Foo);
            else
                root.Foo = new BlobArray<int4>();

            ref float4x4 bar = ref builder.Allocate(ref root.Bar);
            bar = definition.Bar;

            root.Baz = definition.Baz;

            BlobAssetReference<FooBarBaz> reference = builder.CreateBlobAssetReference<FooBarBaz>(Allocator.Persistent);
            builder.Dispose();

            return reference;
        }
    }
    
    public sealed class FooBarBazConvertSystem : SingletonBlobConvertSystem<FooBarBaz> {}

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(FooBarBazConvertSystem))]
    public sealed class FooBarBazChangedSystem : SingletonBlobChangedComponentSystem<FooBarBaz>
    {
        private readonly StringBuilder m_stringBuilder = new StringBuilder(1024);
        protected override void OnUpdate()
        {
            ref FooBarBaz fooBarBaz = ref GetSingleton<BlobRefData<FooBarBaz>>().Resolve;

            m_stringBuilder.AppendLine(fooBarBaz.Name.ToString());

            for (int i = 0; i < fooBarBaz.Foo.Length; i++)
            {
                ref int4 fooElem = ref fooBarBaz.Foo[i];
                m_stringBuilder.AppendLine($"Foo {i:D}: {fooElem.ToString()}");
            }

            ref float4x4 bar = ref fooBarBaz.Bar.Value;
            m_stringBuilder.AppendLine($"Bar: {bar.ToString()}");

            m_stringBuilder.AppendLine($"Baz: {fooBarBaz.Baz:D}");
            
            Debug.Log(m_stringBuilder.ToString());
            m_stringBuilder.Clear();
        }
    }
}

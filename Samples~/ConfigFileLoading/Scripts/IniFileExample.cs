using System;
using System.IO;
using System.Text;
using Hydrogen.Entities;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Assertions;

[assembly: RegisterGenericComponentType(typeof(BlobRefData<IniFile>))]
[assembly: RegisterGenericComponentType(typeof(SingletonConverter<BlobRefData<IniFile>>))]

namespace Hydrogen.Entities
{
    public class IniFileExample : MonoBehaviour
    {
        private const string kFileName = "TestConfig.ini";
        private const string kIniContents = "foo=42\nbar=\"hello world\"\nbaz=12.34\nqux=Slow\n";

        [SerializeField] private string m_fileName = kFileName;
        [SerializeField, Multiline(8)] private string m_iniContents = kIniContents;
        
        private void Start()
        {
            using (var reader = new StringReader(m_iniContents))
            {
                var builder = new BlobBuilder(Allocator.Temp);
                BlobAssetReference<IniFile> configReference;

                try
                {
                    ref IniFile root = ref builder.ConstructRoot<IniFile>();
                
                    builder.AllocateString(ref root.Name, m_fileName);
            
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] pairStr = line.Split('=');
                        if(pairStr.Length != 2 || string.IsNullOrEmpty(pairStr[0])) continue;

                        switch (pairStr[0])
                        {
                            case "foo":
                                int.TryParse(pairStr[1], out root.Foo);
                                break;
                            case "bar":
                                if (root.Bar.Length != 0) continue;
                                builder.AllocateString(ref root.Bar, pairStr[1]);
                                break;
                            case "baz":
                                float.TryParse(pairStr[1], out root.Baz);
                                break;
                            case "qux":
                                Enum.TryParse(pairStr[1], true, out root.Qux);
                                break;
                        }
                    }

                    configReference = builder.CreateBlobAssetReference<IniFile>(Allocator.Persistent);
                }
                finally
                {
                    builder.Dispose();
                }
            
                Assert.IsTrue(configReference.IsCreated);

                BlobRefData<IniFile> blobRef = configReference;
                SingletonConverter<BlobRefData<IniFile>> converter = blobRef;

                EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                Entity entity = entityManager.CreateEntity(typeof(SingletonConverter<BlobRefData<IniFile>>));
                entityManager.SetComponentData(entity, converter);
            }
        }
    }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public sealed class IniFileConvertSystem : SingletonBlobConvertSystem<IniFile> { }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(IniFileConvertSystem))]
    public sealed class IniFileChangedSystem : SingletonBlobChangedComponentSystem<IniFile>
    {
        protected override void OnUpdate()
        {
            ref IniFile iniFile = ref GetSingleton<BlobRefData<IniFile>>().Resolve;
            
            var strBuilder = new StringBuilder(1024);
            strBuilder.AppendLine($"Ini File Name: {iniFile.Name.ToString()}");
            strBuilder.AppendLine($"Foo: {iniFile.Foo:D}");
            strBuilder.AppendLine($"Bar: {iniFile.Bar.ToString()}");
            strBuilder.AppendLine($"Baz: {iniFile.Baz:N}");
            strBuilder.AppendLine($"Qux Speed is: {iniFile.Qux.ToString()}");

            Debug.Log(strBuilder.ToString());
        }
    }

    public struct IniFile
    {
        public BlobString Name;
        public BlobString Bar;
        public int Foo;
        public float Baz;
        public Speed Qux;
    }

    public enum Speed
    {
        Slow,
        Medium,
        Fast
    }
}

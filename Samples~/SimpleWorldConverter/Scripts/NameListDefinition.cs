using System.Text;
using Hydrogen.Entities;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Assertions;

[assembly: RegisterGenericComponentType(typeof(BlobRefData<NameList>))]
[assembly: RegisterGenericComponentType(typeof(SingletonConverter<BlobRefData<NameList>>))]

namespace Hydrogen.Entities
{
    [CreateAssetMenu(fileName = "NameList", menuName = "Samples/NameList Definition", order = 0)]
    public class NameListDefinition : ScriptableObject, IConvertScriptableObjectToBlob<NameList>
    {
        [SerializeField] private string[] m_names = {"Blinky", "Pinky", "Inky", "Clyde"};

        public string[] Names => m_names;
        
        public BlobAssetReference<NameList> Convert(ScriptableObjectConversionSystem conversion)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref NameList root = ref builder.ConstructRoot<NameList>();
            
            if(!string.IsNullOrEmpty(name) && name.Length > 0)
                builder.AllocateString(ref root.Name, name);
            else
                root.Name = new BlobString();

            int len = m_names?.Length ?? 0;

            if (len > 0)
            {
                Assert.IsNotNull(m_names);
                BlobBuilderArray<BlobString> array = builder.Allocate(ref root.Names, len);

                for (int i = 0; i < len; i++)
                {
                    ref BlobString str = ref array[i];
                    builder.AllocateString(ref str, m_names[i]);
                }
            }
            else
                root.Names = new BlobArray<BlobString>();


            BlobAssetReference<NameList> nameListReference =
                builder.CreateBlobAssetReference<NameList>(Allocator.Persistent);
            
            builder.Dispose();
            
            return nameListReference;
        }
    }

    public struct NameList
    {
        public BlobString Name;
        public BlobArray<BlobString> Names;
    }

    public sealed class NameListConvertSystem : SingletonBlobConvertSystem<NameList> { }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(NameListConvertSystem))]
    public sealed class NameListChangedSystem : SingletonBlobChangedComponentSystem<NameList>
    {
        private readonly StringBuilder m_stringBuilder = new StringBuilder(1024);

        protected override void OnUpdate()
        {
            var nameListRefData = GetSingleton<BlobRefData<NameList>>();

            ref NameList nameList = ref nameListRefData.Resolve;

            m_stringBuilder.AppendLine(nameList.Name.ToString());
            int len = nameList.Names.Length;

            m_stringBuilder.AppendLine($"Total Names: {len:D}");
            
            for (int i = 0; i < len; i++)
            {
                ref BlobString str = ref nameList.Names[i];
                m_stringBuilder.AppendLine(str.ToString());
            }
            
            Debug.Log(m_stringBuilder.ToString());
            m_stringBuilder.Clear();
        }
    }
}

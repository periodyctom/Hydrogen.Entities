using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Hydrogen.Entities 
{
    public sealed class DatabaseDefinition : ScriptableObject, IConvertScriptableObjectToBlob<Database>
    {
        public AttackDefinition[] Attacks;

        public BlobAssetReference<Database> Convert(ScriptableObjectConversionSystem conversion)
        {
            BlobAssetReference<Database> reference;

            var builder = new BlobBuilder(Allocator.Temp);
            try
            {
                ref Database root = ref builder.ConstructRoot<Database>();

                builder.AllocateString(ref root.Name, name);
                root.NullAttack = default;
                builder.AllocateString(ref root.NullAttack.Name, "(Null)");

                int attacksLength = Attacks.Length;

                if (attacksLength > 0)
                {
                    BlobBuilderArray<AttackEntry> attackArray = builder.Allocate(ref root.Attacks, attacksLength);

                    for (int i = 0; i < attacksLength; i++)
                    {
                        AttackDefinition atkDef = Attacks[i];
                        ref AttackEntry entry = ref attackArray[i];
                        entry.NameHash = atkDef.Name.GetHashCode();
                        ref Attack atk = ref builder.Allocate(ref entry.ValuePtr);
                        builder.AllocateString(ref atk.Name, atkDef.Name);
                        atk.Power = atkDef.Power;
                        atk.Speed = atkDef.Speed;
                        atk.Debuffs = atkDef.Debuffs;
                    }
                }
                else
                    root.Attacks = new BlobArray<AttackEntry>();

                reference = builder.CreateBlobAssetReference<Database>(Allocator.Persistent);
            }
            finally
            {
                builder.Dispose();
            }
            
            return reference;
        }
    }
    
    [Serializable]
    public sealed class AttackDefinition
    {
        public string Name;
        public int Power;
        public float Speed;
        public Debuffs Debuffs;
    }
}

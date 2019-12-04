using System;
using System.Text;
using Hydrogen.Entities;
using Unity.Entities;
using UnityEngine;

[assembly: RegisterGenericComponentType(typeof(BlobRefData<Database>))]
[assembly: RegisterGenericComponentType(typeof(SingletonConverter<BlobRefData<Database>>))]

namespace Hydrogen.Entities
{
    public class DatabaseExample : MonoBehaviour
    {
        public TextAsset Database;

        private void Start()
        {
            if (Database == null || string.IsNullOrEmpty(Database.text))
                return;

            var definition = ScriptableObject.CreateInstance<DatabaseDefinition>();
            JsonUtility.FromJsonOverwrite(Database.text, definition);
            definition.name = "Test Database";

            var authorGo = new GameObject("Test Database", typeof(DatabaseAuthoring));
            var authoring = authorGo.GetComponent<DatabaseAuthoring>();
            authoring.Source = definition;

            authorGo.AddComponent<ConvertToEntity>();
        }
    }
    
    public sealed class DatabaseConvertSystem : SingletonBlobConvertSystem<Database> { }
    
    public sealed class DatabaseChangedSystem : SingletonBlobChangedComponentSystem<Database>
    {
        protected override void OnUpdate()
        {
            ref Database db = ref GetSingleton<BlobRefData<Database>>().Resolve;
            
            var strBuilder = new StringBuilder(1024);

            ref Attack burningHands = ref db.GetAttackByName("Burning Hands");
            Log(strBuilder, ref burningHands);
            
            ref Attack prismaticBeam = ref db.GetAttackByName("Prismatic Beam");
            Log(strBuilder, ref prismaticBeam);

            ref Attack poisonRainVornado = ref db.GetAttackByName("Poison Rain Vornado");
            Log(strBuilder, ref poisonRainVornado);
            
            ref Attack magicMissile = ref db.GetAttackByName("Magic Missile");
            Log(strBuilder, ref magicMissile);

            ref Attack missing = ref db.GetAttackByName("Missing");
            Log(strBuilder, ref missing);

            Debug.Log(strBuilder.ToString());
        }

        private static void Log(StringBuilder strBuilder, ref Attack atk)
        {
            strBuilder.AppendLine(atk.Name.ToString());
            strBuilder.AppendLine($"Power: {atk.Power:D}");
            strBuilder.AppendLine($"Speed: {atk.Speed:N2}");
            strBuilder.AppendLine($"Debuffs: {atk.Debuffs:F}");
            strBuilder.AppendLine();
        }
    }

    [Flags]
    public enum Debuffs : byte
    {
        None = 0x00,
        Burn = 0x01,
        Soak = 0x02,
        Shock = 0x04,
        Freeze = 0x08,
        Dizzy = 0x10,
        Poison = 0x20,
        Slow = 0x40,
        Wither = 0x80 
    }

    public struct Database
    {
        public BlobString Name;
        public BlobArray<AttackEntry> Attacks;
        public Attack NullAttack;
    }

    public struct AttackEntry
    {
        public int NameHash;
        public BlobPtr<Attack> ValuePtr;
    }

    public struct Attack
    {
        public BlobString Name;
        public int Power;
        public float Speed;
        public Debuffs Debuffs;
    }

    public static class DatabaseEx
    {
        public static ref Attack GetAttackByName(this ref Database db, string name)
        {
            int nameHash = !string.IsNullOrEmpty(name) ? name.GetHashCode() : 0;
            int length = db.Attacks.Length;

            for (int i = 0; i < length; i++)
            {
                ref AttackEntry entry = ref db.Attacks[i];

                if (entry.NameHash == nameHash)
                    return ref entry.ValuePtr.Value;
            }

            return ref db.NullAttack;
        }
    }

}

using UnityEditor;
using UnityEngine;

namespace Hydrogen.Entities.Tests
{
    internal static class TestUtilities
    {
        public const string kContentPath =
            "Packages/com.periodyc.hydrogen.entities/Hydrogen.Entities.Hybrid.Tests/Content/";
        
        public static GameObject LoadPrefab(string name)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{kContentPath}{name}.prefab");
        }
    }
}

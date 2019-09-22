using UnityEditor;
using UnityEngine;
// ReSharper disable CheckNamespace

namespace Hydrogen.Entities.Tests
{
    internal static class TestUtilities
    {
        private const string kContentPath =
            "Packages/com.periodyc.hydrogen.entities/Hydrogen.Entities.Hybrid.Tests/Content/";
        public static GameObject LoadPrefab(string name)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{kContentPath}{name}.prefab");
        }

        public static T LoadAsset<T>(string name)
            where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>($"{kContentPath}{name}.asset");
        }
    }
}

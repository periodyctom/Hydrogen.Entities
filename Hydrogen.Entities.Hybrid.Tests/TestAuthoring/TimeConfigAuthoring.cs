using UnityEngine;


namespace Hydrogen.Entities.Tests
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Hidden/DontUse")]
    public class TimeConfigAuthoring : SingletonConvertDataAuthoring<TimeConfig> { }
}
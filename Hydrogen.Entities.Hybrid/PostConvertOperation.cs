using Unity.Entities;
using UnityEngine;

namespace Hydrogen.Entities
{
    public abstract class PostConvertOperation : ScriptableObject
    {
        public abstract void Perform(EntityManager manager);
    }
}

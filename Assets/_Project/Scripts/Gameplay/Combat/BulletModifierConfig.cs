using UnityEngine;

namespace _Project.Scripts.Gameplay.Combat
{
    /// <summary>
    /// Inspector-friendly bullet modifier definition that creates a fresh runtime modifier per spawned bullet.
    /// </summary>
    public abstract class BulletModifierConfig : ScriptableObject
    {
        public abstract IBulletModifier CreateModifier(BulletSpawner ownerSpawner);
    }
}

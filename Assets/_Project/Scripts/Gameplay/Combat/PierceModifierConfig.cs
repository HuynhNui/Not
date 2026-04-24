using UnityEngine;

namespace _Project.Scripts.Gameplay.Combat
{
    /// <summary>
    /// Creates runtime pierce modifiers for bullets.
    /// </summary>
    [CreateAssetMenu(fileName = "PierceModifierConfig", menuName = "Chibi Pixel Gate/Combat/Bullet Modifiers/Pierce")]
    public sealed class PierceModifierConfig : BulletModifierConfig
    {
        [SerializeField] private int pierceCount = 1;

        public override IBulletModifier CreateModifier(BulletSpawner ownerSpawner)
        {
            return new PierceModifier(Mathf.Max(0, pierceCount));
        }
    }
}

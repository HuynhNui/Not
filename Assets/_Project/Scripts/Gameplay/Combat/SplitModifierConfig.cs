using UnityEngine;

namespace _Project.Scripts.Gameplay.Combat
{
    /// <summary>
    /// Creates runtime split modifiers for bullets.
    /// </summary>
    [CreateAssetMenu(fileName = "SplitModifierConfig", menuName = "Chibi Pixel Gate/Combat/Bullet Modifiers/Split")]
    public sealed class SplitModifierConfig : BulletModifierConfig
    {
        [SerializeField] private int splitCount = 2;
        [SerializeField] private float spreadAngle = 30f;
        [SerializeField] private float damageMultiplier = 0.5f;

        public override IBulletModifier CreateModifier(BulletSpawner ownerSpawner)
        {
            return new SplitModifier(
                this,
                Mathf.Max(0, splitCount),
                Mathf.Max(0f, spreadAngle),
                Mathf.Max(0f, damageMultiplier));
        }
    }
}

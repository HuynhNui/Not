using UnityEngine;

namespace _Project.Scripts.Gameplay.Combat
{
    /// <summary>
    /// Creates runtime homing modifiers for bullets.
    /// </summary>
    [CreateAssetMenu(fileName = "HomingModifierConfig", menuName = "Chibi Pixel Gate/Combat/Bullet Modifiers/Homing")]
    public sealed class HomingModifierConfig : BulletModifierConfig
    {
        [SerializeField] private float searchRadius = 5f;
        [SerializeField] private float turnSpeed = 360f;
        [SerializeField] private LayerMask targetLayers = ~0;

        public override IBulletModifier CreateModifier(BulletSpawner ownerSpawner)
        {
            return new HomingModifier(
                Mathf.Max(0f, searchRadius),
                Mathf.Max(0f, turnSpeed),
                targetLayers);
        }
    }
}

using UnityEngine;

namespace _Project.Scripts.Data.ScriptableObjects.UnitData
{
    /// <summary>
    /// Stores reusable unit configuration values shared by player or enemy archetypes.
    /// </summary>
    [CreateAssetMenu(fileName = "UnitData", menuName = "Chibi Pixel Gate/Data/Unit Data")]
    public sealed class UnitData : ScriptableObject
    {
        [SerializeField] private float maxHealth = 1f;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float contactDamage = 1f;

        public float MaxHealth => maxHealth;
        public float MoveSpeed => moveSpeed;
        public float ContactDamage => contactDamage;
    }
}

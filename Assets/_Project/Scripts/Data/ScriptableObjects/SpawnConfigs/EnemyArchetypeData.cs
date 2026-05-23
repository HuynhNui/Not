using UnityEngine;

namespace _Project.Scripts.Data.ScriptableObjects.SpawnConfigs
{
    /// <summary>
    /// Defines the runtime stats and presentation for an enemy archetype.
    /// One prefab can be reused by many archetypes to keep pooling cheap.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyArchetype", menuName = "Chibi Pixel Gate/Data/Enemy Archetype")]
    public sealed class EnemyArchetypeData : ScriptableObject
    {
        [SerializeField] private string archetypeId = "fodder";
        [SerializeField] private Color bodyColor = Color.white;
        [SerializeField] private Vector2 visualScale = Vector2.one;
        [SerializeField] private float maxHealth = 3f;
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float contactDamage = 1f;
        [SerializeField] private int scoreValue = 1;
        [SerializeField] private int coinReward = 1;
        [SerializeField] private bool destroyOnPlayerHit = true;

        public string ArchetypeId => archetypeId;
        public Color BodyColor => bodyColor;
        public Vector2 VisualScale => visualScale;
        public float MaxHealth => Mathf.Max(0.01f, maxHealth);
        public float MoveSpeed => Mathf.Max(0f, moveSpeed);
        public float ContactDamage => Mathf.Max(0f, contactDamage);
        public int ScoreValue => Mathf.Max(0, scoreValue);
        public int CoinReward => Mathf.Max(0, coinReward);
        public bool DestroyOnPlayerHit => destroyOnPlayerHit;
    }
}

using UnityEngine;

namespace _Project.Scripts.Data.ScriptableObjects.SpawnConfigs
{
    /// <summary>
    /// Stores enemy spawn pacing and fallback spawn bounds.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemySpawnConfig", menuName = "Chibi Pixel Gate/Data/Enemy Spawn Config")]
    public sealed class EnemySpawnConfig : ScriptableObject
    {
        [SerializeField] private AnimationCurve difficultyCurve = AnimationCurve.Linear(0f, 1f, 60f, 2f);
        [SerializeField] private float baseSpawnInterval = 1.5f;
        [SerializeField] private float minimumSpawnInterval = 0.35f;
        [SerializeField] private float spawnYOffset = 1f;
        [SerializeField] private float horizontalSpawnPadding = 0.35f;

        public AnimationCurve DifficultyCurve => difficultyCurve;
        public float BaseSpawnInterval => baseSpawnInterval;
        public float MinimumSpawnInterval => minimumSpawnInterval;
        public float SpawnYOffset => spawnYOffset;
        public float HorizontalSpawnPadding => horizontalSpawnPadding;
    }
}

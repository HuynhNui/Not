using _Project.Scripts.Gameplay.Enemies;
using UnityEngine;

namespace _Project.Scripts.Systems.EnemySpawnerSystem
{
    /// <summary>
    /// Controls enemy wave pacing, spawn positions, and difficulty scaling over time.
    /// </summary>
    public sealed class EnemySpawnerSystem : MonoBehaviour
    {
        [SerializeField] private EnemyController enemyPrefab;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private AnimationCurve difficultyCurve = AnimationCurve.Linear(0f, 1f, 60f, 2f);

        public void Init()
        {
        }

        private void Update()
        {
        }

        public void Spawn()
        {
        }
    }
}

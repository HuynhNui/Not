using _Project.Scripts.Gameplay.Enemies;
using UnityEngine;
using RuntimeEnemySpawnerSystem = _Project.Scripts.Systems.EnemySpawnerSystem.EnemySpawnerSystem;

namespace _Project.Scripts.Systems.RunStatsSystem
{
    /// <summary>
    /// Tracks run-scoped survival metrics and lightweight persistent best values.
    /// </summary>
    public sealed class RunStatsTracker : MonoBehaviour
    {
        private const string BestSurvivalTimeKey = "RunStats.BestSurvivalTime";
        private const string BestKillCountKey = "RunStats.BestKillCount";
        private const string BestCoinsEarnedKey = "RunStats.BestCoinsEarned";
        private const string WalletCoinsKey = "RunStats.WalletCoins";

        private RuntimeEnemySpawnerSystem _enemySpawnerSystem;
        private bool _isTracking;
        private float _survivalTime;
        private int _enemyKills;
        private int _coinsEarned;

        public float SurvivalTime => _survivalTime;
        public int EnemyKills => _enemyKills;
        public int CoinsEarned => _coinsEarned;
        public int WalletCoins => PlayerPrefs.GetInt(WalletCoinsKey, 0);
        public float BestSurvivalTime => PlayerPrefs.GetFloat(BestSurvivalTimeKey, 0f);
        public int BestKillCount => PlayerPrefs.GetInt(BestKillCountKey, 0);
        public int BestCoinsEarned => PlayerPrefs.GetInt(BestCoinsEarnedKey, 0);

        public void Init(RuntimeEnemySpawnerSystem enemySpawnerSystem)
        {
            if (_enemySpawnerSystem != null)
            {
                _enemySpawnerSystem.EnemyKilled -= HandleEnemyKilled;
            }

            _enemySpawnerSystem = enemySpawnerSystem;

            if (_enemySpawnerSystem != null)
            {
                _enemySpawnerSystem.EnemyKilled -= HandleEnemyKilled;
                _enemySpawnerSystem.EnemyKilled += HandleEnemyKilled;
            }
        }

        private void OnDestroy()
        {
            if (_enemySpawnerSystem != null)
            {
                _enemySpawnerSystem.EnemyKilled -= HandleEnemyKilled;
            }
        }

        private void Update()
        {
            if (!_isTracking)
            {
                return;
            }

            _survivalTime += Time.deltaTime;
        }

        public void BeginRun()
        {
            _survivalTime = 0f;
            _enemyKills = 0;
            _coinsEarned = 0;
            _isTracking = true;
        }

        public void EndRun()
        {
            if (!_isTracking)
            {
                return;
            }

            _isTracking = false;
            SaveBestStats();
        }

        public RunStatsSnapshot CreateSnapshot()
        {
            return new RunStatsSnapshot(
                _survivalTime,
                _enemyKills,
                _coinsEarned,
                WalletCoins,
                BestSurvivalTime,
                BestKillCount,
                BestCoinsEarned);
        }

        private void HandleEnemyKilled(EnemyController enemy)
        {
            if (!_isTracking || enemy == null)
            {
                return;
            }

            _enemyKills++;
            _coinsEarned += enemy.CoinReward;
        }

        private void SaveBestStats()
        {
            if (_survivalTime > BestSurvivalTime)
            {
                PlayerPrefs.SetFloat(BestSurvivalTimeKey, _survivalTime);
            }

            if (_enemyKills > BestKillCount)
            {
                PlayerPrefs.SetInt(BestKillCountKey, _enemyKills);
            }

            if (_coinsEarned > BestCoinsEarned)
            {
                PlayerPrefs.SetInt(BestCoinsEarnedKey, _coinsEarned);
            }

            PlayerPrefs.SetInt(WalletCoinsKey, WalletCoins + _coinsEarned);
            PlayerPrefs.Save();
        }
    }

    public readonly struct RunStatsSnapshot
    {
        public readonly float SurvivalTime;
        public readonly int EnemyKills;
        public readonly int CoinsEarned;
        public readonly int WalletCoins;
        public readonly float BestSurvivalTime;
        public readonly int BestKillCount;
        public readonly int BestCoinsEarned;

        public RunStatsSnapshot(
            float survivalTime,
            int enemyKills,
            int coinsEarned,
            int walletCoins,
            float bestSurvivalTime,
            int bestKillCount,
            int bestCoinsEarned)
        {
            SurvivalTime = survivalTime;
            EnemyKills = enemyKills;
            CoinsEarned = coinsEarned;
            WalletCoins = walletCoins;
            BestSurvivalTime = bestSurvivalTime;
            BestKillCount = bestKillCount;
            BestCoinsEarned = bestCoinsEarned;
        }
    }
}

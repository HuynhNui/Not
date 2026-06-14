using _Project.Scripts.Gameplay.Enemies;
using _Project.Scripts.Systems.SaveSystem;
using UnityEngine;
using RuntimeEnemySpawnerSystem = _Project.Scripts.Systems.EnemySpawnerSystem.EnemySpawnerSystem;

namespace _Project.Scripts.Systems.RunStatsSystem
{
    /// <summary>
    /// Tracks run-scoped survival metrics and persists progression through SaveService.
    /// </summary>
    public sealed class RunStatsTracker : MonoBehaviour
    {
        public const string BestSurvivalTimePrefsKey = "RunStats.BestSurvivalTime";
        public const string BestKillCountPrefsKey = "RunStats.BestKillCount";
        public const string BestCoinsEarnedPrefsKey = "RunStats.BestCoinsEarned";
        public const string BestScorePrefsKey = "RunStats.BestScore";
        public const string WalletCoinsPrefsKey = "RunStats.WalletCoins";

        private RuntimeEnemySpawnerSystem _enemySpawnerSystem;
        private bool _isTracking;
        private float _survivalTime;
        private int _enemyKills;
        private int _coinsEarned;
        private int _score;

        public float SurvivalTime => _survivalTime;
        public int EnemyKills => _enemyKills;
        public int CoinsEarned => _coinsEarned;
        public int Score => _score;
        public int WalletCoins => SaveService.Instance.Data.walletCoins;
        public float BestSurvivalTime => SaveService.Instance.Data.bestSurvivalTime;
        public int BestKillCount => SaveService.Instance.Data.bestKillCount;
        public int BestCoinsEarned => SaveService.Instance.Data.bestCoinsEarned;
        public int BestScore => SaveService.Instance.Data.bestScore;

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
            _score = 0;
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
                _score,
                WalletCoins,
                BestSurvivalTime,
                BestKillCount,
                BestCoinsEarned,
                BestScore);
        }

        public bool TrySpendWalletCoins(int amount)
        {
            return SaveService.Instance.TrySpendWalletCoins(amount);
        }

        private void HandleEnemyKilled(EnemyController enemy)
        {
            if (!_isTracking || enemy == null)
            {
                return;
            }

            _enemyKills++;
            _coinsEarned += enemy.CoinReward;
            _score += Mathf.Max(0, enemy.ScoreValue);
        }

        private void SaveBestStats()
        {
            SaveService.Instance.RecordRunResult(_survivalTime, _enemyKills, _coinsEarned, _score);
        }
    }

    public readonly struct RunStatsSnapshot
    {
        public readonly float SurvivalTime;
        public readonly int EnemyKills;
        public readonly int CoinsEarned;
        public readonly int Score;
        public readonly int WalletCoins;
        public readonly float BestSurvivalTime;
        public readonly int BestKillCount;
        public readonly int BestCoinsEarned;
        public readonly int BestScore;

        public RunStatsSnapshot(
            float survivalTime,
            int enemyKills,
            int coinsEarned,
            int score,
            int walletCoins,
            float bestSurvivalTime,
            int bestKillCount,
            int bestCoinsEarned,
            int bestScore)
        {
            SurvivalTime = survivalTime;
            EnemyKills = enemyKills;
            CoinsEarned = coinsEarned;
            Score = score;
            WalletCoins = walletCoins;
            BestSurvivalTime = bestSurvivalTime;
            BestKillCount = bestKillCount;
            BestCoinsEarned = bestCoinsEarned;
            BestScore = bestScore;
        }
    }
}

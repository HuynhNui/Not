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
        public int WalletCoins => PlayerPrefs.GetInt(WalletCoinsPrefsKey, 0);
        public float BestSurvivalTime => PlayerPrefs.GetFloat(BestSurvivalTimePrefsKey, 0f);
        public int BestKillCount => PlayerPrefs.GetInt(BestKillCountPrefsKey, 0);
        public int BestCoinsEarned => PlayerPrefs.GetInt(BestCoinsEarnedPrefsKey, 0);
        public int BestScore => PlayerPrefs.GetInt(BestScorePrefsKey, 0);

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
            int safeAmount = Mathf.Max(0, amount);
            int walletCoins = WalletCoins;

            if (walletCoins < safeAmount)
            {
                return false;
            }

            PlayerPrefs.SetInt(WalletCoinsPrefsKey, walletCoins - safeAmount);
            PlayerPrefs.Save();
            return true;
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
            if (_survivalTime > BestSurvivalTime)
            {
                PlayerPrefs.SetFloat(BestSurvivalTimePrefsKey, _survivalTime);
            }

            if (_enemyKills > BestKillCount)
            {
                PlayerPrefs.SetInt(BestKillCountPrefsKey, _enemyKills);
            }

            if (_coinsEarned > BestCoinsEarned)
            {
                PlayerPrefs.SetInt(BestCoinsEarnedPrefsKey, _coinsEarned);
            }

            if (_score > BestScore)
            {
                PlayerPrefs.SetInt(BestScorePrefsKey, _score);
            }

            PlayerPrefs.SetInt(WalletCoinsPrefsKey, WalletCoins + _coinsEarned);
            PlayerPrefs.Save();
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

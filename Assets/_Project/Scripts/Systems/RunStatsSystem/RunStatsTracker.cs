using _Project.Scripts.Gameplay.Enemies;
using _Project.Scripts.Data.Balance;
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
        private float _coinRewardPoints;
        private int _killScore;
        private int _eliteBonusScore;
        private float _coinRewardMultiplier = 1f;
        [SerializeField] private EconomyConfig economyConfig;

        public float SurvivalTime => _survivalTime;
        public int EnemyKills => _enemyKills;
        public int CoinsEarned => _isTracking
            ? CalculateFinalCoins()
            : _coinsEarned;
        public float CoinRewardPoints => _coinRewardPoints;
        public int KillScore => _killScore;
        public int TimeScore => CalculateTimeScore();
        public int Score => _killScore + CalculateTimeScore() + _eliteBonusScore;
        public float CoinRewardMultiplier => _coinRewardMultiplier;
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

        public void SetEconomyConfig(EconomyConfig config)
        {
            economyConfig = config;
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
            _coinRewardPoints = 0f;
            _killScore = 0;
            _eliteBonusScore = 0;
            _coinRewardMultiplier = 1f;
            _isTracking = true;
        }

        public void EndRun()
        {
            if (!_isTracking)
            {
                return;
            }

            _isTracking = false;
            _coinsEarned = CalculateFinalCoins();
            SaveBestStats();
        }

        public RunStatsSnapshot CreateSnapshot()
        {
            return new RunStatsSnapshot(
                _survivalTime,
                _enemyKills,
                CoinsEarned,
                Score,
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

        public void SetCoinRewardMultiplier(float multiplier)
        {
            _coinRewardMultiplier = Mathf.Max(0f, multiplier);
        }

        private void HandleEnemyKilled(EnemyController enemy)
        {
            if (!_isTracking || enemy == null)
            {
                return;
            }

            _enemyKills++;
            _coinRewardPoints += Mathf.Max(
                0f,
                enemy.RewardPoints * _coinRewardMultiplier);
            _killScore += Mathf.Max(0, enemy.ScoreValue);
        }

        private void SaveBestStats()
        {
            SaveService.Instance.RecordRunResult(
                _survivalTime,
                _enemyKills,
                _coinsEarned,
                Score);
        }

        private int CalculateFinalCoins()
        {
            return economyConfig != null
                ? economyConfig.CalculateFinalCoins(_coinRewardPoints, _survivalTime)
                : EconomyConfig.CalculateDefaultFinalCoins(_coinRewardPoints, _survivalTime);
        }

        private int CalculateTimeScore()
        {
            return economyConfig != null
                ? economyConfig.CalculateTimeScore(_survivalTime)
                : EconomyConfig.CalculateDefaultTimeScore(_survivalTime);
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

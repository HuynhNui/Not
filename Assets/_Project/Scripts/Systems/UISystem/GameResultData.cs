using System;

namespace _Project.Scripts.Systems.UISystem
{
    [Serializable]
    public readonly struct GameResultData
    {
        public readonly float SurvivalTime;
        public readonly int Score;
        public readonly int CoinsEarned;
        public readonly int Kills;
        public readonly int RewardCoins;
        public readonly int BestScore;
        public readonly float BestSurvivalTime;
        public readonly int BestKills;

        public GameResultData(
            float survivalTime,
            int score,
            int coinsEarned,
            int kills,
            int rewardCoins,
            int bestScore,
            float bestSurvivalTime,
            int bestKills)
        {
            SurvivalTime = survivalTime;
            Score = score;
            CoinsEarned = coinsEarned;
            Kills = kills;
            RewardCoins = rewardCoins;
            BestScore = bestScore;
            BestSurvivalTime = bestSurvivalTime;
            BestKills = bestKills;
        }
    }
}

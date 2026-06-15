using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Data.Balance
{
    [CreateAssetMenu(
        fileName = "EconomyConfig_v1",
        menuName = "Chibi Pixel Gate/Balance/Economy Config")]
    public sealed class EconomyConfig : ScriptableObject
    {
        public const float DefaultRewardScale = 1f;
        public const float DefaultTimeCoinPer30Seconds = 0f;
        public const float DefaultTimeScorePerSecond = 0.5f;

        [SerializeField] private string configVersion = CombatScalingConfig.DefaultConfigVersion;
        [SerializeField] private float rewardScale = DefaultRewardScale;
        [SerializeField] private float timeCoinPer30Seconds;
        [SerializeField] private float timeScorePerSecond = DefaultTimeScorePerSecond;
        [SerializeField] private float eliteCoinBonusMin = 12f;
        [SerializeField] private float eliteCoinBonusMax = 18f;
        [SerializeField] private int[] storyMilestones = { 1, 3, 5, 8, 12, 17, 23, 30, 38, 47, 57 };

        public string ConfigVersion => configVersion;
        public float RewardScale => rewardScale;
        public float TimeCoinPer30Seconds => timeCoinPer30Seconds;
        public float TimeScorePerSecond => timeScorePerSecond;
        public float EliteCoinBonusMin => eliteCoinBonusMin;
        public float EliteCoinBonusMax => eliteCoinBonusMax;
        public IReadOnlyList<int> StoryMilestones => storyMilestones;

        public int CalculateFinalCoins(
            float rewardPoints,
            float survivalSeconds,
            float milestoneBonus = 0f)
        {
            return CalculateFinalCoins(
                rewardPoints,
                survivalSeconds,
                milestoneBonus,
                rewardScale,
                timeCoinPer30Seconds);
        }

        public int CalculateTimeScore(float survivalSeconds)
        {
            return CalculateTimeScore(survivalSeconds, timeScorePerSecond);
        }

        public static int CalculateDefaultFinalCoins(
            float rewardPoints,
            float survivalSeconds,
            float milestoneBonus = 0f)
        {
            return CalculateFinalCoins(
                rewardPoints,
                survivalSeconds,
                milestoneBonus,
                DefaultRewardScale,
                DefaultTimeCoinPer30Seconds);
        }

        public static int CalculateDefaultTimeScore(float survivalSeconds)
        {
            return CalculateTimeScore(survivalSeconds, DefaultTimeScorePerSecond);
        }

        private static int CalculateFinalCoins(
            float rewardPoints,
            float survivalSeconds,
            float milestoneBonus,
            float rewardScale,
            float timeCoinPer30Seconds)
        {
            float rewardTotal = Mathf.Max(0f, rewardPoints) * Mathf.Max(0f, rewardScale);
            float completedThirtySecondBlocks = Mathf.Floor(Mathf.Max(0f, survivalSeconds) / 30f);
            float timeBonus = completedThirtySecondBlocks * Mathf.Max(0f, timeCoinPer30Seconds);
            return Mathf.Max(
                0,
                Mathf.RoundToInt(rewardTotal + timeBonus + Mathf.Max(0f, milestoneBonus)));
        }

        private static int CalculateTimeScore(float survivalSeconds, float scorePerSecond)
        {
            return Mathf.Max(
                0,
                Mathf.FloorToInt(
                    Mathf.Max(0f, survivalSeconds) * Mathf.Max(0f, scorePerSecond)));
        }

        public void ValidateValues()
        {
            if (string.IsNullOrWhiteSpace(configVersion))
            {
                configVersion = CombatScalingConfig.DefaultConfigVersion;
            }

            rewardScale = Mathf.Max(0f, rewardScale);
            timeCoinPer30Seconds = Mathf.Max(0f, timeCoinPer30Seconds);
            timeScorePerSecond = Mathf.Max(0f, timeScorePerSecond);
            eliteCoinBonusMin = Mathf.Max(0f, eliteCoinBonusMin);
            eliteCoinBonusMax = Mathf.Max(eliteCoinBonusMin, eliteCoinBonusMax);
            storyMilestones ??= Array.Empty<int>();
            Array.Sort(storyMilestones);

            int writeIndex = 0;
            for (int readIndex = 0; readIndex < storyMilestones.Length; readIndex++)
            {
                int milestone = Mathf.Max(1, storyMilestones[readIndex]);
                if (writeIndex > 0 && storyMilestones[writeIndex - 1] == milestone)
                {
                    continue;
                }

                storyMilestones[writeIndex] = milestone;
                writeIndex++;
            }

            if (writeIndex != storyMilestones.Length)
            {
                Array.Resize(ref storyMilestones, writeIndex);
            }
        }

        private void OnValidate()
        {
            ValidateValues();
        }
    }
}

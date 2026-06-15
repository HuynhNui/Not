using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Data.Balance
{
    [CreateAssetMenu(
        fileName = "GatePoolConfig_v1",
        menuName = "Chibi Pixel Gate/Balance/Gate Pool Config")]
    public sealed class GatePoolConfig : ScriptableObject
    {
        public const float DefaultGateCadenceSeconds = 15f;
        public const float DefaultMajorGateCadenceSeconds = 60f;

        [SerializeField] private string configVersion = CombatScalingConfig.DefaultConfigVersion;
        [SerializeField] private float gateCadenceSeconds = DefaultGateCadenceSeconds;
        [SerializeField] private float majorGateCadenceSeconds = DefaultMajorGateCadenceSeconds;
        [SerializeField] private List<BalanceGateEntry> entries = new List<BalanceGateEntry>();

        public string ConfigVersion => configVersion;
        public float GateCadenceSeconds => gateCadenceSeconds;
        public float MajorGateCadenceSeconds => majorGateCadenceSeconds;
        public IReadOnlyList<BalanceGateEntry> Entries => entries;

        public static IReadOnlyList<BalanceGateEntry> CreateDefaultEntries()
        {
            return new List<BalanceGateEntry>
            {
                new BalanceGateEntry(
                    "stable_damage",
                    "DAMAGE +10%",
                    BalanceGateCategory.Stable,
                    1f,
                    0f,
                    BalanceEffectType.DamageMultiplier,
                    1.1f),
                new BalanceGateEntry(
                    "stable_fire_rate",
                    "FIRE RATE +0.2",
                    BalanceGateCategory.Stable,
                    1f,
                    0f,
                    BalanceEffectType.FireRateFlat,
                    0.2f),
                new BalanceGateEntry(
                    "stable_vitality",
                    "MAX HP +8%",
                    BalanceGateCategory.Stable,
                    1f,
                    0f,
                    BalanceEffectType.MaxHpMultiplier,
                    1.08f),
                new BalanceGateEntry(
                    "utility_repair",
                    "REPAIR 20%",
                    BalanceGateCategory.Utility,
                    1f,
                    0f,
                    BalanceEffectType.HealMissingHpRatio,
                    0.2f),
                new BalanceGateEntry(
                    "utility_barrier",
                    "BARRIER 1 HIT",
                    BalanceGateCategory.Utility,
                    1f,
                    0f,
                    BalanceEffectType.BarrierHits,
                    1f,
                    15f),
                new BalanceGateEntry(
                    "utility_freeze",
                    "FREEZE 20S",
                    BalanceGateCategory.Utility,
                    1f,
                    0f,
                    BalanceEffectType.EnemySpeedMultiplier,
                    0.75f,
                    20f),
                new BalanceGateEntry(
                    "risky_glass_cannon",
                    "GLASS CANNON",
                    BalanceGateCategory.Risky,
                    1f,
                    0f,
                    BalanceEffectType.DamageMultiplier,
                    1.25f,
                    0f,
                    BalanceEffectType.None,
                    0f,
                    0f,
                    BalanceEffectType.IncomingDamageMultiplier,
                    1.2f),
                new BalanceGateEntry(
                    "risky_bullet_storm",
                    "BULLET STORM",
                    BalanceGateCategory.Risky,
                    1f,
                    0f,
                    BalanceEffectType.ProjectileFlat,
                    1f,
                    0f,
                    BalanceEffectType.None,
                    0f,
                    0f,
                    BalanceEffectType.DamageMultiplier,
                    0.88f),
                new BalanceGateEntry(
                    "risky_reinforcement",
                    "REINFORCEMENT",
                    BalanceGateCategory.Risky,
                    1f,
                    0f,
                    BalanceEffectType.SquadFlat,
                    1f,
                    0f,
                    BalanceEffectType.None,
                    0f,
                    0f,
                    BalanceEffectType.EnemyPressureMultiplier,
                    1.15f),
                new BalanceGateEntry(
                    "risky_bounty",
                    "BOUNTY 30S",
                    BalanceGateCategory.Risky,
                    1f,
                    0f,
                    BalanceEffectType.CoinRewardMultiplier,
                    1.5f,
                    30f,
                    BalanceEffectType.EnemySpeedMultiplier,
                    1.15f,
                    30f),
                new BalanceGateEntry(
                    "major_projectile",
                    "PROJECTILE +1",
                    BalanceGateCategory.Major,
                    1f,
                    DefaultMajorGateCadenceSeconds,
                    BalanceEffectType.ProjectileFlat,
                    1f),
                new BalanceGateEntry(
                    "major_recruit",
                    "RECRUIT +1",
                    BalanceGateCategory.Major,
                    1f,
                    DefaultMajorGateCadenceSeconds,
                    BalanceEffectType.SquadFlat,
                    1f),
                new BalanceGateEntry(
                    "major_overclock",
                    "OVERCLOCK",
                    BalanceGateCategory.Major,
                    1f,
                    DefaultMajorGateCadenceSeconds,
                    BalanceEffectType.DamageMultiplier,
                    1.15f,
                    0f,
                    BalanceEffectType.FireRateMultiplier,
                    1.08f)
            };
        }

        public void ValidateValues()
        {
            if (string.IsNullOrWhiteSpace(configVersion))
            {
                configVersion = CombatScalingConfig.DefaultConfigVersion;
            }

            gateCadenceSeconds = Mathf.Max(1f, gateCadenceSeconds);
            majorGateCadenceSeconds = Mathf.Max(gateCadenceSeconds, majorGateCadenceSeconds);
            entries ??= new List<BalanceGateEntry>();

            for (int index = entries.Count - 1; index >= 0; index--)
            {
                if (entries[index] == null)
                {
                    entries.RemoveAt(index);
                    continue;
                }

                entries[index].Validate();
            }
        }

        private void OnValidate()
        {
            ValidateValues();
        }
    }

    public enum BalanceGateCategory
    {
        Stable = 0,
        Utility = 1,
        Risky = 2,
        Major = 3
    }

    public enum BalanceEffectType
    {
        None = 0,
        DamageMultiplier = 1,
        FireRateFlat = 2,
        FireRateMultiplier = 3,
        MaxHpMultiplier = 4,
        HealMissingHpRatio = 5,
        BarrierHits = 6,
        EnemySpeedMultiplier = 7,
        ProjectileFlat = 8,
        SquadFlat = 9,
        IncomingDamageMultiplier = 10,
        EnemyPressureMultiplier = 11,
        CoinRewardMultiplier = 12
    }

    [Serializable]
    public sealed class BalanceGateEntry
    {
        [SerializeField] private string gateId;
        [SerializeField] private string displayLabel;
        [SerializeField] private BalanceGateCategory category;
        [SerializeField] private float weight = 1f;
        [SerializeField] private float minTimeSeconds;
        [SerializeField] private BalanceEffectType effectType;
        [SerializeField] private float magnitude;
        [SerializeField] private float durationSeconds;
        [SerializeField] private BalanceEffectType secondaryEffectType;
        [SerializeField] private float secondaryMagnitude;
        [SerializeField] private float secondaryDurationSeconds;
        [SerializeField] private BalanceEffectType drawbackType;
        [SerializeField] private float drawbackMagnitude;
        [SerializeField] private float drawbackDurationSeconds;

        public string GateId => gateId;
        public string DisplayLabel => displayLabel;
        public BalanceGateCategory Category => category;
        public float Weight => weight;
        public float MinTimeSeconds => minTimeSeconds;
        public BalanceEffectType EffectType => effectType;
        public float Magnitude => magnitude;
        public float DurationSeconds => durationSeconds;
        public BalanceEffectType SecondaryEffectType => secondaryEffectType;
        public float SecondaryMagnitude => secondaryMagnitude;
        public float SecondaryDurationSeconds => secondaryDurationSeconds;
        public BalanceEffectType DrawbackType => drawbackType;
        public float DrawbackMagnitude => drawbackMagnitude;
        public float DrawbackDurationSeconds => drawbackDurationSeconds;

        public BalanceGateEntry(
            string gateId,
            string displayLabel,
            BalanceGateCategory category,
            float weight,
            float minTimeSeconds,
            BalanceEffectType effectType,
            float magnitude,
            float durationSeconds = 0f,
            BalanceEffectType secondaryEffectType = BalanceEffectType.None,
            float secondaryMagnitude = 0f,
            float secondaryDurationSeconds = 0f,
            BalanceEffectType drawbackType = BalanceEffectType.None,
            float drawbackMagnitude = 0f,
            float drawbackDurationSeconds = 0f)
        {
            this.gateId = gateId;
            this.displayLabel = displayLabel;
            this.category = category;
            this.weight = weight;
            this.minTimeSeconds = minTimeSeconds;
            this.effectType = effectType;
            this.magnitude = magnitude;
            this.durationSeconds = durationSeconds;
            this.secondaryEffectType = secondaryEffectType;
            this.secondaryMagnitude = secondaryMagnitude;
            this.secondaryDurationSeconds = secondaryDurationSeconds;
            this.drawbackType = drawbackType;
            this.drawbackMagnitude = drawbackMagnitude;
            this.drawbackDurationSeconds = drawbackDurationSeconds;
            Validate();
        }

        public void Validate()
        {
            gateId = string.IsNullOrWhiteSpace(gateId) ? "unnamed_gate" : gateId.Trim();
            displayLabel = string.IsNullOrWhiteSpace(displayLabel) ? gateId : displayLabel.Trim();
            weight = Mathf.Max(0f, weight);
            minTimeSeconds = Mathf.Max(0f, minTimeSeconds);
            magnitude = Mathf.Max(0f, magnitude);
            durationSeconds = Mathf.Max(0f, durationSeconds);
            secondaryMagnitude = Mathf.Max(0f, secondaryMagnitude);
            secondaryDurationSeconds = Mathf.Max(0f, secondaryDurationSeconds);
            drawbackMagnitude = Mathf.Max(0f, drawbackMagnitude);
            drawbackDurationSeconds = Mathf.Max(0f, drawbackDurationSeconds);
        }
    }
}

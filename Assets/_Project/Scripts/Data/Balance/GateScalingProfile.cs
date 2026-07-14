using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Data.Balance
{
    [CreateAssetMenu(
        fileName = "GateScalingProfile",
        menuName = "Chibi Pixel Gate/Balance/Gate Scaling Profile")]
    public sealed class GateScalingProfile : ScriptableObject
    {
        [SerializeField] private string profileVersion = "balance-v1.3.0-survival-bridge";
        [SerializeField] private List<GateScalingPhase> phases = CreateDefaultPhases();
        [SerializeField] private MajorGateSettings majorSettings = new MajorGateSettings();
        [SerializeField] private GateRunStatCaps runStatCaps = new GateRunStatCaps();

        private readonly HashSet<string> _warnedMissingOverrides = new HashSet<string>();

        public string ProfileVersion => profileVersion;
        public IReadOnlyList<GateScalingPhase> Phases => phases;
        public MajorGateSettings MajorSettings => majorSettings;
        public GateRunStatCaps RunStatCaps => runStatCaps;

        public GateScalingPhase EvaluatePhase(float elapsedSeconds)
        {
            EnsureDefaults();
            float safeElapsed = Mathf.Max(0f, elapsedSeconds);
            GateScalingPhase result = phases[0];

            for (int index = 0; index < phases.Count; index++)
            {
                GateScalingPhase phase = phases[index];
                if (phase != null && phase.StartSeconds <= safeElapsed)
                {
                    result = phase;
                }
            }

            return result;
        }

        public ResolvedGateEntry Resolve(BalanceGateEntry baseEntry, float elapsedSeconds)
        {
            if (baseEntry == null)
            {
                return default;
            }

            GateScalingPhase phase = EvaluatePhase(elapsedSeconds);
            GatePhaseOverride gateOverride = phase?.FindOverride(baseEntry.GateId);
            if (gateOverride == null)
            {
                WarnMissingOverrideOnce(baseEntry.GateId, phase != null ? phase.PhaseId : string.Empty);
                return ResolvedGateEntry.FromBase(baseEntry, phase != null ? phase.PhaseId : string.Empty);
            }

            return ResolvedGateEntry.FromOverride(baseEntry, phase.PhaseId, gateOverride);
        }

        public float GetMajorChance(float elapsedSeconds)
        {
            majorSettings ??= new MajorGateSettings();
            return majorSettings.GetChance(elapsedSeconds);
        }

        public void ValidateValues()
        {
            if (string.IsNullOrWhiteSpace(profileVersion))
            {
                profileVersion = "balance-v1.3.0-survival-bridge";
            }

            EnsureDefaults();
            phases.RemoveAll(phase => phase == null);
            EnsureDefaults();
            phases.Sort((left, right) => left.StartSeconds.CompareTo(right.StartSeconds));

            for (int index = 0; index < phases.Count; index++)
            {
                phases[index].Validate(index);
            }

            majorSettings ??= new MajorGateSettings();
            majorSettings.Validate();
            runStatCaps ??= new GateRunStatCaps();
            runStatCaps.Validate();
        }

        private void EnsureDefaults()
        {
            if (phases == null || phases.Count == 0)
            {
                phases = CreateDefaultPhases();
            }
        }

        private void WarnMissingOverrideOnce(string gateId, string phaseId)
        {
            if (!Application.isEditor && !Debug.isDebugBuild)
            {
                return;
            }

            string key = $"{gateId}|{phaseId}";
            if (_warnedMissingOverrides.Add(key))
            {
                Debug.LogWarning($"Gate scaling override missing for gate '{gateId}' in phase '{phaseId}'. Using base gate values.");
            }
        }

        private void OnValidate()
        {
            ValidateValues();
        }

        public static List<GateScalingPhase> CreateDefaultPhases()
        {
            return new List<GateScalingPhase>
            {
                GateScalingPhase.Create("early", 0f, new []
                {
                    O("stable_damage", "DAMAGE +10%", 1.10f, offerWeightMultiplier: 1.0f),
                    O("stable_fire_rate", "FIRE RATE +0.2", 0.20f, offerWeightMultiplier: 1.0f),
                    O("stable_vitality", "MAX HP +8%", 1.08f, offerWeightMultiplier: 1.0f),
                    O("utility_repair", "REPAIR 20%", 0.20f),
                    O("utility_barrier", "BARRIER 1 HIT", 1f, 15f),
                    O("utility_freeze", "FREEZE 20S", 0.75f, 20f),
                    O("risky_glass_cannon", "GLASS CANNON", 1.25f, 0f, 0f, 0f, 1.20f),
                    O("risky_bullet_storm", "BULLET STORM", 1f, 0f, 0f, 0f, 0.90f, 20f),
                    O("risky_reinforcement", "REINFORCEMENT", 1f, 0f, 0f, 0f, 1.15f, 25f),
                    O("risky_bounty", "BOUNTY 30S", 1.5f, 30f, 1.15f, 30f),
                    O("major_projectile", "PROJECTILE +2", 2f),
                    O("major_recruit", "RECRUIT +2", 2f),
                    O("major_overclock", "OVERCLOCK", 1.15f, 0f, 1.08f)
                }),
                GateScalingPhase.Create("growth", 90f, new []
                {
                    O("stable_damage", "DAMAGE +12%", 1.12f, offerWeightMultiplier: 1.5f),
                    O("stable_fire_rate", "FIRE RATE +0.3", 0.30f, offerWeightMultiplier: 1.5f),
                    O("stable_vitality", "MAX HP +10%", 1.10f, offerWeightMultiplier: 1.0f),
                    O("utility_repair", "REPAIR 25%", 0.25f),
                    O("utility_barrier", "BARRIER 1 HIT", 1f, 18f),
                    O("utility_freeze", "FREEZE 22S", 0.72f, 22f),
                    O("risky_glass_cannon", "GLASS CANNON", 1.30f, 0f, 0f, 0f, 1.18f),
                    O("risky_bullet_storm", "BULLET STORM", 1f, 0f, 0f, 0f, 0.92f, 20f),
                    O("risky_reinforcement", "REINFORCEMENT", 1f, 0f, 0f, 0f, 1.12f, 25f),
                    O("risky_bounty", "BOUNTY 30S", 1.5f, 30f, 1.15f, 30f),
                    O("major_projectile", "PROJECTILE +2", 2f),
                    O("major_recruit", "RECRUIT +2", 2f),
                    O("major_overclock", "OVERCLOCK", 1.20f, 0f, 1.10f)
                }),
                GateScalingPhase.Create("pressure", 180f, new []
                {
                    O("stable_damage", "DAMAGE +15%", 1.15f, offerWeightMultiplier: 2.0f),
                    O("stable_fire_rate", "FIRE RATE +0.4", 0.40f, offerWeightMultiplier: 2.0f),
                    O("stable_vitality", "MAX HP +12%", 1.12f, offerWeightMultiplier: 1.25f),
                    O("utility_repair", "REPAIR 30%", 0.30f),
                    O("utility_barrier", "BARRIER 2 HIT", 2f, 18f),
                    O("utility_freeze", "FREEZE 25S", 0.68f, 25f),
                    O("risky_glass_cannon", "GLASS CANNON", 1.35f, 0f, 0f, 0f, 1.16f),
                    O("risky_bullet_storm", "BULLET STORM", 2f, 0f, 0f, 0f, 0.94f, 20f),
                    O("risky_reinforcement", "REINFORCEMENT", 2f, 0f, 0f, 0f, 1.10f, 20f),
                    O("risky_bounty", "BOUNTY 30S", 1.5f, 30f, 1.15f, 30f),
                    O("major_projectile", "PROJECTILE +2", 2f),
                    O("major_recruit", "RECRUIT +2", 2f),
                    O("major_overclock", "OVERCLOCK", 1.25f, 0f, 1.12f)
                }),
                GateScalingPhase.Create("late", 300f, new []
                {
                    O("stable_damage", "DAMAGE +18%", 1.18f, offerWeightMultiplier: 2.0f),
                    O("stable_fire_rate", "FIRE RATE +0.5", 0.50f, offerWeightMultiplier: 2.0f),
                    O("stable_vitality", "MAX HP +15%", 1.15f, offerWeightMultiplier: 1.5f),
                    O("utility_repair", "REPAIR 40%", 0.40f),
                    O("utility_barrier", "BARRIER 2 HIT", 2f, 22f),
                    O("utility_freeze", "FREEZE 28S", 0.64f, 28f),
                    O("risky_glass_cannon", "GLASS CANNON", 1.40f, 0f, 0f, 0f, 1.14f),
                    O("risky_bullet_storm", "BULLET STORM", 2f, 0f, 0f, 0f, 0.96f, 15f),
                    O("risky_reinforcement", "REINFORCEMENT", 2f, 0f, 0f, 0f, 1.08f, 20f),
                    O("risky_bounty", "BOUNTY 30S", 1.5f, 30f, 1.15f, 30f),
                    O("major_projectile", "PROJECTILE +3", 3f),
                    O("major_recruit", "RECRUIT +3", 3f),
                    O("major_overclock", "OVERCLOCK", 1.30f, 0f, 1.15f)
                }),
                GateScalingPhase.Create("endgame", 420f, new []
                {
                    O("stable_damage", "DAMAGE +20%", 1.20f, offerWeightMultiplier: 1.5f),
                    O("stable_fire_rate", "FIRE RATE +0.6", 0.60f, offerWeightMultiplier: 1.5f),
                    O("stable_vitality", "MAX HP +18%", 1.18f, offerWeightMultiplier: 2.0f),
                    O("utility_repair", "REPAIR 50%", 0.50f),
                    O("utility_barrier", "BARRIER 3 HIT", 3f, 25f),
                    O("utility_freeze", "FREEZE 30S", 0.60f, 30f),
                    O("risky_glass_cannon", "GLASS CANNON", 1.45f, 0f, 0f, 0f, 1.12f),
                    O("risky_bullet_storm", "BULLET STORM", 2f, 0f, 0f, 0f, 0.98f, 15f),
                    O("risky_reinforcement", "REINFORCEMENT", 2f, 0f, 0f, 0f, 1.05f, 15f),
                    O("risky_bounty", "BOUNTY 30S", 1.5f, 30f, 1.15f, 30f),
                    O("major_projectile", "PROJECTILE +3", 3f),
                    O("major_recruit", "RECRUIT +3", 3f),
                    O("major_overclock", "OVERCLOCK", 1.35f, 0f, 1.18f)
                })
            };
        }

        private static GatePhaseOverride O(
            string gateId,
            string label,
            float magnitude,
            float duration = 0f,
            float secondaryMagnitude = 0f,
            float secondaryDuration = 0f,
            float drawbackMagnitude = 0f,
            float drawbackDuration = 0f,
            float offerWeightMultiplier = 1f)
        {
            return new GatePhaseOverride(
                gateId,
                label,
                magnitude,
                duration,
                secondaryMagnitude,
                secondaryDuration,
                drawbackMagnitude,
                drawbackDuration,
                offerWeightMultiplier);
        }
    }

    [Serializable]
    public sealed class GateScalingPhase
    {
        [SerializeField] private string phaseId;
        [SerializeField] private float startSeconds;
        [SerializeField] private List<GatePhaseOverride> overrides = new List<GatePhaseOverride>();

        public string PhaseId => phaseId;
        public float StartSeconds => Mathf.Max(0f, startSeconds);
        public IReadOnlyList<GatePhaseOverride> Overrides => overrides;

        public static GateScalingPhase Create(
            string phaseId,
            float startSeconds,
            IEnumerable<GatePhaseOverride> overrides)
        {
            return new GateScalingPhase
            {
                phaseId = phaseId,
                startSeconds = Mathf.Max(0f, startSeconds),
                overrides = new List<GatePhaseOverride>(overrides)
            };
        }

        public GatePhaseOverride FindOverride(string gateId)
        {
            if (string.IsNullOrWhiteSpace(gateId) || overrides == null)
            {
                return null;
            }

            for (int index = 0; index < overrides.Count; index++)
            {
                GatePhaseOverride candidate = overrides[index];
                if (candidate != null && string.Equals(candidate.GateId, gateId, StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            return null;
        }

        public void Validate(int fallbackIndex)
        {
            phaseId = string.IsNullOrWhiteSpace(phaseId)
                ? $"phase_{fallbackIndex}"
                : phaseId.Trim();
            startSeconds = Mathf.Max(0f, startSeconds);
            overrides ??= new List<GatePhaseOverride>();
            overrides.RemoveAll(value => value == null);

            for (int index = 0; index < overrides.Count; index++)
            {
                overrides[index].Validate();
            }
        }
    }

    [Serializable]
    public sealed class GatePhaseOverride
    {
        [SerializeField] private string gateId;
        [SerializeField] private string displayLabel;
        [SerializeField] private float magnitude;
        [SerializeField] private float durationSeconds;
        [SerializeField] private float secondaryMagnitude;
        [SerializeField] private float secondaryDurationSeconds;
        [SerializeField] private float drawbackMagnitude;
        [SerializeField] private float drawbackDurationSeconds;
        [SerializeField] private float offerWeightMultiplier = 1f;

        public string GateId => gateId;
        public string DisplayLabel => displayLabel;
        public float Magnitude => magnitude;
        public float DurationSeconds => durationSeconds;
        public float SecondaryMagnitude => secondaryMagnitude;
        public float SecondaryDurationSeconds => secondaryDurationSeconds;
        public float DrawbackMagnitude => drawbackMagnitude;
        public float DrawbackDurationSeconds => drawbackDurationSeconds;
        public float OfferWeightMultiplier => Mathf.Max(0.01f, offerWeightMultiplier);

        public GatePhaseOverride(
            string gateId,
            string displayLabel,
            float magnitude,
            float durationSeconds,
            float secondaryMagnitude,
            float secondaryDurationSeconds,
            float drawbackMagnitude,
            float drawbackDurationSeconds,
            float offerWeightMultiplier = 1f)
        {
            this.gateId = gateId;
            this.displayLabel = displayLabel;
            this.magnitude = magnitude;
            this.durationSeconds = durationSeconds;
            this.secondaryMagnitude = secondaryMagnitude;
            this.secondaryDurationSeconds = secondaryDurationSeconds;
            this.drawbackMagnitude = drawbackMagnitude;
            this.drawbackDurationSeconds = drawbackDurationSeconds;
            this.offerWeightMultiplier = offerWeightMultiplier;
            Validate();
        }

        public void Validate()
        {
            gateId = string.IsNullOrWhiteSpace(gateId) ? string.Empty : gateId.Trim();
            displayLabel = string.IsNullOrWhiteSpace(displayLabel) ? gateId : displayLabel.Trim();
            magnitude = Mathf.Max(0f, magnitude);
            durationSeconds = Mathf.Max(0f, durationSeconds);
            secondaryMagnitude = Mathf.Max(0f, secondaryMagnitude);
            secondaryDurationSeconds = Mathf.Max(0f, secondaryDurationSeconds);
            drawbackMagnitude = Mathf.Max(0f, drawbackMagnitude);
            drawbackDurationSeconds = Mathf.Max(0f, drawbackDurationSeconds);
            offerWeightMultiplier = offerWeightMultiplier <= 0f
                ? 1f
                : Mathf.Max(0.01f, offerWeightMultiplier);
        }
    }

    [Serializable]
    public sealed class MajorGateSettings
    {
        [SerializeField] private float unlockSeconds = 60f;
        [SerializeField] private float earlyChance = 0.25f;
        [SerializeField] private float midChance = 0.40f;
        [SerializeField] private float lateChance = 0.60f;
        [SerializeField] private int guaranteedAfterEligibleMisses = 1;

        public float UnlockSeconds => unlockSeconds;
        public float EarlyChance => earlyChance;
        public float MidChance => midChance;
        public float LateChance => lateChance;
        public int GuaranteedAfterEligibleMisses => guaranteedAfterEligibleMisses;

        public float GetChance(float elapsedSeconds)
        {
            if (elapsedSeconds < unlockSeconds)
            {
                return 0f;
            }

            if (elapsedSeconds < 180f)
            {
                return earlyChance;
            }

            if (elapsedSeconds < 300f)
            {
                return midChance;
            }

            return lateChance;
        }

        public void Validate()
        {
            unlockSeconds = Mathf.Max(0f, unlockSeconds);
            earlyChance = Mathf.Clamp01(earlyChance);
            midChance = Mathf.Clamp01(midChance);
            lateChance = Mathf.Clamp01(lateChance);
            guaranteedAfterEligibleMisses = Mathf.Max(0, guaranteedAfterEligibleMisses);
        }
    }

    [Serializable]
    public sealed class GateRunStatCaps
    {
        [SerializeField] private float damage = 3.50f;
        [SerializeField] private float fireRate = 8.50f;
        [SerializeField] private float maxHp = 36f;
        [SerializeField] private int projectileCount = 9;
        [SerializeField] private int squadCount = 16;
        [SerializeField] private float maxIncomingDamageMultiplier = 1.75f;
        [SerializeField] private float maxEnemyPressureMultiplier = 1.50f;
        [SerializeField] private float minEnemySpeedMultiplier = 0.50f;

        public float Damage => damage;
        public float FireRate => fireRate;
        public float MaxHp => maxHp;
        public int ProjectileCount => projectileCount;
        public int SquadCount => squadCount;
        public float MaxIncomingDamageMultiplier => maxIncomingDamageMultiplier;
        public float MaxEnemyPressureMultiplier => maxEnemyPressureMultiplier;
        public float MinEnemySpeedMultiplier => minEnemySpeedMultiplier;

        public void Validate()
        {
            damage = Mathf.Max(0.01f, damage);
            fireRate = Mathf.Max(0.01f, fireRate);
            maxHp = Mathf.Max(1f, maxHp);
            projectileCount = Mathf.Max(1, projectileCount);
            squadCount = Mathf.Max(1, squadCount);
            maxIncomingDamageMultiplier = Mathf.Max(1f, maxIncomingDamageMultiplier);
            maxEnemyPressureMultiplier = Mathf.Max(1f, maxEnemyPressureMultiplier);
            minEnemySpeedMultiplier = Mathf.Clamp(minEnemySpeedMultiplier, 0.01f, 1f);
        }
    }

    public readonly struct ResolvedGateEntry
    {
        public readonly string GateId;
        public readonly string PhaseId;
        public readonly BalanceGateCategory Category;
        public readonly string DisplayLabel;
        public readonly BalanceEffectType EffectType;
        public readonly float Magnitude;
        public readonly float DurationSeconds;
        public readonly BalanceEffectType SecondaryEffectType;
        public readonly float SecondaryMagnitude;
        public readonly float SecondaryDurationSeconds;
        public readonly BalanceEffectType DrawbackType;
        public readonly float DrawbackMagnitude;
        public readonly float DrawbackDurationSeconds;
        public readonly float OfferWeightMultiplier;

        public ResolvedGateEntry(
            string gateId,
            string phaseId,
            BalanceGateCategory category,
            string displayLabel,
            BalanceEffectType effectType,
            float magnitude,
            float durationSeconds,
            BalanceEffectType secondaryEffectType,
            float secondaryMagnitude,
            float secondaryDurationSeconds,
            BalanceEffectType drawbackType,
            float drawbackMagnitude,
            float drawbackDurationSeconds,
            float offerWeightMultiplier = 1f)
        {
            GateId = gateId ?? string.Empty;
            PhaseId = phaseId ?? string.Empty;
            Category = category;
            DisplayLabel = displayLabel ?? string.Empty;
            EffectType = effectType;
            Magnitude = Mathf.Max(0f, magnitude);
            DurationSeconds = Mathf.Max(0f, durationSeconds);
            SecondaryEffectType = secondaryEffectType;
            SecondaryMagnitude = Mathf.Max(0f, secondaryMagnitude);
            SecondaryDurationSeconds = Mathf.Max(0f, secondaryDurationSeconds);
            DrawbackType = drawbackType;
            DrawbackMagnitude = Mathf.Max(0f, drawbackMagnitude);
            DrawbackDurationSeconds = Mathf.Max(0f, drawbackDurationSeconds);
            OfferWeightMultiplier = Mathf.Max(0.01f, offerWeightMultiplier);
        }

        public bool IsValid => !string.IsNullOrWhiteSpace(GateId);

        public static ResolvedGateEntry FromBase(BalanceGateEntry entry, string phaseId)
        {
            return new ResolvedGateEntry(
                entry.GateId,
                phaseId,
                entry.Category,
                entry.DisplayLabel,
                entry.EffectType,
                entry.Magnitude,
                entry.DurationSeconds,
                entry.SecondaryEffectType,
                entry.SecondaryMagnitude,
                entry.SecondaryDurationSeconds,
                entry.DrawbackType,
                entry.DrawbackMagnitude,
                entry.DrawbackDurationSeconds,
                1f);
        }

        public static ResolvedGateEntry FromOverride(
            BalanceGateEntry entry,
            string phaseId,
            GatePhaseOverride gateOverride)
        {
            return new ResolvedGateEntry(
                entry.GateId,
                phaseId,
                entry.Category,
                gateOverride.DisplayLabel,
                entry.EffectType,
                gateOverride.Magnitude,
                gateOverride.DurationSeconds,
                entry.SecondaryEffectType,
                gateOverride.SecondaryMagnitude,
                gateOverride.SecondaryDurationSeconds,
                entry.DrawbackType,
                gateOverride.DrawbackMagnitude,
                gateOverride.DrawbackDurationSeconds,
                gateOverride.OfferWeightMultiplier);
        }
    }

    public readonly struct MajorRollResult
    {
        public readonly bool IsEligible;
        public readonly bool WasForced;
        public readonly bool MajorSpawned;
        public readonly int ConsecutiveMissesAfter;
        public readonly string FailureReason;

        public MajorRollResult(
            bool isEligible,
            bool wasForced,
            bool majorSpawned,
            int consecutiveMissesAfter,
            string failureReason)
        {
            IsEligible = isEligible;
            WasForced = wasForced;
            MajorSpawned = majorSpawned;
            ConsecutiveMissesAfter = Mathf.Max(0, consecutiveMissesAfter);
            FailureReason = failureReason ?? string.Empty;
        }

        public static MajorRollResult Evaluate(
            bool isEligible,
            float chance,
            float randomValue,
            int consecutiveMisses,
            int guaranteedAfterMisses,
            bool hasApplicableMajor)
        {
            int safeMisses = Mathf.Max(0, consecutiveMisses);
            if (!isEligible)
            {
                return new MajorRollResult(false, false, false, safeMisses, "not_eligible");
            }

            if (!hasApplicableMajor)
            {
                return new MajorRollResult(true, false, false, safeMisses, "no_applicable_major");
            }

            if (guaranteedAfterMisses > 0 && safeMisses >= guaranteedAfterMisses)
            {
                return new MajorRollResult(true, true, true, 0, string.Empty);
            }

            if (Mathf.Clamp01(randomValue) < Mathf.Clamp01(chance))
            {
                return new MajorRollResult(true, false, true, 0, string.Empty);
            }

            return new MajorRollResult(true, false, false, safeMisses + 1, "chance_failed");
        }
    }
}

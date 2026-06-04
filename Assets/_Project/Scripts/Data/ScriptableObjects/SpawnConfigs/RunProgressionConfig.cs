using System;
using System.Collections.Generic;
using _Project.Scripts.Gameplay.Enemies;
using UnityEngine;

namespace _Project.Scripts.Data.ScriptableObjects.SpawnConfigs
{
    /// <summary>
    /// Stores run-time enemy pressure tuning so balance can move without rewriting spawn logic.
    /// </summary>
    [CreateAssetMenu(fileName = "RunProgressionConfig", menuName = "Chibi Pixel Gate/Data/Run Progression Config")]
    public sealed class RunProgressionConfig : ScriptableObject
    {
        [SerializeField] private AnimationCurve spawnIntervalCurve = new AnimationCurve(
            new Keyframe(0f, 1.30f),
            new Keyframe(60f, 1.10f),
            new Keyframe(180f, 0.90f),
            new Keyframe(300f, 0.80f),
            new Keyframe(420f, 0.70f),
            new Keyframe(720f, 0.65f));

        [SerializeField] private AnimationCurve spawnBatchSizeCurve = new AnimationCurve(
            new Keyframe(0f, 2f),
            new Keyframe(60f, 2f),
            new Keyframe(180f, 2.5f),
            new Keyframe(300f, 3f),
            new Keyframe(420f, 3f),
            new Keyframe(720f, 3.5f));

        [SerializeField] private AnimationCurve maxActiveEnemiesCurve = new AnimationCurve(
            new Keyframe(0f, 45f),
            new Keyframe(60f, 60f),
            new Keyframe(180f, 85f),
            new Keyframe(300f, 110f),
            new Keyframe(420f, 130f),
            new Keyframe(720f, 150f));

        [SerializeField] private AnimationCurve enemyHpMultiplierCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(60f, 1.2f),
            new Keyframe(180f, 1.75f),
            new Keyframe(300f, 2.4f),
            new Keyframe(420f, 3f),
            new Keyframe(720f, 4.2f));

        [SerializeField] private AnimationCurve enemyMoveSpeedMultiplierCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(60f, 1.05f),
            new Keyframe(180f, 1.12f),
            new Keyframe(300f, 1.18f),
            new Keyframe(420f, 1.24f),
            new Keyframe(720f, 1.32f));

        [SerializeField] private AnimationCurve enemyDamageMultiplierCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(60f, 1f),
            new Keyframe(180f, 1.15f),
            new Keyframe(300f, 1.3f),
            new Keyframe(420f, 1.45f),
            new Keyframe(720f, 1.75f));

        [SerializeField] private AnimationCurve enemyProjectileSpeedMultiplierCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(60f, 1.05f),
            new Keyframe(180f, 1.12f),
            new Keyframe(300f, 1.2f),
            new Keyframe(420f, 1.25f),
            new Keyframe(720f, 1.35f));

        [SerializeField] private List<EnemyProgressionWeightRule> enemyWeightRules = new List<EnemyProgressionWeightRule>
        {
            new EnemyProgressionWeightRule(
                EnemyProgressionRole.BasicMelee,
                0f,
                new AnimationCurve(
                    new Keyframe(0f, 100f),
                    new Keyframe(180f, 90f),
                    new Keyframe(300f, 80f),
                    new Keyframe(420f, 70f),
                    new Keyframe(720f, 70f))),
            new EnemyProgressionWeightRule(
                EnemyProgressionRole.ExploderMelee,
                45f,
                new AnimationCurve(
                    new Keyframe(0f, 0f),
                    new Keyframe(45f, 8f),
                    new Keyframe(180f, 14f),
                    new Keyframe(300f, 18f),
                    new Keyframe(420f, 20f),
                    new Keyframe(720f, 20f))),
            new EnemyProgressionWeightRule(
                EnemyProgressionRole.Ranged,
                75f,
                new AnimationCurve(
                    new Keyframe(0f, 0f),
                    new Keyframe(75f, 6f),
                    new Keyframe(240f, 12f),
                    new Keyframe(360f, 16f),
                    new Keyframe(420f, 18f),
                    new Keyframe(720f, 18f)))
        };

        private static readonly ProgressionKey[] DefaultSpawnIntervalKeys =
        {
            new ProgressionKey(0f, 1.30f),
            new ProgressionKey(60f, 1.10f),
            new ProgressionKey(180f, 0.90f),
            new ProgressionKey(300f, 0.80f),
            new ProgressionKey(420f, 0.70f),
            new ProgressionKey(720f, 0.65f)
        };

        private static readonly ProgressionKey[] DefaultSpawnBatchSizeKeys =
        {
            new ProgressionKey(0f, 2f),
            new ProgressionKey(60f, 2f),
            new ProgressionKey(180f, 2.5f),
            new ProgressionKey(300f, 3f),
            new ProgressionKey(420f, 3f),
            new ProgressionKey(720f, 3.5f)
        };

        private static readonly ProgressionKey[] DefaultMaxActiveEnemyKeys =
        {
            new ProgressionKey(0f, 45f),
            new ProgressionKey(60f, 60f),
            new ProgressionKey(180f, 85f),
            new ProgressionKey(300f, 110f),
            new ProgressionKey(420f, 130f),
            new ProgressionKey(720f, 150f)
        };

        private static readonly ProgressionKey[] DefaultHpMultiplierKeys =
        {
            new ProgressionKey(0f, 1f),
            new ProgressionKey(60f, 1.2f),
            new ProgressionKey(180f, 1.75f),
            new ProgressionKey(300f, 2.4f),
            new ProgressionKey(420f, 3f),
            new ProgressionKey(720f, 4.2f)
        };

        private static readonly ProgressionKey[] DefaultMoveSpeedMultiplierKeys =
        {
            new ProgressionKey(0f, 1f),
            new ProgressionKey(60f, 1.05f),
            new ProgressionKey(180f, 1.12f),
            new ProgressionKey(300f, 1.18f),
            new ProgressionKey(420f, 1.24f),
            new ProgressionKey(720f, 1.32f)
        };

        private static readonly ProgressionKey[] DefaultDamageMultiplierKeys =
        {
            new ProgressionKey(0f, 1f),
            new ProgressionKey(60f, 1f),
            new ProgressionKey(180f, 1.15f),
            new ProgressionKey(300f, 1.3f),
            new ProgressionKey(420f, 1.45f),
            new ProgressionKey(720f, 1.75f)
        };

        private static readonly ProgressionKey[] DefaultProjectileSpeedMultiplierKeys =
        {
            new ProgressionKey(0f, 1f),
            new ProgressionKey(60f, 1.05f),
            new ProgressionKey(180f, 1.12f),
            new ProgressionKey(300f, 1.2f),
            new ProgressionKey(420f, 1.25f),
            new ProgressionKey(720f, 1.35f)
        };

        private static readonly ProgressionKey[] DefaultBasicWeightKeys =
        {
            new ProgressionKey(0f, 100f),
            new ProgressionKey(180f, 90f),
            new ProgressionKey(300f, 80f),
            new ProgressionKey(420f, 70f),
            new ProgressionKey(720f, 70f)
        };

        private static readonly ProgressionKey[] DefaultExploderWeightKeys =
        {
            new ProgressionKey(0f, 0f),
            new ProgressionKey(45f, 8f),
            new ProgressionKey(180f, 14f),
            new ProgressionKey(300f, 18f),
            new ProgressionKey(420f, 20f),
            new ProgressionKey(720f, 20f)
        };

        private static readonly ProgressionKey[] DefaultRangedWeightKeys =
        {
            new ProgressionKey(0f, 0f),
            new ProgressionKey(75f, 6f),
            new ProgressionKey(240f, 12f),
            new ProgressionKey(360f, 16f),
            new ProgressionKey(420f, 18f),
            new ProgressionKey(720f, 18f)
        };

        public float GetSpawnInterval(float elapsedSeconds)
        {
            return Mathf.Max(0.01f, EvaluateCurveOrDefault(spawnIntervalCurve, elapsedSeconds, DefaultSpawnIntervalKeys));
        }

        public int GetMaxActiveEnemies(float elapsedSeconds)
        {
            return Mathf.Max(1, Mathf.RoundToInt(EvaluateCurveOrDefault(
                maxActiveEnemiesCurve,
                elapsedSeconds,
                DefaultMaxActiveEnemyKeys)));
        }

        public int GetSpawnBatchSize(float elapsedSeconds)
        {
            return Mathf.Max(1, Mathf.RoundToInt(EvaluateCurveOrDefault(
                spawnBatchSizeCurve,
                elapsedSeconds,
                DefaultSpawnBatchSizeKeys)));
        }

        public EnemyRunScaling GetEnemyRunScaling(float elapsedSeconds)
        {
            return new EnemyRunScaling(
                EvaluateCurveOrDefault(enemyHpMultiplierCurve, elapsedSeconds, DefaultHpMultiplierKeys),
                EvaluateCurveOrDefault(enemyMoveSpeedMultiplierCurve, elapsedSeconds, DefaultMoveSpeedMultiplierKeys),
                EvaluateCurveOrDefault(enemyDamageMultiplierCurve, elapsedSeconds, DefaultDamageMultiplierKeys),
                EvaluateCurveOrDefault(enemyProjectileSpeedMultiplierCurve, elapsedSeconds, DefaultProjectileSpeedMultiplierKeys));
        }

        public float GetUnlockAfterSeconds(EnemyProgressionRole role, float fallbackUnlockAfterSeconds)
        {
            EnemyProgressionWeightRule rule = FindWeightRule(role);
            if (rule != null)
            {
                return rule.UnlockAfterSeconds;
            }

            return GetDefaultUnlockAfterSeconds(role, fallbackUnlockAfterSeconds);
        }

        public float GetSpawnWeight(EnemyProgressionRole role, float elapsedSeconds, float fallbackWeight)
        {
            EnemyProgressionWeightRule rule = FindWeightRule(role);
            if (rule != null)
            {
                return rule.GetWeight(elapsedSeconds, fallbackWeight);
            }

            return GetDefaultSpawnWeight(role, elapsedSeconds, fallbackWeight);
        }

        public static float GetDefaultSpawnInterval(float elapsedSeconds)
        {
            return Mathf.Max(0.01f, EvaluateKeys(DefaultSpawnIntervalKeys, elapsedSeconds));
        }

        public static int GetDefaultMaxActiveEnemies(float elapsedSeconds)
        {
            return Mathf.Max(1, Mathf.RoundToInt(EvaluateKeys(DefaultMaxActiveEnemyKeys, elapsedSeconds)));
        }

        public static int GetDefaultSpawnBatchSize(float elapsedSeconds)
        {
            return Mathf.Max(1, Mathf.RoundToInt(EvaluateKeys(DefaultSpawnBatchSizeKeys, elapsedSeconds)));
        }

        public static EnemyRunScaling GetDefaultEnemyRunScaling(float elapsedSeconds)
        {
            return new EnemyRunScaling(
                EvaluateKeys(DefaultHpMultiplierKeys, elapsedSeconds),
                EvaluateKeys(DefaultMoveSpeedMultiplierKeys, elapsedSeconds),
                EvaluateKeys(DefaultDamageMultiplierKeys, elapsedSeconds),
                EvaluateKeys(DefaultProjectileSpeedMultiplierKeys, elapsedSeconds));
        }

        public static float GetDefaultUnlockAfterSeconds(EnemyProgressionRole role, float fallbackUnlockAfterSeconds)
        {
            return role switch
            {
                EnemyProgressionRole.BasicMelee => 0f,
                EnemyProgressionRole.ExploderMelee => 45f,
                EnemyProgressionRole.Ranged => 75f,
                _ => Mathf.Max(0f, fallbackUnlockAfterSeconds)
            };
        }

        public static float GetDefaultSpawnWeight(
            EnemyProgressionRole role,
            float elapsedSeconds,
            float fallbackWeight)
        {
            return role switch
            {
                EnemyProgressionRole.BasicMelee => EvaluateKeys(DefaultBasicWeightKeys, elapsedSeconds),
                EnemyProgressionRole.ExploderMelee => EvaluateKeys(DefaultExploderWeightKeys, elapsedSeconds),
                EnemyProgressionRole.Ranged => EvaluateKeys(DefaultRangedWeightKeys, elapsedSeconds),
                _ => Mathf.Max(0f, fallbackWeight)
            };
        }

        private EnemyProgressionWeightRule FindWeightRule(EnemyProgressionRole role)
        {
            if (role == EnemyProgressionRole.Auto || enemyWeightRules == null)
            {
                return null;
            }

            for (int index = 0; index < enemyWeightRules.Count; index++)
            {
                EnemyProgressionWeightRule rule = enemyWeightRules[index];
                if (rule != null && rule.Role == role)
                {
                    return rule;
                }
            }

            return null;
        }

        private static float EvaluateCurveOrDefault(
            AnimationCurve curve,
            float elapsedSeconds,
            IReadOnlyList<ProgressionKey> defaultKeys)
        {
            if (curve != null && curve.length > 0)
            {
                return Mathf.Max(0f, curve.Evaluate(Mathf.Max(0f, elapsedSeconds)));
            }

            return EvaluateKeys(defaultKeys, elapsedSeconds);
        }

        private static float EvaluateKeys(IReadOnlyList<ProgressionKey> keys, float elapsedSeconds)
        {
            if (keys == null || keys.Count <= 0)
            {
                return 0f;
            }

            float time = Mathf.Max(0f, elapsedSeconds);
            if (time <= keys[0].Time)
            {
                return keys[0].Value;
            }

            for (int index = 1; index < keys.Count; index++)
            {
                ProgressionKey previous = keys[index - 1];
                ProgressionKey next = keys[index];

                if (time > next.Time)
                {
                    continue;
                }

                float range = Mathf.Max(0.01f, next.Time - previous.Time);
                float t = Mathf.Clamp01((time - previous.Time) / range);
                return Mathf.Lerp(previous.Value, next.Value, t);
            }

            return keys[keys.Count - 1].Value;
        }

        private readonly struct ProgressionKey
        {
            public readonly float Time;
            public readonly float Value;

            public ProgressionKey(float time, float value)
            {
                Time = time;
                Value = value;
            }
        }
    }

    public enum EnemyProgressionRole
    {
        Auto = 0,
        BasicMelee = 1,
        ExploderMelee = 2,
        Ranged = 3
    }

    [Serializable]
    public sealed class EnemyProgressionWeightRule
    {
        [SerializeField] private EnemyProgressionRole role = EnemyProgressionRole.BasicMelee;
        [SerializeField] private float unlockAfterSeconds;
        [SerializeField] private AnimationCurve weightCurve = AnimationCurve.Linear(0f, 1f, 60f, 1f);

        public EnemyProgressionWeightRule(
            EnemyProgressionRole role,
            float unlockAfterSeconds,
            AnimationCurve weightCurve)
        {
            this.role = role;
            this.unlockAfterSeconds = unlockAfterSeconds;
            this.weightCurve = weightCurve;
        }

        public EnemyProgressionRole Role => role;
        public float UnlockAfterSeconds => Mathf.Max(0f, unlockAfterSeconds);

        public float GetWeight(float elapsedSeconds, float fallbackWeight)
        {
            if (weightCurve == null || weightCurve.length <= 0)
            {
                return Mathf.Max(0f, fallbackWeight);
            }

            return Mathf.Max(0f, weightCurve.Evaluate(Mathf.Max(0f, elapsedSeconds)));
        }
    }
}

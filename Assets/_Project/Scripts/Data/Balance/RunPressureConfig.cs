using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Data.Balance
{
    [CreateAssetMenu(
        fileName = "RunPressureConfig_v1",
        menuName = "Chibi Pixel Gate/Balance/Run Pressure Config")]
    public sealed class RunPressureConfig : ScriptableObject
    {
        private static readonly float[] DefaultNodeTimes =
        {
            0f,
            60f,
            180f,
            300f,
            420f,
            720f
        };

        private static readonly RunPressureSnapshot[] DefaultSnapshots =
        {
            new RunPressureSnapshot(12, 8, 2f, 3f, 1f, 0.75f, 1f),
            new RunPressureSnapshot(18, 12, 4f, 4f, 1.15f, 0.85f, 1.05f),
            new RunPressureSnapshot(28, 20, 7f, 6f, 1.5f, 1f, 1.12f),
            new RunPressureSnapshot(38, 27, 10f, 8f, 2.1f, 1.2f, 1.18f),
            new RunPressureSnapshot(48, 34, 13f, 10f, 2.9f, 1.45f, 1.24f),
            new RunPressureSnapshot(60, 42, 16f, 12f, 4.5f, 1.9f, 1.32f)
        };

        [SerializeField] private string configVersion = CombatScalingConfig.DefaultConfigVersion;
        [SerializeField] private List<RunPressureNode> nodes = CreateDefaultNodes();

        public string ConfigVersion => configVersion;
        public IReadOnlyList<RunPressureNode> Nodes => nodes;

        public RunPressureSnapshot Evaluate(float elapsedSeconds)
        {
            EnsureDefaults();
            float time = Mathf.Max(0f, elapsedSeconds);

            if (time <= nodes[0].TimeSeconds)
            {
                return nodes[0].ToSnapshot();
            }

            for (int index = 1; index < nodes.Count; index++)
            {
                RunPressureNode previous = nodes[index - 1];
                RunPressureNode next = nodes[index];

                if (time > next.TimeSeconds)
                {
                    continue;
                }

                float duration = Mathf.Max(0.01f, next.TimeSeconds - previous.TimeSeconds);
                float t = Mathf.Clamp01((time - previous.TimeSeconds) / duration);
                return RunPressureSnapshot.Lerp(previous.ToSnapshot(), next.ToSnapshot(), t);
            }

            return nodes[nodes.Count - 1].ToSnapshot();
        }

        public static RunPressureSnapshot EvaluateDefault(float elapsedSeconds)
        {
            float time = Mathf.Max(0f, elapsedSeconds);

            if (time <= DefaultNodeTimes[0])
            {
                return DefaultSnapshots[0];
            }

            for (int index = 1; index < DefaultNodeTimes.Length; index++)
            {
                if (time > DefaultNodeTimes[index])
                {
                    continue;
                }

                float duration = DefaultNodeTimes[index] - DefaultNodeTimes[index - 1];
                float t = Mathf.Clamp01((time - DefaultNodeTimes[index - 1]) / duration);
                return RunPressureSnapshot.Lerp(
                    DefaultSnapshots[index - 1],
                    DefaultSnapshots[index],
                    t);
            }

            return DefaultSnapshots[DefaultSnapshots.Length - 1];
        }

        public void ValidateValues()
        {
            if (string.IsNullOrWhiteSpace(configVersion))
            {
                configVersion = CombatScalingConfig.DefaultConfigVersion;
            }

            EnsureDefaults();

            nodes.RemoveAll(node => node == null);
            EnsureDefaults();

            nodes.Sort((left, right) => left.TimeSeconds.CompareTo(right.TimeSeconds));

            for (int index = 0; index < nodes.Count; index++)
            {
                nodes[index].Validate(index == 0 ? 0f : nodes[index - 1].TimeSeconds);
            }
        }

        private static List<RunPressureNode> CreateDefaultNodes()
        {
            return new List<RunPressureNode>
            {
                new RunPressureNode(0f, 12, 8, 2f, 3f, 1f, 0.75f, 1f),
                new RunPressureNode(60f, 18, 12, 4f, 4f, 1.15f, 0.85f, 1.05f),
                new RunPressureNode(180f, 28, 20, 7f, 6f, 1.5f, 1f, 1.12f),
                new RunPressureNode(300f, 38, 27, 10f, 8f, 2.1f, 1.2f, 1.18f),
                new RunPressureNode(420f, 48, 34, 13f, 10f, 2.9f, 1.45f, 1.24f),
                new RunPressureNode(720f, 60, 42, 16f, 12f, 4.5f, 1.9f, 1.32f)
            };
        }

        private void EnsureDefaults()
        {
            if (nodes == null || nodes.Count == 0)
            {
                nodes = CreateDefaultNodes();
            }
        }

        private void OnValidate()
        {
            ValidateValues();
        }
    }

    [Serializable]
    public sealed class RunPressureNode
    {
        [SerializeField] private float timeSeconds;
        [SerializeField] private int activeCap;
        [SerializeField] private int minimumVisible;
        [SerializeField] private float threatBudget;
        [SerializeField] private float spawnPerSecond;
        [SerializeField] private float hpMultiplier;
        [SerializeField] private float damageMultiplier;
        [SerializeField] private float speedMultiplier;

        public RunPressureNode(
            float timeSeconds,
            int activeCap,
            int minimumVisible,
            float threatBudget,
            float spawnPerSecond,
            float hpMultiplier,
            float damageMultiplier,
            float speedMultiplier)
        {
            this.timeSeconds = timeSeconds;
            this.activeCap = activeCap;
            this.minimumVisible = minimumVisible;
            this.threatBudget = threatBudget;
            this.spawnPerSecond = spawnPerSecond;
            this.hpMultiplier = hpMultiplier;
            this.damageMultiplier = damageMultiplier;
            this.speedMultiplier = speedMultiplier;
            Validate(0f);
        }

        public float TimeSeconds => timeSeconds;
        public int ActiveCap => activeCap;
        public int MinimumVisible => minimumVisible;
        public float ThreatBudget => threatBudget;
        public float SpawnPerSecond => spawnPerSecond;
        public float HpMultiplier => hpMultiplier;
        public float DamageMultiplier => damageMultiplier;
        public float SpeedMultiplier => speedMultiplier;

        public void Validate(float minimumTime)
        {
            timeSeconds = Mathf.Max(0f, Mathf.Max(minimumTime, timeSeconds));
            activeCap = Mathf.Max(1, activeCap);
            minimumVisible = Mathf.Clamp(minimumVisible, 0, activeCap);
            threatBudget = Mathf.Max(0f, threatBudget);
            spawnPerSecond = Mathf.Max(0.01f, spawnPerSecond);
            hpMultiplier = Mathf.Max(0.01f, hpMultiplier);
            damageMultiplier = Mathf.Max(0f, damageMultiplier);
            speedMultiplier = Mathf.Max(0f, speedMultiplier);
        }

        public RunPressureSnapshot ToSnapshot()
        {
            return new RunPressureSnapshot(
                activeCap,
                minimumVisible,
                threatBudget,
                spawnPerSecond,
                hpMultiplier,
                damageMultiplier,
                speedMultiplier);
        }
    }

    public readonly struct RunPressureSnapshot
    {
        public readonly int ActiveCap;
        public readonly int MinimumVisible;
        public readonly float ThreatBudget;
        public readonly float SpawnPerSecond;
        public readonly float HpMultiplier;
        public readonly float DamageMultiplier;
        public readonly float SpeedMultiplier;

        public RunPressureSnapshot(
            int activeCap,
            int minimumVisible,
            float threatBudget,
            float spawnPerSecond,
            float hpMultiplier,
            float damageMultiplier,
            float speedMultiplier)
        {
            ActiveCap = Mathf.Max(1, activeCap);
            MinimumVisible = Mathf.Clamp(minimumVisible, 0, ActiveCap);
            ThreatBudget = Mathf.Max(0f, threatBudget);
            SpawnPerSecond = Mathf.Max(0.01f, spawnPerSecond);
            HpMultiplier = Mathf.Max(0.01f, hpMultiplier);
            DamageMultiplier = Mathf.Max(0f, damageMultiplier);
            SpeedMultiplier = Mathf.Max(0f, speedMultiplier);
        }

        public static RunPressureSnapshot Lerp(
            RunPressureSnapshot from,
            RunPressureSnapshot to,
            float t)
        {
            float clampedT = Mathf.Clamp01(t);
            int activeCap = Mathf.RoundToInt(Mathf.Lerp(from.ActiveCap, to.ActiveCap, clampedT));
            int minimumVisible = Mathf.RoundToInt(
                Mathf.Lerp(from.MinimumVisible, to.MinimumVisible, clampedT));

            return new RunPressureSnapshot(
                activeCap,
                Mathf.Min(minimumVisible, activeCap),
                Mathf.Lerp(from.ThreatBudget, to.ThreatBudget, clampedT),
                Mathf.Lerp(from.SpawnPerSecond, to.SpawnPerSecond, clampedT),
                Mathf.Lerp(from.HpMultiplier, to.HpMultiplier, clampedT),
                Mathf.Lerp(from.DamageMultiplier, to.DamageMultiplier, clampedT),
                Mathf.Lerp(from.SpeedMultiplier, to.SpeedMultiplier, clampedT));
        }
    }
}

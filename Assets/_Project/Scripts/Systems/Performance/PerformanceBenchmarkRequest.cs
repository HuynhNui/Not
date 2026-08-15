using System;

namespace _Project.Scripts.Systems.Performance
{
    [Serializable]
    public sealed class PerformanceBenchmarkRequest
    {
        public bool enabled = true;
        public string runId = "android-baseline";
        public string sourceCommit = "unknown";
        public string benchmarkProfileId = "performance-baseline-full-meta";
        public float menuWarmupSeconds = 10f;
        public float menuMeasurementSeconds = 30f;
        public float gameplayWarmupSeconds = 10f;
        public float gameplayDurationSeconds = 600f;
        public float sampleIntervalSeconds = 1f;
        public bool noGateBaseline = true;
        public bool invulnerable = true;
        public float startingDamage = 3f;
        public float startingFireRate = 6.4f;
        public float startingMaxHp = 20f;
        public int startingProjectileCount = 3;
        public int startingSquadSize = 4;
        public bool autoconnectProfiler;
        public bool deepProfiling;

        public void Validate()
        {
            runId = SanitizePathSegment(runId, "android-baseline");
            sourceCommit = string.IsNullOrWhiteSpace(sourceCommit) ? "unknown" : sourceCommit.Trim();
            benchmarkProfileId = string.IsNullOrWhiteSpace(benchmarkProfileId)
                ? "performance-baseline-full-meta"
                : benchmarkProfileId.Trim();
            menuWarmupSeconds = Math.Max(0f, menuWarmupSeconds);
            menuMeasurementSeconds = Math.Max(1f, menuMeasurementSeconds);
            gameplayWarmupSeconds = Math.Max(0f, gameplayWarmupSeconds);
            gameplayDurationSeconds = Math.Max(gameplayWarmupSeconds + 1f, gameplayDurationSeconds);
            sampleIntervalSeconds = Math.Max(0.25f, sampleIntervalSeconds);
            startingDamage = Math.Max(0.01f, startingDamage);
            startingFireRate = Math.Max(0.01f, startingFireRate);
            startingMaxHp = Math.Max(1f, startingMaxHp);
            startingProjectileCount = Math.Max(1, startingProjectileCount);
            startingSquadSize = Math.Max(1, startingSquadSize);
        }

        private static string SanitizePathSegment(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            char[] characters = value.Trim().ToCharArray();
            for (int index = 0; index < characters.Length; index++)
            {
                char character = characters[index];
                if (!char.IsLetterOrDigit(character) && character != '-' && character != '_')
                {
                    characters[index] = '_';
                }
            }

            return new string(characters);
        }
    }
}

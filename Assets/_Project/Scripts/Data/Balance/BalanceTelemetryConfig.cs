using UnityEngine;

namespace _Project.Scripts.Data.Balance
{
    [CreateAssetMenu(
        fileName = "BalanceTelemetryConfig_v1",
        menuName = "Chibi Pixel Gate/Balance/Telemetry Config")]
    public sealed class BalanceTelemetryConfig : ScriptableObject
    {
        [SerializeField] private string telemetryConfigVersion = CombatScalingConfig.DefaultConfigVersion;
        [SerializeField] private float snapshotIntervalSeconds = 15f;
        [SerializeField] private bool exportCsv = true;
        [SerializeField] private bool exportJsonl = true;
        [SerializeField] private int maxSnapshotsPerRun = 80;
        [SerializeField] private bool developmentBuildOnly = true;

        public string TelemetryConfigVersion => telemetryConfigVersion;
        public float SnapshotIntervalSeconds => snapshotIntervalSeconds;
        public bool ExportCsv => exportCsv;
        public bool ExportJsonl => exportJsonl;
        public int MaxSnapshotsPerRun => maxSnapshotsPerRun;
        public bool DevelopmentBuildOnly => developmentBuildOnly;

        public void ValidateValues()
        {
            if (string.IsNullOrWhiteSpace(telemetryConfigVersion))
            {
                telemetryConfigVersion = CombatScalingConfig.DefaultConfigVersion;
            }

            snapshotIntervalSeconds = Mathf.Max(1f, snapshotIntervalSeconds);
            maxSnapshotsPerRun = Mathf.Max(1, maxSnapshotsPerRun);
        }

        private void OnValidate()
        {
            ValidateValues();
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Data.Balance
{
    [CreateAssetMenu(
        fileName = "BalanceBootstrapConfig",
        menuName = "Chibi Pixel Gate/Balance/Balance Bootstrap Config")]
    public sealed class BalanceBootstrapConfig : ScriptableObject
    {
        [SerializeField] private string activeBalanceVersion = CombatScalingConfig.DefaultConfigVersion;
        [SerializeField] private CombatScalingConfig combatScalingConfig;
        [SerializeField] private PlayerMetaBalanceConfig playerMetaBalanceConfig;
        [SerializeField] private GatePoolConfig gatePoolConfig;
        [SerializeField] private GateScalingProfile gateScalingProfile;
        [SerializeField] private BalanceBenchmarkProfile benchmarkProfile;
        [SerializeField] private BalanceBenchmarkPreset benchmarkPreset = BalanceBenchmarkPreset.FullMetaStart;
        [SerializeField] private BalanceBenchmarkProfile runCapBenchmarkProfile;
        [SerializeField] private RunPressureConfig runPressureConfig;
        [SerializeField] private EconomyConfig economyConfig;
        [SerializeField] private BalanceTelemetryConfig telemetryConfig;
        [SerializeField] private List<EnemyRoleConfig> enemyRoleConfigs = new List<EnemyRoleConfig>();

        public string ActiveBalanceVersion => activeBalanceVersion;
        public CombatScalingConfig CombatScalingConfig => combatScalingConfig;
        public PlayerMetaBalanceConfig PlayerMetaBalanceConfig => playerMetaBalanceConfig;
        public GatePoolConfig GatePoolConfig => gatePoolConfig;
        public GateScalingProfile GateScalingProfile => gateScalingProfile;
        public BalanceBenchmarkProfile BenchmarkProfile => benchmarkProfile;
        public BalanceBenchmarkProfile FullMetaBenchmarkProfile => benchmarkProfile;
        public BalanceBenchmarkPreset BenchmarkPreset => benchmarkPreset;
        public BalanceBenchmarkProfile RunCapBenchmarkProfile => runCapBenchmarkProfile;
        public BalanceBenchmarkProfile ActiveBenchmarkProfile
        {
            get
            {
                if (!Application.isEditor && !Debug.isDebugBuild)
                {
                    return benchmarkProfile;
                }

                return benchmarkPreset == BalanceBenchmarkPreset.RunCapStart
                    ? runCapBenchmarkProfile != null ? runCapBenchmarkProfile : benchmarkProfile
                    : benchmarkProfile;
            }
        }
        public RunPressureConfig RunPressureConfig => runPressureConfig;
        public EconomyConfig EconomyConfig => economyConfig;
        public BalanceTelemetryConfig TelemetryConfig => telemetryConfig;
        public IReadOnlyList<EnemyRoleConfig> EnemyRoleConfigs => enemyRoleConfigs;

        public EnemyRoleConfig GetEnemyRoleConfig(BalanceEnemyRole role)
        {
            if (enemyRoleConfigs == null)
            {
                return null;
            }

            for (int index = 0; index < enemyRoleConfigs.Count; index++)
            {
                EnemyRoleConfig config = enemyRoleConfigs[index];
                if (config != null && config.Role == role)
                {
                    return config;
                }
            }

            return null;
        }

        public void ValidateValues()
        {
            if (string.IsNullOrWhiteSpace(activeBalanceVersion))
            {
                activeBalanceVersion = CombatScalingConfig.DefaultConfigVersion;
            }

            gateScalingProfile?.ValidateValues();
            enemyRoleConfigs ??= new List<EnemyRoleConfig>();
            enemyRoleConfigs.RemoveAll(config => config == null);
        }

        private void OnValidate()
        {
            ValidateValues();
        }
    }
}

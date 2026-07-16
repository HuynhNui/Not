using System.Collections.Generic;
using _Project.Scripts.Systems.ProgressionSystem;
using UnityEngine;

namespace _Project.Scripts.Systems.MissionSystem
{
    [CreateAssetMenu(
        fileName = "MissionCatalog_v1",
        menuName = "Chibi Pixel Gate/Missions/Mission Catalog")]
    public sealed class MissionCatalog : ScriptableObject
    {
        [SerializeField] private string catalogVersion = "mission-v1";
        [SerializeField] private List<MissionDefinition> missions = CreateDefaultMissionChain();

        public string CatalogVersion => catalogVersion;
        public IReadOnlyList<MissionDefinition> Missions => missions;

        public int Count => missions != null ? missions.Count : 0;

        public MissionDefinition GetMissionAt(int index)
        {
            return missions != null && index >= 0 && index < missions.Count
                ? missions[index]
                : null;
        }

        public MissionDefinition GetMissionById(string missionId)
        {
            if (string.IsNullOrWhiteSpace(missionId) || missions == null)
            {
                return null;
            }

            for (int index = 0; index < missions.Count; index++)
            {
                MissionDefinition mission = missions[index];
                if (mission != null && mission.Id == missionId)
                {
                    return mission;
                }
            }

            return null;
        }

        public int IndexOf(string missionId)
        {
            if (string.IsNullOrWhiteSpace(missionId) || missions == null)
            {
                return -1;
            }

            for (int index = 0; index < missions.Count; index++)
            {
                MissionDefinition mission = missions[index];
                if (mission != null && mission.Id == missionId)
                {
                    return index;
                }
            }

            return -1;
        }

        public void ResetToDefaultMissionChain()
        {
            catalogVersion = "mission-v1";
            missions = CreateDefaultMissionChain();
            ValidateValues();
        }

        public void ValidateValues()
        {
            catalogVersion = string.IsNullOrWhiteSpace(catalogVersion)
                ? "mission-v1"
                : catalogVersion.Trim();
            missions ??= CreateDefaultMissionChain();

            if (missions.Count == 0)
            {
                missions = CreateDefaultMissionChain();
            }

            for (int index = missions.Count - 1; index >= 0; index--)
            {
                if (missions[index] == null)
                {
                    missions.RemoveAt(index);
                    continue;
                }

                missions[index].Validate();
            }
        }

        public static MissionCatalog CreateRuntimeDefault()
        {
            MissionCatalog catalog = CreateInstance<MissionCatalog>();
            catalog.ResetToDefaultMissionChain();
            return catalog;
        }

        public static List<MissionDefinition> CreateDefaultMissionChain()
        {
            return new List<MissionDefinition>
            {
                Mission("boot_finish_tutorial", "BOOT", "FINISH TUTORIAL",
                    MissionObjectiveType.GameplayTutorialCompleted, MissionProgressMode.AbsoluteLifetime, 1),
                Mission("boot_first_loop", "BOOT", "COMPLETE FIRST LOOP",
                    MissionObjectiveType.TotalRunsCompleted, MissionProgressMode.AbsoluteLifetime, 1),
                Mission("boot_purchase_upgrade", "BOOT", "PURCHASE ANY UPGRADE",
                    MissionObjectiveType.AnyCoreUpgradePurchased, MissionProgressMode.DeltaSinceUnlock, 1),
                Mission("boot_select_3_gates", "BOOT", "PASS THROUGH 3 GATES",
                    MissionObjectiveType.GatesSelected, MissionProgressMode.DeltaSinceUnlock, 3),
                Mission("observe_3_loops", "OBSERVE", "COMPLETE 3 LOOPS",
                    MissionObjectiveType.TotalRunsCompleted, MissionProgressMode.AbsoluteLifetime, 3),
                Mission("observe_100_kills_run", "OBSERVE", "DEFEAT 100 ENEMIES IN ONE RUN",
                    MissionObjectiveType.SingleRunEnemyKills, MissionProgressMode.BestSingleRun, 100),
                Mission("observe_dmg_lv2", "OBSERVE", "UPGRADE DMG TO LV.2",
                    MissionObjectiveType.UpgradeLevel, MissionProgressMode.AbsoluteLifetime, 2, upgradeType: PlayerMetaUpgradeType.Damage),
                Mission("observe_survive_120", "OBSERVE", "SURVIVE 2 MINUTES",
                    MissionObjectiveType.SingleRunSurvivalTime, MissionProgressMode.BestSingleRun, 120),
                Mission("memory_select_10_gates", "MEMORY LEAK", "PASS THROUGH 10 GATES",
                    MissionObjectiveType.GatesSelected, MissionProgressMode.DeltaSinceUnlock, 10),
                Mission("memory_10_loops", "MEMORY LEAK", "COMPLETE 10 LOOPS",
                    MissionObjectiveType.TotalRunsCompleted, MissionProgressMode.AbsoluteLifetime, 10),
                Mission("memory_survive_180", "MEMORY LEAK", "SURVIVE 3 MINUTES",
                    MissionObjectiveType.SingleRunSurvivalTime, MissionProgressMode.BestSingleRun, 180),
                Mission("memory_three_upgrades_lv2", "MEMORY LEAK", "RAISE 3 CORE UPGRADES TO LV.2",
                    MissionObjectiveType.CoreUpgradesAtLevel, MissionProgressMode.AbsoluteLifetime, 3, 2),
                Mission("command_1000_total_kills", "HUMAN COMMAND", "DEFEAT 1,000 ENEMIES TOTAL",
                    MissionObjectiveType.TotalEnemyKills, MissionProgressMode.AbsoluteLifetime, 1000),
                Mission("command_20_loops", "HUMAN COMMAND", "COMPLETE 20 LOOPS",
                    MissionObjectiveType.TotalRunsCompleted, MissionProgressMode.AbsoluteLifetime, 20),
                Mission("command_survive_300", "HUMAN COMMAND", "SURVIVE 5 MINUTES",
                    MissionObjectiveType.SingleRunSurvivalTime, MissionProgressMode.BestSingleRun, 300),
                Mission("command_squad_3", "HUMAN COMMAND", "REACH SQUAD SIZE 3",
                    MissionObjectiveType.SquadSize, MissionProgressMode.AbsoluteLifetime, 3),
                Mission("fatigue_major_5", "SYSTEM FATIGUE", "TRIGGER 5 MAJOR GATES",
                    MissionObjectiveType.MajorGatesSelected, MissionProgressMode.DeltaSinceUnlock, 5),
                Mission("fatigue_35_loops", "SYSTEM FATIGUE", "COMPLETE 35 LOOPS",
                    MissionObjectiveType.TotalRunsCompleted, MissionProgressMode.AbsoluteLifetime, 35),
                Mission("fatigue_survive_360", "SYSTEM FATIGUE", "SURVIVE 6 MINUTES",
                    MissionObjectiveType.SingleRunSurvivalTime, MissionProgressMode.BestSingleRun, 360),
                Mission("fatigue_max_one_upgrade", "SYSTEM FATIGUE", "MAX ANY CORE UPGRADE",
                    MissionObjectiveType.MaxedCoreUpgrades, MissionProgressMode.AbsoluteLifetime, 1),
                Mission("break_max_all_upgrades", "BREAK THE CYCLE", "MAX ALL 5 CORE UPGRADES",
                    MissionObjectiveType.MaxedCoreUpgrades, MissionProgressMode.AbsoluteLifetime, 5),
                Mission("break_3000_total_kills", "BREAK THE CYCLE", "DEFEAT 3,000 ENEMIES TOTAL",
                    MissionObjectiveType.TotalEnemyKills, MissionProgressMode.AbsoluteLifetime, 3000),
                Mission("break_survive_420", "BREAK THE CYCLE", "SURVIVE 7 MINUTES",
                    MissionObjectiveType.SingleRunSurvivalTime, MissionProgressMode.BestSingleRun, 420),
                Mission("break_final_choice", "BREAK THE CYCLE", "MAKE THE FINAL CHOICE",
                    MissionObjectiveType.FinalChoiceResolved, MissionProgressMode.AbsoluteLifetime, 1)
            };
        }

        private void OnValidate()
        {
            ValidateValues();
        }

        private static MissionDefinition Mission(
            string id,
            string phase,
            string title,
            MissionObjectiveType objectiveType,
            MissionProgressMode progressMode,
            float targetValue,
            float objectiveParameterValue = 0f,
            PlayerMetaUpgradeType upgradeType = PlayerMetaUpgradeType.Damage)
        {
            return new MissionDefinition(
                id,
                phase,
                title,
                objectiveType,
                progressMode,
                targetValue,
                objectiveParameterValue,
                upgradeType);
        }
    }
}

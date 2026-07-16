using System;
using _Project.Scripts.Systems.ProgressionSystem;
using UnityEngine;

namespace _Project.Scripts.Systems.MissionSystem
{
    [Serializable]
    public sealed class MissionDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private string phase;
        [SerializeField] private string title;
        [SerializeField] private MissionObjectiveType objectiveType;
        [SerializeField] private MissionProgressMode progressMode;
        [SerializeField] private float targetValue;
        [SerializeField] private float objectiveParameterValue;
        [SerializeField] private PlayerMetaUpgradeType upgradeType;
        [SerializeField] private int rewardCoins;

        public MissionDefinition()
        {
        }

        public MissionDefinition(
            string id,
            string phase,
            string title,
            MissionObjectiveType objectiveType,
            MissionProgressMode progressMode,
            float targetValue,
            float objectiveParameterValue = 0f,
            PlayerMetaUpgradeType upgradeType = PlayerMetaUpgradeType.Damage,
            int rewardCoins = 1000)
        {
            this.id = id;
            this.phase = phase;
            this.title = title;
            this.objectiveType = objectiveType;
            this.progressMode = progressMode;
            this.targetValue = targetValue;
            this.objectiveParameterValue = objectiveParameterValue;
            this.upgradeType = upgradeType;
            this.rewardCoins = rewardCoins;
            Validate();
        }

        public string Id => id;
        public string Phase => phase;
        public string Title => title;
        public MissionObjectiveType ObjectiveType => objectiveType;
        public MissionProgressMode ProgressMode => progressMode;
        public float TargetValue => targetValue;
        public float ObjectiveParameterValue => objectiveParameterValue;
        public PlayerMetaUpgradeType UpgradeType => upgradeType;
        public int RewardCoins => rewardCoins;

        public void Validate()
        {
            id = string.IsNullOrWhiteSpace(id) ? "missing_mission_id" : id.Trim();
            phase = string.IsNullOrWhiteSpace(phase) ? "UNKNOWN" : phase.Trim();
            title = string.IsNullOrWhiteSpace(title) ? id : title.Trim();
            targetValue = Mathf.Max(0f, targetValue);
            objectiveParameterValue = Mathf.Max(0f, objectiveParameterValue);
            rewardCoins = Mathf.Max(0, rewardCoins);
        }
    }
}

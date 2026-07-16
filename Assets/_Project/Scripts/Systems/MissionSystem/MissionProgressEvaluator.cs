using System;
using UnityEngine;

namespace _Project.Scripts.Systems.MissionSystem
{
    public static class MissionProgressEvaluator
    {
        public static MissionProgressResult Evaluate(
            MissionDefinition definition,
            MissionProgressSnapshot snapshot,
            float storedProgressOrBaseline = 0f)
        {
            if (definition == null)
            {
                return new MissionProgressResult(0f, 0f);
            }

            float rawProgress = GetRawProgress(definition, snapshot);
            float progress = definition.ProgressMode switch
            {
                MissionProgressMode.AbsoluteLifetime => rawProgress,
                MissionProgressMode.DeltaSinceUnlock => Mathf.Max(0f, rawProgress - storedProgressOrBaseline),
                MissionProgressMode.BestSingleRun => Mathf.Max(storedProgressOrBaseline, rawProgress),
                _ => rawProgress
            };

            return new MissionProgressResult(progress, definition.TargetValue);
        }

        public static float CaptureBaseline(MissionDefinition definition, MissionProgressSnapshot snapshot)
        {
            if (definition == null)
            {
                return 0f;
            }

            return definition.ProgressMode == MissionProgressMode.DeltaSinceUnlock
                ? GetRawProgress(definition, snapshot)
                : 0f;
        }

        public static float GetRawProgress(MissionDefinition definition, MissionProgressSnapshot snapshot)
        {
            if (definition == null)
            {
                return 0f;
            }

            return definition.ObjectiveType switch
            {
                MissionObjectiveType.GameplayTutorialCompleted => snapshot.GameplayTutorialCompleted ? 1f : 0f,
                MissionObjectiveType.TotalRunsCompleted => snapshot.TotalRunsCompleted,
                MissionObjectiveType.AnyCoreUpgradePurchased => snapshot.CoreUpgradeLevelTotal,
                MissionObjectiveType.GatesSelected => snapshot.LifetimeGatesSelected,
                MissionObjectiveType.SingleRunEnemyKills => snapshot.CurrentRunEnemyKills,
                MissionObjectiveType.UpgradeLevel => snapshot.GetUpgradeLevel(definition.UpgradeType),
                MissionObjectiveType.SingleRunSurvivalTime => snapshot.CurrentRunSurvivalTime,
                MissionObjectiveType.CoreUpgradesAtLevel => snapshot.CountCoreUpgradesAtOrAboveLevel(
                    Mathf.RoundToInt(definition.ObjectiveParameterValue)),
                MissionObjectiveType.TotalEnemyKills => snapshot.TotalEnemyKills,
                MissionObjectiveType.SquadSize => snapshot.SquadSizeValue,
                MissionObjectiveType.MajorGatesSelected => snapshot.LifetimeMajorGatesSelected,
                MissionObjectiveType.MaxedCoreUpgrades => snapshot.CountMaxedCoreUpgrades(),
                MissionObjectiveType.FinalChoiceResolved => snapshot.FinalChoiceResolved ? 1f : 0f,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}

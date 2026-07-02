using _Project.Scripts.Systems.SaveSystem;
using UnityEngine;

namespace _Project.Cutscenes
{
    public readonly struct StoryCutsceneProgressContext
    {
        public StoryCutsceneProgressContext(
            int loopCount,
            float survivalSeconds,
            int runKills,
            int totalKills)
        {
            LoopCount = Mathf.Max(0, loopCount);
            SurvivalSeconds = Mathf.Max(0f, survivalSeconds);
            RunKills = Mathf.Max(0, runKills);
            TotalKills = Mathf.Max(0, totalKills);
        }

        public int LoopCount { get; }
        public float SurvivalSeconds { get; }
        public int RunKills { get; }
        public int TotalKills { get; }
    }

    public static class StoryCutsceneUnlockRules
    {
        public static readonly string[] FinalChoiceBranchIds =
        {
            StoryCutsceneIds.FinalChoiceContinueProtocol,
            StoryCutsceneIds.FinalChoiceShutDownCore
        };

        private static readonly Rule[] Rules =
        {
            new Rule(StoryCutsceneIds.BootSequence, null, 0, 0f, 0, 0),
            new Rule(StoryCutsceneIds.FirstDeathRecovery, StoryCutsceneIds.BootSequence, 1, 0f, 0, 0),
            new Rule(StoryCutsceneIds.EnemyDoesNotCharge, StoryCutsceneIds.FirstDeathRecovery, 3, 180f, 100, 0),
            new Rule(StoryCutsceneIds.GateMemoryLeak, StoryCutsceneIds.EnemyDoesNotCharge, 20, 240f, 0, 0),
            new Rule(StoryCutsceneIds.HumanCommand, StoryCutsceneIds.GateMemoryLeak, 35, 300f, 0, 1000),
            new Rule(StoryCutsceneIds.SystemFatigue, StoryCutsceneIds.HumanCommand, 50, 360f, 0, 0),
            new Rule(StoryCutsceneIds.FinalChoicePreChoice, StoryCutsceneIds.SystemFatigue, 70, 420f, 0, 0)
        };

        public static bool TryGetFirstEligible(
            SaveData saveData,
            StoryCutsceneProgressContext context,
            out string cutsceneId)
        {
            cutsceneId = string.Empty;

            if (saveData == null)
            {
                return false;
            }

            for (int index = 0; index < Rules.Length; index++)
            {
                Rule rule = Rules[index];
                if (!IsEligible(rule, saveData, context))
                {
                    continue;
                }

                cutsceneId = rule.CutsceneId;
                return true;
            }

            return false;
        }

        public static bool IsEligible(
            string cutsceneId,
            SaveData saveData,
            StoryCutsceneProgressContext context)
        {
            if (saveData == null || string.IsNullOrWhiteSpace(cutsceneId))
            {
                return false;
            }

            string playableId = NormalizePlayableCutsceneId(cutsceneId);
            for (int index = 0; index < Rules.Length; index++)
            {
                Rule rule = Rules[index];
                if (rule.CutsceneId == playableId)
                {
                    return IsEligible(rule, saveData, context);
                }
            }

            return false;
        }

        public static string NormalizePlayableCutsceneId(string cutsceneId)
        {
            return cutsceneId == StoryCutsceneIds.FinalChoice
                ? StoryCutsceneIds.FinalChoicePreChoice
                : cutsceneId;
        }

        private static bool IsEligible(
            Rule rule,
            SaveData saveData,
            StoryCutsceneProgressContext context)
        {
            if (saveData.HasSeenCutscene(rule.CutsceneId))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(rule.RequiredSeenCutsceneId)
                && !saveData.HasSeenCutscene(rule.RequiredSeenCutsceneId))
            {
                return false;
            }

            return context.LoopCount >= rule.MinLoopCount
                && context.SurvivalSeconds >= rule.MinSurvivalSeconds
                && context.RunKills >= rule.MinRunKills
                && context.TotalKills >= rule.MinTotalKills;
        }

        private readonly struct Rule
        {
            public Rule(
                string cutsceneId,
                string requiredSeenCutsceneId,
                int minLoopCount,
                float minSurvivalSeconds,
                int minRunKills,
                int minTotalKills)
            {
                CutsceneId = cutsceneId;
                RequiredSeenCutsceneId = requiredSeenCutsceneId;
                MinLoopCount = minLoopCount;
                MinSurvivalSeconds = minSurvivalSeconds;
                MinRunKills = minRunKills;
                MinTotalKills = minTotalKills;
            }

            public string CutsceneId { get; }
            public string RequiredSeenCutsceneId { get; }
            public int MinLoopCount { get; }
            public float MinSurvivalSeconds { get; }
            public int MinRunKills { get; }
            public int MinTotalKills { get; }
        }
    }
}

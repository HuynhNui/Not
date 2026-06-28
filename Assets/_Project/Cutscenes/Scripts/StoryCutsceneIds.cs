namespace _Project.Cutscenes
{
    public static class StoryCutsceneIds
    {
        public const string BootSequence = "CS_01_BootSequence";
        public const string FirstDeathRecovery = "CS_02_FirstDeathRecovery";
        public const string EnemyDoesNotCharge = "CS_03_EnemyDoesNotCharge";
        public const string GateMemoryLeak = "CS_04_GateMemoryLeak";
        public const string HumanCommand = "CS_05_HumanCommand";
        public const string SystemFatigue = "CS_06_SystemFatigue";
        public const string FinalChoice = "CS_07_FinalChoice";
        public const string FinalChoicePreChoice = "CS_07_FinalChoice_PreChoice";
        public const string FinalChoiceContinueProtocol = "CS_07_FinalChoice_ContinueProtocol";
        public const string FinalChoiceShutDownCore = "CS_07_FinalChoice_ShutDownCore";

        public static readonly string[] All =
        {
            BootSequence,
            FirstDeathRecovery,
            EnemyDoesNotCharge,
            GateMemoryLeak,
            HumanCommand,
            SystemFatigue,
            FinalChoice
        };
    }
}

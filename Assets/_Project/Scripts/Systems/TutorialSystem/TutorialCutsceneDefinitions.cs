using _Project.Cutscenes;

namespace _Project.Scripts.Systems.TutorialSystem
{
    public static class TutorialCutsceneDefinitions
    {
        public static StoryCutsceneDefinition MovementIntro => Create(
            "TUTORIAL_MOVEMENT_INTRO",
            new StoryDialogueLine("SYSTEM", "cold", "Movement calibration required."));

        public static StoryCutsceneDefinition MovementPractice => Create(
            "TUTORIAL_MOVEMENT_PRACTICE",
            new StoryDialogueLine("SYSTEM", "cold", "Drag left or right to reposition UNIT-07."));

        public static StoryCutsceneDefinition AutoFire => Create(
            "TUTORIAL_AUTO_FIRE",
            new StoryDialogueLine("SYSTEM", "cold", "Weapon system is automatic."),
            new StoryDialogueLine("SYSTEM", "cold", "Focus on survival."));

        public static StoryCutsceneDefinition EnemyWarning => Create(
            "TUTORIAL_ENEMY_WARNING",
            new StoryDialogueLine("SYSTEM", "warning", "Hostile signatures detected."),
            new StoryDialogueLine("SYSTEM", "warning", "Avoid direct contact."));

        public static StoryCutsceneDefinition RecruitGate => Create(
            "TUTORIAL_RECRUIT_GATE",
            new StoryDialogueLine("SYSTEM", "cold", "Recruit gate detected."),
            new StoryDialogueLine("SYSTEM", "cold", "Enter RECRUIT +1 to increase squad capacity."));

        public static StoryCutsceneDefinition DefaultGateChoice => Create(
            "TUTORIAL_DEFAULT_GATE_CHOICE",
            new StoryDialogueLine("SYSTEM", "cold", "Gate array detected."),
            new StoryDialogueLine("SYSTEM", "cold", "Choose one enhancement route."));

        public static StoryCutsceneDefinition GameplayComplete => Create(
            "TUTORIAL_GAMEPLAY_COMPLETE",
            new StoryDialogueLine("SYSTEM", "cold", "Calibration complete."),
            new StoryDialogueLine("SYSTEM", "cold", "Deployment authorized."));

        private static StoryCutsceneDefinition Create(string id, params StoryDialogueLine[] lines)
        {
            return new StoryCutsceneDefinition(id, lines);
        }
    }
}

using System;
using System.Collections.Generic;
using _Project.Cutscenes;

namespace _Project.Scripts.Systems.TutorialSystem
{
    public static class TutorialCutsceneDefinitions
    {
        private static readonly StoryCutsceneDefinition MovementIntroDefinition = Create(
            "TUTORIAL_MOVEMENT_INTRO",
            new StoryDialogueLine("SYSTEM", "cold", "Movement calibration required."));

        private static readonly StoryCutsceneDefinition MovementPracticeDefinition = Create(
            "TUTORIAL_MOVEMENT_PRACTICE",
            new StoryDialogueLine("SYSTEM", "cold", "Drag left or right to reposition UNIT-07."));

        private static readonly StoryCutsceneDefinition AutoFireDefinition = Create(
            "TUTORIAL_AUTO_FIRE",
            new StoryDialogueLine("SYSTEM", "cold", "Weapon system is automatic."),
            new StoryDialogueLine("SYSTEM", "cold", "Focus on survival."));

        private static readonly StoryCutsceneDefinition EnemyWarningDefinition = Create(
            "TUTORIAL_ENEMY_WARNING",
            new StoryDialogueLine("SYSTEM", "warning", "Hostile signatures detected."),
            new StoryDialogueLine("SYSTEM", "warning", "Avoid direct contact."));

        private static readonly StoryCutsceneDefinition RecruitGateDefinition = Create(
            "TUTORIAL_RECRUIT_GATE",
            new StoryDialogueLine("SYSTEM", "cold", "Recruit gate detected."),
            new StoryDialogueLine("SYSTEM", "cold", "Enter RECRUIT +1 to increase squad capacity."));

        private static readonly StoryCutsceneDefinition DefaultGateChoiceDefinition = Create(
            "TUTORIAL_DEFAULT_GATE_CHOICE",
            new StoryDialogueLine("SYSTEM", "cold", "Gate array detected."),
            new StoryDialogueLine("SYSTEM", "cold", "Choose one enhancement route."));

        private static readonly StoryCutsceneDefinition GameplayCompleteDefinition = Create(
            "TUTORIAL_GAMEPLAY_COMPLETE",
            new StoryDialogueLine("SYSTEM", "cold", "Calibration complete."),
            new StoryDialogueLine("SYSTEM", "cold", "Deployment authorized."));

        private static readonly StoryCutsceneDefinition UpdateOnboardingDefinition = Create(
            "TUTORIAL_UPDATE_ONBOARDING",
            new StoryDialogueLine("SYSTEM", "cold", "Combat shell destroyed."),
            new StoryDialogueLine("SYSTEM", "cold", "Core recovered."),
            new StoryDialogueLine("SYSTEM", "cold", "Combat data can reinforce the next shell."),
            new StoryDialogueLine("SYSTEM", "cold", "Open UPDATE."));

        private static readonly StoryCutsceneDefinition[] OrderedDefinitions =
        {
            MovementIntroDefinition,
            MovementPracticeDefinition,
            AutoFireDefinition,
            EnemyWarningDefinition,
            RecruitGateDefinition,
            DefaultGateChoiceDefinition,
            GameplayCompleteDefinition,
            UpdateOnboardingDefinition
        };

        private static readonly IReadOnlyList<StoryCutsceneDefinition> OrderedDefinitionsView =
            Array.AsReadOnly(OrderedDefinitions);

        public static StoryCutsceneDefinition MovementIntro => MovementIntroDefinition;
        public static StoryCutsceneDefinition MovementPractice => MovementPracticeDefinition;
        public static StoryCutsceneDefinition AutoFire => AutoFireDefinition;
        public static StoryCutsceneDefinition EnemyWarning => EnemyWarningDefinition;
        public static StoryCutsceneDefinition RecruitGate => RecruitGateDefinition;
        public static StoryCutsceneDefinition DefaultGateChoice => DefaultGateChoiceDefinition;
        public static StoryCutsceneDefinition GameplayComplete => GameplayCompleteDefinition;
        public static StoryCutsceneDefinition UpdateOnboarding => UpdateOnboardingDefinition;

        public static IReadOnlyList<StoryCutsceneDefinition> GetAll()
        {
            return OrderedDefinitionsView;
        }

        private static StoryCutsceneDefinition Create(string id, params StoryDialogueLine[] lines)
        {
            return new StoryCutsceneDefinition(id, lines);
        }
    }
}

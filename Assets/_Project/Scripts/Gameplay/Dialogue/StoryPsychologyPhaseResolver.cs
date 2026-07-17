using _Project.Cutscenes;
using _Project.Scripts.Systems.SaveSystem;

namespace _Project.Scripts.Gameplay.Dialogue
{
    public static class StoryPsychologyPhaseResolver
    {
        public static PsychologyPhase Resolve(SaveData saveData)
        {
            if (saveData == null)
            {
                return PsychologyPhase.Protocol;
            }

            if (saveData.HasSeenCutscene(StoryCutsceneIds.SystemFatigue)
                || saveData.HasSeenCutscene(StoryCutsceneIds.FinalChoicePreChoice)
                || saveData.HasSeenCutscene(StoryCutsceneIds.FinalChoiceContinueProtocol)
                || saveData.HasSeenCutscene(StoryCutsceneIds.FinalChoiceShutDownCore)
                || saveData.HasSeenCutscene(StoryCutsceneIds.FinalChoice))
            {
                return PsychologyPhase.Awakening;
            }

            if (saveData.HasSeenCutscene(StoryCutsceneIds.GateMemoryLeak)
                || saveData.HasSeenCutscene(StoryCutsceneIds.HumanCommand))
            {
                return PsychologyPhase.Doubt;
            }

            return PsychologyPhase.Protocol;
        }
    }
}

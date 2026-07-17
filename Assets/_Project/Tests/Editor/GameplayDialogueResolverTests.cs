using _Project.Cutscenes;
using _Project.Scripts.Gameplay.Dialogue;
using _Project.Scripts.Systems.SaveSystem;
using NUnit.Framework;

namespace _Project.Tests.Editor
{
    public sealed class GameplayDialogueResolverTests
    {
        [Test]
        public void Resolve_NullSave_ReturnsProtocol()
        {
            Assert.That(StoryPsychologyPhaseResolver.Resolve(null), Is.EqualTo(PsychologyPhase.Protocol));
        }

        [Test]
        public void Resolve_NoPhaseCutscene_ReturnsProtocol()
        {
            SaveData saveData = SaveData.CreateNew(1);
            saveData.MarkCutsceneSeen(StoryCutsceneIds.EnemyDoesNotCharge);

            Assert.That(StoryPsychologyPhaseResolver.Resolve(saveData), Is.EqualTo(PsychologyPhase.Protocol));
        }

        [TestCase(StoryCutsceneIds.GateMemoryLeak)]
        [TestCase(StoryCutsceneIds.HumanCommand)]
        public void Resolve_DoubtCutsceneSeen_ReturnsDoubt(string cutsceneId)
        {
            SaveData saveData = SaveData.CreateNew(1);
            saveData.MarkCutsceneSeen(cutsceneId);

            Assert.That(StoryPsychologyPhaseResolver.Resolve(saveData), Is.EqualTo(PsychologyPhase.Doubt));
        }

        [TestCase(StoryCutsceneIds.SystemFatigue)]
        [TestCase(StoryCutsceneIds.FinalChoicePreChoice)]
        [TestCase(StoryCutsceneIds.FinalChoiceContinueProtocol)]
        [TestCase(StoryCutsceneIds.FinalChoiceShutDownCore)]
        [TestCase(StoryCutsceneIds.FinalChoice)]
        public void Resolve_AwakeningCutsceneSeen_ReturnsAwakening(string cutsceneId)
        {
            SaveData saveData = SaveData.CreateNew(1);
            saveData.MarkCutsceneSeen(StoryCutsceneIds.GateMemoryLeak);
            saveData.MarkCutsceneSeen(cutsceneId);

            Assert.That(StoryPsychologyPhaseResolver.Resolve(saveData), Is.EqualTo(PsychologyPhase.Awakening));
        }
    }
}

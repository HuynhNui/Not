using _Project.Editor.Voice;
using _Project.Scripts.Gameplay.Dialogue;
using NUnit.Framework;

namespace _Project.Tests.Editor
{
    public sealed class GameplayVoiceEmotionResolverTests
    {
        [TestCase(PsychologyPhase.Protocol, "OPENING", "neutral")]
        [TestCase(PsychologyPhase.Protocol, "COMBAT", "alert")]
        [TestCase(PsychologyPhase.Protocol, "UPGRADE", "proud")]
        [TestCase(PsychologyPhase.Protocol, "LOOP", "neutral")]
        [TestCase(PsychologyPhase.Protocol, "BELIEF", "proud")]
        [TestCase(PsychologyPhase.Doubt, "MEMORY", "confused")]
        [TestCase(PsychologyPhase.Doubt, "OBSERVATION", "confused")]
        [TestCase(PsychologyPhase.Doubt, "DEFINITION", "hesitant")]
        [TestCase(PsychologyPhase.Doubt, "LOOP", "hesitant")]
        [TestCase(PsychologyPhase.Doubt, "SELF", "hesitant")]
        [TestCase(PsychologyPhase.Awakening, "AWARENESS", "tired")]
        [TestCase(PsychologyPhase.Awakening, "GUILT", "broken")]
        [TestCase(PsychologyPhase.Awakening, "FATIGUE", "tired")]
        [TestCase(PsychologyPhase.Awakening, "RESISTANCE", "defiant")]
        [TestCase(PsychologyPhase.Awakening, "FREEDOM", "calm")]
        public void Resolve_ReturnsConfiguredEmotion(PsychologyPhase phase, string tag, string expected)
        {
            Assert.That(GameplayVoiceEmotionResolver.Resolve(phase, tag), Is.EqualTo(expected));
        }

        [Test]
        public void Resolve_UnknownPhaseOrTag_ReturnsUnknown()
        {
            Assert.That(GameplayVoiceEmotionResolver.Resolve(PsychologyPhase.Protocol, "OTHER"), Is.EqualTo("unknown"));
            Assert.That(GameplayVoiceEmotionResolver.Resolve((PsychologyPhase)99, "OPENING"), Is.EqualTo("unknown"));
        }
    }
}

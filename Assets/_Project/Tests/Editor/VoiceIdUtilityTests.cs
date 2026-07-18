using _Project.Scripts.Systems.AudioSystem;
using NUnit.Framework;

namespace _Project.Tests.Editor
{
    public sealed class VoiceIdUtilityTests
    {
        [Test]
        public void CanonicalIds_MatchSchema()
        {
            Assert.That(VoiceIdUtility.ForStory("BOOT_SEQUENCE", 0), Is.EqualTo("VO_CS_BOOT_SEQUENCE_000"));
            Assert.That(VoiceIdUtility.ForTutorial("TUTORIAL_AUTO_FIRE", 1), Is.EqualTo("VO_TUT_TUTORIAL_AUTO_FIRE_001"));
            Assert.That(VoiceIdUtility.ForGameplay("P3_FREE_10"), Is.EqualTo("VO_GP_P3_FREE_10"));
        }

        [TestCase("boot sequence", "BOOT_SEQUENCE")]
        [TestCase("boot-sequence", "BOOT_SEQUENCE")]
        [TestCase("__boot___sequence__", "BOOT_SEQUENCE")]
        [TestCase("Boot Sequence", "BOOT_SEQUENCE")]
        [TestCase("boot.sequence!", "BOOTSEQUENCE")]
        [TestCase("", "")]
        [TestCase(null, "")]
        public void NormalizeIdSegment_AppliesCanonicalRules(string input, string expected)
        {
            Assert.That(VoiceIdUtility.NormalizeIdSegment(input), Is.EqualTo(expected));
        }
    }
}

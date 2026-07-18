using System.Linq;
using _Project.Editor.Voice;
using NUnit.Framework;

namespace _Project.Tests.Editor
{
    public sealed class VoiceManifestValidationTests
    {
        [Test]
        public void Validate_DuplicateVoiceIdAndOutputPathAreErrors()
        {
            VoiceManifestRecord first = CreateValidRecord("VO_TEST_001", "TEST_A", "A.wav");
            VoiceManifestRecord second = CreateValidRecord("VO_TEST_001", "TEST_B", "A.wav");

            VoiceManifestValidationResult result = VoiceManifestValidation.Validate(new[] { first, second });

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.Contains("Duplicate Voice ID")), Is.True);
            Assert.That(result.Errors.Any(error => error.Contains("Duplicate output path")), Is.True);
        }

        [Test]
        public void Validate_MissingSourceNullTextAndNegativeIndexAreErrors()
        {
            VoiceManifestRecord record = CreateValidRecord("VO_TEST_001", string.Empty, "A.wav");
            record.Text = null;
            record.LineIndex = -1;

            VoiceManifestValidationResult result = VoiceManifestValidation.Validate(new[] { record });

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.Contains("source ID is missing")), Is.True);
            Assert.That(result.Errors.Any(error => error.Contains("text is null")), Is.True);
            Assert.That(result.Errors.Any(error => error.Contains("line index cannot be negative")), Is.True);
        }

        [Test]
        public void Validate_DuplicateCandidateAndUnmappedEmotionAreWarningsOnly()
        {
            VoiceManifestRecord record = CreateValidRecord("VO_TEST_001", "TEST", "A.wav");
            record.Emotion = "unknown";
            record.PsychologyPhase = "PROTOCOL";
            record.Tag = "OTHER";
            record.DuplicateOf = "VO_TEST_000";
            record.Notes = "UNMAPPED_EMOTION";

            VoiceManifestValidationResult result = VoiceManifestValidation.Validate(new[] { record });

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Warnings.Any(warning => warning.Contains("unmapped gameplay emotion")), Is.True);
            Assert.That(result.Warnings.Any(warning => warning.Contains("duplicate candidate")), Is.True);
        }

        private static VoiceManifestRecord CreateValidRecord(string voiceId, string sourceId, string fileName)
        {
            return new VoiceManifestRecord
            {
                VoiceId = voiceId,
                SourceType = "STORY",
                SourceId = sourceId,
                LineIndex = 0,
                Speaker = "SYSTEM",
                Emotion = "cold",
                PsychologyPhase = string.Empty,
                Tag = string.Empty,
                Text = "Test line.",
                OutputPath = "Assets/_Project/Audio/Voice/System/" + fileName,
                Status = "PENDING",
                DuplicateOf = string.Empty,
                Notes = string.Empty
            };
        }
    }
}

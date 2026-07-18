using System;
using System.IO;
using System.Linq;
using _Project.Cutscenes;
using _Project.Editor.Voice;
using _Project.Scripts.Gameplay.Dialogue;
using _Project.Scripts.Systems.TutorialSystem;
using NUnit.Framework;

namespace _Project.Tests.Editor
{
    public sealed class VoiceManifestCollectorTests
    {
        [Test]
        public void CollectAll_ContainsEverySourceRecordInSourceOrder()
        {
            var records = VoiceManifestCollector.CollectAll();
            var gameplayEntries = GameplayDialogueCsvParser.Parse(
                File.ReadAllText(VoiceManifestCollector.GameplayCsvPath),
                VoiceManifestCollector.GameplayCsvPath);
            int storyCount = StoryCutsceneLibrary.GetAll().Sum(definition => definition.Lines.Count);
            int tutorialCount = TutorialCutsceneDefinitions.GetAll().Sum(definition => definition.Lines.Count);

            Assert.That(records, Has.Count.EqualTo(storyCount + tutorialCount + gameplayEntries.Count));
            Assert.That(records.Take(storyCount).All(record => record.SourceType == "STORY"), Is.True);
            Assert.That(records.Skip(storyCount).Take(tutorialCount).All(record => record.SourceType == "TUTORIAL"), Is.True);
            Assert.That(records.Skip(storyCount + tutorialCount).All(record => record.SourceType == "GAMEPLAY"), Is.True);
            CollectionAssert.AreEqual(
                gameplayEntries.Select(entry => entry.DialogueId),
                records.Where(record => record.SourceType == "GAMEPLAY").Select(record => record.SourceId));
        }

        [Test]
        public void CollectAll_PreservesDefinitionDataAndExportsUpdateOnboarding()
        {
            var records = VoiceManifestCollector.CollectAll();
            StoryCutsceneDefinition firstDefinition = StoryCutsceneLibrary.GetAll()[0];
            StoryDialogueLine firstLine = firstDefinition.Lines[0];
            VoiceManifestRecord firstRecord = records[0];

            Assert.That(firstRecord.SourceId, Is.EqualTo(firstDefinition.CutsceneId));
            Assert.That(firstRecord.LineIndex, Is.Zero);
            Assert.That(firstRecord.Speaker, Is.EqualTo(firstLine.Speaker));
            Assert.That(firstRecord.Emotion, Is.EqualTo(firstLine.Emotion));
            Assert.That(firstRecord.Text, Is.EqualTo(firstLine.Text));
            Assert.That(
                records.Any(record => record.SourceId == TutorialCutsceneDefinitions.UpdateOnboarding.CutsceneId),
                Is.True);
        }

        [Test]
        public void CollectAll_AssignsStableIdsPathsSkipStatusAndDuplicates()
        {
            var records = VoiceManifestCollector.CollectAll();

            Assert.That(records.Select(record => record.VoiceId).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(records.Count));
            Assert.That(records.Select(record => record.OutputPath).Distinct(StringComparer.OrdinalIgnoreCase).Count(), Is.EqualTo(records.Count));
            Assert.That(records.Where(record => record.Text.Trim() == "...").All(record => record.Status == "SKIP"), Is.True);
            Assert.That(records.Where(record => record.Text.Trim() != "...").All(record => record.Status == "PENDING"), Is.True);
            Assert.That(records.Single(record => record.VoiceId == "VO_CS_CS_01_BOOTSEQUENCE_000").OutputPath,
                Does.StartWith("Assets/_Project/Audio/Voice/System/"));
            Assert.That(records.Where(record => !string.IsNullOrEmpty(record.DuplicateOf)).All(record =>
                records.Any(candidate => candidate.VoiceId == record.DuplicateOf)), Is.True);
            Assert.That(VoiceManifestValidation.Validate(records).IsValid, Is.True);
        }

        [Test]
        public void CreateGameplayRecord_UnmappedTagAddsWarningNote()
        {
            var entry = new GameplayDialogueEntry("UNKNOWN_01", PsychologyPhase.Protocol, "OTHER", "Unknown tag.");

            VoiceManifestRecord record = VoiceManifestCollector.CreateGameplayRecord(entry);

            Assert.That(record.Emotion, Is.EqualTo("unknown"));
            Assert.That(record.Notes.Split(';'), Does.Contain("UNMAPPED_EMOTION"));
        }

        [Test]
        public void DefinitionLineIndexes_StartAtZeroAndRemainContiguous()
        {
            var records = VoiceManifestCollector.CollectAll();
            var indexedGroups = records
                .Where(record => record.SourceType == "STORY" || record.SourceType == "TUTORIAL")
                .GroupBy(record => new { record.SourceType, record.SourceId });

            foreach (var group in indexedGroups)
            {
                CollectionAssert.AreEqual(Enumerable.Range(0, group.Count()), group.Select(record => record.LineIndex));
            }
        }
    }
}

using System.IO;
using System.Linq;
using _Project.Scripts.Gameplay.Dialogue;
using NUnit.Framework;

namespace _Project.Tests.Editor
{
    public sealed class GameplayDialogueContentTests
    {
        private const string CsvPath = "Assets/_Project/Data/Dialogue/GameplayDialogueContent_v0.1.csv";

        [Test]
        public void Parser_SupportsBomQuotedCommaAndEscapedQuote()
        {
            string csv = "\uFEFFDialogueId,PsychologyPhase,Tag,Text\r\n"
                + "TEST_01,PROTOCOL,OPENING,\"Unit says, \"\"Proceed\"\".\"\r\n";

            var entries = GameplayDialogueCsvParser.Parse(csv, "inline");

            Assert.That(entries, Has.Count.EqualTo(1));
            Assert.That(entries[0].DialogueId, Is.EqualTo("TEST_01"));
            Assert.That(entries[0].PsychologyPhase, Is.EqualTo(PsychologyPhase.Protocol));
            Assert.That(entries[0].Text, Is.EqualTo("Unit says, \"Proceed\"."));
        }

        [Test]
        public void SuppliedCsv_HasExpectedPhaseCountsAndUniqueIds()
        {
            string csv = File.ReadAllText(CsvPath);

            var entries = GameplayDialogueCsvParser.Parse(csv, CsvPath);

            Assert.That(entries, Has.Count.EqualTo(150));
            Assert.That(entries.Select(entry => entry.DialogueId).Distinct().Count(), Is.EqualTo(150));
            Assert.That(entries.Count(entry => entry.PsychologyPhase == PsychologyPhase.Protocol), Is.EqualTo(50));
            Assert.That(entries.Count(entry => entry.PsychologyPhase == PsychologyPhase.Doubt), Is.EqualTo(50));
            Assert.That(entries.Count(entry => entry.PsychologyPhase == PsychologyPhase.Awakening), Is.EqualTo(50));
            Assert.That(entries.Count(entry => entry.PsychologyPhase == PsychologyPhase.Protocol && entry.IsOpening), Is.EqualTo(10));
            Assert.That(entries.Any(entry => string.IsNullOrWhiteSpace(entry.Text)), Is.False);
        }
    }
}

using System;
using System.Collections.Generic;
using _Project.Scripts.Gameplay.Dialogue;
using NUnit.Framework;

namespace _Project.Tests.Editor
{
    public sealed class DialogueShuffleBagTests
    {
        [Test]
        public void Next_UsesEveryEntryBeforeRepeating()
        {
            List<GameplayDialogueEntry> pool = CreatePool(3);
            DialogueShuffleBag bag = new DialogueShuffleBag(pool, new Random(7));

            var seen = new HashSet<string>();
            for (int index = 0; index < pool.Count; index++)
            {
                seen.Add(bag.Next().DialogueId);
            }

            Assert.That(seen, Has.Count.EqualTo(pool.Count));
        }

        [Test]
        public void Reshuffle_DoesNotStartWithLastDialogueIdWhenPossible()
        {
            List<GameplayDialogueEntry> pool = CreatePool(4);
            DialogueShuffleBag bag = new DialogueShuffleBag(pool, new Random(3), lastDialogueId: "ID_02");

            GameplayDialogueEntry first = bag.Next();

            Assert.That(first.DialogueId, Is.Not.EqualTo("ID_02"));
        }

        private static List<GameplayDialogueEntry> CreatePool(int count)
        {
            var entries = new List<GameplayDialogueEntry>();
            for (int index = 0; index < count; index++)
            {
                entries.Add(new GameplayDialogueEntry(
                    $"ID_{index:00}",
                    PsychologyPhase.Protocol,
                    string.Empty,
                    $"Line {index}"));
            }

            return entries;
        }
    }
}

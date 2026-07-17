using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Dialogue
{
    [CreateAssetMenu(
        fileName = "GameplayDialogueCatalog",
        menuName = "True Gate/Gameplay Dialogue Catalog")]
    public sealed class GameplayDialogueCatalog : ScriptableObject
    {
        [SerializeField] private TextAsset sourceCsv;
        [SerializeField] private List<GameplayDialogueEntry> entries = new List<GameplayDialogueEntry>();

        public TextAsset SourceCsv => sourceCsv;
        public IReadOnlyList<GameplayDialogueEntry> Entries => entries;

        public List<GameplayDialogueEntry> CreateOpeningPool(PsychologyPhase phase)
        {
            List<GameplayDialogueEntry> openingEntries = Filter(phase, entry => entry.IsOpening);
            return openingEntries.Count > 0
                ? openingEntries
                : Filter(phase, _ => true);
        }

        public List<GameplayDialogueEntry> CreatePeriodicPool(PsychologyPhase phase, out bool usedFallback)
        {
            usedFallback = false;
            if (phase != PsychologyPhase.Protocol)
            {
                return Filter(phase, _ => true);
            }

            List<GameplayDialogueEntry> nonOpeningEntries = Filter(phase, entry => !entry.IsOpening);
            if (nonOpeningEntries.Count > 0)
            {
                return nonOpeningEntries;
            }

            usedFallback = true;
            return Filter(phase, _ => true);
        }

        public int CountByPhase(PsychologyPhase phase)
        {
            int count = 0;
            for (int index = 0; index < entries.Count; index++)
            {
                GameplayDialogueEntry entry = entries[index];
                if (entry != null && entry.PsychologyPhase == phase)
                {
                    count++;
                }
            }

            return count;
        }

#if UNITY_EDITOR
        public void ReplaceEntries(TextAsset source, List<GameplayDialogueEntry> replacementEntries)
        {
            sourceCsv = source;
            entries = replacementEntries != null
                ? new List<GameplayDialogueEntry>(replacementEntries)
                : new List<GameplayDialogueEntry>();
        }
#endif

        private List<GameplayDialogueEntry> Filter(
            PsychologyPhase phase,
            Predicate<GameplayDialogueEntry> predicate)
        {
            var pool = new List<GameplayDialogueEntry>();
            for (int index = 0; index < entries.Count; index++)
            {
                GameplayDialogueEntry entry = entries[index];
                if (entry == null || entry.PsychologyPhase != phase || !predicate(entry))
                {
                    continue;
                }

                pool.Add(entry);
            }

            return pool;
        }
    }
}

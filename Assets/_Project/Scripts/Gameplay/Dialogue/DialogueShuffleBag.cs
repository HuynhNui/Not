using System;
using System.Collections.Generic;

namespace _Project.Scripts.Gameplay.Dialogue
{
    public sealed class DialogueShuffleBag
    {
        private readonly List<GameplayDialogueEntry> _sourcePool = new List<GameplayDialogueEntry>();
        private readonly List<GameplayDialogueEntry> _bag = new List<GameplayDialogueEntry>();
        private readonly Random _random;
        private int _nextIndex;
        private string _lastDialogueId;

        public DialogueShuffleBag(
            IReadOnlyList<GameplayDialogueEntry> pool,
            Random random = null,
            string lastDialogueId = null)
        {
            if (pool != null)
            {
                for (int index = 0; index < pool.Count; index++)
                {
                    if (pool[index] != null)
                    {
                        _sourcePool.Add(pool[index]);
                    }
                }
            }

            _random = random ?? new Random(Guid.NewGuid().GetHashCode());
            _lastDialogueId = lastDialogueId;
            Reshuffle();
        }

        public int Count => _sourcePool.Count;
        public string LastDialogueId => _lastDialogueId;

        public GameplayDialogueEntry Next()
        {
            if (_bag.Count == 0)
            {
                return null;
            }

            if (_nextIndex >= _bag.Count)
            {
                Reshuffle();
            }

            GameplayDialogueEntry entry = _bag[_nextIndex];
            _nextIndex++;
            _lastDialogueId = entry.DialogueId;
            return entry;
        }

        private void Reshuffle()
        {
            _bag.Clear();
            _bag.AddRange(_sourcePool);
            _nextIndex = 0;

            for (int index = _bag.Count - 1; index > 0; index--)
            {
                int swapIndex = _random.Next(index + 1);
                (_bag[index], _bag[swapIndex]) = (_bag[swapIndex], _bag[index]);
            }

            if (_bag.Count <= 1 || string.IsNullOrEmpty(_lastDialogueId))
            {
                return;
            }

            if (string.Equals(_bag[0].DialogueId, _lastDialogueId, StringComparison.Ordinal))
            {
                (_bag[0], _bag[1]) = (_bag[1], _bag[0]);
            }
        }
    }
}

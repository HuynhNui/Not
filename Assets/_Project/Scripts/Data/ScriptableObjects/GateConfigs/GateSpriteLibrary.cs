using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Data.ScriptableObjects.GateConfigs
{
    [CreateAssetMenu(fileName = "GateSpriteLibrary", menuName = "Chibi Pixel Gate/Data/Gate Sprite Library")]
    public sealed class GateSpriteLibrary : ScriptableObject
    {
        [SerializeField] private List<Entry> entries = new List<Entry>();

        private Dictionary<string, Sprite> _spritesByGateId;

        public IReadOnlyList<Entry> Entries => entries;

        public bool TryGetSprite(string gateId, out Sprite sprite)
        {
            EnsureLookup();
            return _spritesByGateId.TryGetValue(NormalizeGateId(gateId), out sprite) && sprite != null;
        }

        public void SetEntries(IEnumerable<Entry> newEntries)
        {
            entries ??= new List<Entry>();
            entries.Clear();

            if (newEntries == null)
            {
                _spritesByGateId = null;
                return;
            }

            foreach (Entry entry in newEntries)
            {
                if (entry == null)
                {
                    continue;
                }

                entries.Add(entry);
            }

            _spritesByGateId = null;
        }

        private void OnEnable()
        {
            _spritesByGateId = null;
        }

        private void OnValidate()
        {
            _spritesByGateId = null;
        }

        private void EnsureLookup()
        {
            if (_spritesByGateId != null)
            {
                return;
            }

            _spritesByGateId = new Dictionary<string, Sprite>();
            if (entries == null)
            {
                return;
            }

            foreach (Entry entry in entries)
            {
                if (entry == null || entry.Sprite == null)
                {
                    continue;
                }

                string key = NormalizeGateId(entry.GateId);
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                _spritesByGateId[key] = entry.Sprite;
            }
        }

        private static string NormalizeGateId(string gateId)
        {
            return string.IsNullOrWhiteSpace(gateId)
                ? string.Empty
                : gateId.Trim().ToLowerInvariant();
        }

        [Serializable]
        public sealed class Entry
        {
            [SerializeField] private string gateId;
            [SerializeField] private Sprite sprite;

            public Entry(string gateId, Sprite sprite)
            {
                this.gateId = gateId;
                this.sprite = sprite;
            }

            public string GateId => gateId;
            public Sprite Sprite => sprite;
        }
    }
}

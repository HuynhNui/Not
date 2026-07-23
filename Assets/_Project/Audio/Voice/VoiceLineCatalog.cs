using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Systems.AudioSystem
{
    [Serializable]
    public sealed class VoiceLineCatalogEntry
    {
        [SerializeField] private string voiceId;
        [SerializeField] private string sourceType;
        [SerializeField] private string sourceId;
        [SerializeField] private int lineIndex;
        [SerializeField] private string speaker;
        [SerializeField] private string emotion;
        [SerializeField] private AudioClip clip;

        public VoiceLineCatalogEntry(
            string voiceId,
            string sourceType,
            string sourceId,
            int lineIndex,
            string speaker,
            string emotion,
            AudioClip clip)
        {
            this.voiceId = voiceId;
            this.sourceType = sourceType;
            this.sourceId = sourceId;
            this.lineIndex = lineIndex;
            this.speaker = speaker;
            this.emotion = emotion;
            this.clip = clip;
        }

        public string VoiceId => voiceId;
        public string SourceType => sourceType;
        public string SourceId => sourceId;
        public int LineIndex => lineIndex;
        public string Speaker => speaker;
        public string Emotion => emotion;
        public AudioClip Clip => clip;
    }

    [Serializable]
    public sealed class VoiceLineCatalogSkip
    {
        [SerializeField] private string sourceId;
        [SerializeField] private int lineIndex;

        public VoiceLineCatalogSkip(string sourceId, int lineIndex)
        {
            this.sourceId = sourceId;
            this.lineIndex = lineIndex;
        }

        public string SourceId => sourceId;
        public int LineIndex => lineIndex;
    }

    [CreateAssetMenu(fileName = "VoiceLineCatalog", menuName = "True Gate/Audio/Voice Line Catalog")]
    public sealed class VoiceLineCatalog : ScriptableObject
    {
        [SerializeField] private List<VoiceLineCatalogEntry> entries = new List<VoiceLineCatalogEntry>();
        [SerializeField] private List<VoiceLineCatalogSkip> skippedLines = new List<VoiceLineCatalogSkip>();

        private Dictionary<LineKey, VoiceLineCatalogEntry> _byLine;
        private Dictionary<string, VoiceLineCatalogEntry> _byVoiceId;
        private HashSet<LineKey> _skipped;
        private bool _lookupBuildAttempted;

        public IReadOnlyList<VoiceLineCatalogEntry> Entries => entries;
        public IReadOnlyList<VoiceLineCatalogSkip> SkippedLines => skippedLines;

        private void OnEnable()
        {
            TryRebuildLookup(out _);
        }

        private void OnValidate()
        {
            InvalidateLookup();
        }

        public bool TryGet(string sourceId, int lineIndex, out VoiceLineCatalogEntry entry)
        {
            EnsureLookup();
            if (_byLine != null)
            {
                return _byLine.TryGetValue(new LineKey(sourceId, lineIndex), out entry);
            }

            entry = null;
            return false;
        }

        public bool TryGetByVoiceId(string voiceId, out VoiceLineCatalogEntry entry)
        {
            EnsureLookup();
            if (_byVoiceId != null && !string.IsNullOrEmpty(voiceId))
            {
                return _byVoiceId.TryGetValue(voiceId, out entry);
            }

            entry = null;
            return false;
        }

        public bool IsSkipped(string sourceId, int lineIndex)
        {
            EnsureLookup();
            return _skipped != null && _skipped.Contains(new LineKey(sourceId, lineIndex));
        }

        public void SetEntries(
            IEnumerable<VoiceLineCatalogEntry> newEntries,
            IEnumerable<VoiceLineCatalogSkip> newSkippedLines = null)
        {
            entries = newEntries != null
                ? new List<VoiceLineCatalogEntry>(newEntries)
                : new List<VoiceLineCatalogEntry>();
            skippedLines = newSkippedLines != null
                ? new List<VoiceLineCatalogSkip>(newSkippedLines)
                : new List<VoiceLineCatalogSkip>();
            InvalidateLookup();
        }

        public bool TryRebuildLookup(out string error)
        {
            var lineLookup = new Dictionary<LineKey, VoiceLineCatalogEntry>();
            var voiceIdLookup = new Dictionary<string, VoiceLineCatalogEntry>(StringComparer.Ordinal);
            var skipLookup = new HashSet<LineKey>();

            for (int index = 0; index < entries.Count; index++)
            {
                VoiceLineCatalogEntry entry = entries[index];
                if (entry == null)
                {
                    return RejectLookup($"Voice catalog entry {index} is null.", out error);
                }

                var key = new LineKey(entry.SourceId, entry.LineIndex);
                if (!lineLookup.TryAdd(key, entry))
                {
                    return RejectLookup(
                        $"Duplicate voice line key '{entry.SourceId}' line {entry.LineIndex}.",
                        out error);
                }

                if (string.IsNullOrEmpty(entry.VoiceId) || !voiceIdLookup.TryAdd(entry.VoiceId, entry))
                {
                    return RejectLookup($"Duplicate or empty VoiceId '{entry.VoiceId}'.", out error);
                }
            }

            for (int index = 0; index < skippedLines.Count; index++)
            {
                VoiceLineCatalogSkip skippedLine = skippedLines[index];
                if (skippedLine == null)
                {
                    return RejectLookup($"Voice catalog SKIP entry {index} is null.", out error);
                }

                var key = new LineKey(skippedLine.SourceId, skippedLine.LineIndex);
                if (lineLookup.ContainsKey(key) || !skipLookup.Add(key))
                {
                    return RejectLookup(
                        $"Duplicate voice line/SKIP key '{skippedLine.SourceId}' line {skippedLine.LineIndex}.",
                        out error);
                }
            }

            _byLine = lineLookup;
            _byVoiceId = voiceIdLookup;
            _skipped = skipLookup;
            _lookupBuildAttempted = true;
            error = string.Empty;
            return true;
        }

        private void EnsureLookup()
        {
            if (_lookupBuildAttempted)
            {
                return;
            }

            if (!TryRebuildLookup(out string error))
            {
                Debug.LogError(error, this);
            }
        }

        private bool RejectLookup(string message, out string error)
        {
            _byLine = null;
            _byVoiceId = null;
            _skipped = null;
            _lookupBuildAttempted = true;
            error = message;
            return false;
        }

        private void InvalidateLookup()
        {
            _byLine = null;
            _byVoiceId = null;
            _skipped = null;
            _lookupBuildAttempted = false;
        }

        private readonly struct LineKey : IEquatable<LineKey>
        {
            private readonly string _sourceId;
            private readonly int _lineIndex;

            public LineKey(string sourceId, int lineIndex)
            {
                _sourceId = sourceId ?? string.Empty;
                _lineIndex = lineIndex;
            }

            public bool Equals(LineKey other)
            {
                return _lineIndex == other._lineIndex
                    && string.Equals(_sourceId, other._sourceId, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is LineKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (StringComparer.Ordinal.GetHashCode(_sourceId) * 397) ^ _lineIndex;
                }
            }
        }
    }
}

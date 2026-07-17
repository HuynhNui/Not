using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace _Project.Scripts.Systems.AudioSystem
{
    [CreateAssetMenu(menuName = "True Gate/Audio/Audio Catalog")]
    public sealed class AudioCatalog : ScriptableObject
    {
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private AudioCueEntry[] cues = Array.Empty<AudioCueEntry>();

        private Dictionary<AudioCueId, AudioCueEntry> _entriesById;

        public AudioMixer Mixer => audioMixer;
        public IReadOnlyList<AudioCueEntry> Cues => cues;

        public bool TryGet(AudioCueId cueId, out AudioCueEntry entry)
        {
            EnsureLookup();
            return _entriesById.TryGetValue(cueId, out entry) && entry != null;
        }

        public int CountAssignedClips()
        {
            int total = 0;
            for (int index = 0; index < cues.Length; index++)
            {
                AudioCueEntry cue = cues[index];
                if (cue?.Clips == null)
                {
                    continue;
                }

                for (int clipIndex = 0; clipIndex < cue.Clips.Length; clipIndex++)
                {
                    if (cue.Clips[clipIndex] != null)
                    {
                        total++;
                    }
                }
            }

            return total;
        }

        private void OnEnable()
        {
            _entriesById = null;
        }

        private void OnValidate()
        {
            _entriesById = null;
        }

        private void EnsureLookup()
        {
            if (_entriesById != null)
            {
                return;
            }

            _entriesById = new Dictionary<AudioCueId, AudioCueEntry>(cues.Length);
            for (int index = 0; index < cues.Length; index++)
            {
                AudioCueEntry cue = cues[index];
                if (cue == null || cue.Id == AudioCueId.None)
                {
                    continue;
                }

                _entriesById[cue.Id] = cue;
            }
        }
    }

    [Serializable]
    public sealed class AudioCueEntry
    {
        [SerializeField] private AudioCueId id;
        [SerializeField] private AudioCueCategory category;
        [SerializeField] private AudioClip[] clips = Array.Empty<AudioClip>();
        [SerializeField] private AudioMixerGroup mixerGroup;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private Vector2 pitchRange = Vector2.one;
        [SerializeField, Min(0f)] private float minimumInterval;
        [SerializeField, Min(1)] private int maximumSimultaneousVoices = 1;
        [SerializeField] private bool loop;
        [SerializeField] private int priority;

        public AudioCueId Id => id;
        public AudioCueCategory Category => category;
        public AudioClip[] Clips => clips;
        public AudioMixerGroup MixerGroup => mixerGroup;
        public float Volume => Mathf.Clamp01(volume);
        public Vector2 PitchRange => pitchRange;
        public float MinimumInterval => Mathf.Max(0f, minimumInterval);
        public int MaximumSimultaneousVoices => Mathf.Max(1, maximumSimultaneousVoices);
        public bool Loop => loop;
        public int Priority => priority;

        public bool HasClips => clips != null && clips.Length > 0 && clips[0] != null;

        public AudioClip GetClip(int index)
        {
            if (clips == null || clips.Length == 0)
            {
                return null;
            }

            return clips[Mathf.Clamp(index, 0, clips.Length - 1)];
        }
    }
}

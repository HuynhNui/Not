using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace _Project.Scripts.Systems.AudioSystem
{
    public sealed class AudioSystem : MonoBehaviour
    {
        public const string MusicEnabledPrefsKey = "Settings.MusicEnabled";
        public const string SfxEnabledPrefsKey = "Settings.SfxEnabled";
        public const string MusicVolumeParameter = "MusicVolumeDb";
        public const string SfxVolumeParameter = "SfxVolumeDb";
        public const string AmbienceVolumeParameter = "AmbienceVolumeDb";
        public const string UiVolumeParameter = "UiVolumeDb";
        public const string DialogueVolumeParameter = "DialogueVolumeDb";
        private const float EnabledDb = 0f;
        private const float DisabledDb = -80f;
        private const int DefaultSfxSourceCount = 10;

        [SerializeField] private AudioCatalog catalog;
        [SerializeField] private int sfxSourceCount = DefaultSfxSourceCount;
        [SerializeField] private bool playMenuMusicOnStart = true;

        private AudioSource[] _musicSources;
        private AudioSource _ambienceSource;
        private AudioSource _uiSource;
        private AudioSource _dialogueSource;
        private AudioSource[] _sfxSources;
        private VoiceState[] _voiceStates;
        private float[] _lastPlayTimes;
        private int[] _lastVariantIndices;
        private AudioCueId _currentMusicCue;
        private int _activeMusicSourceIndex;
        private Coroutine _musicFadeCoroutine;
        private bool _initialized;

        public AudioCatalog Catalog => catalog;
        public AudioCueId CurrentMusicCue => _currentMusicCue;
        public int LastRejectedCueCount { get; private set; }
        public int LastAcceptedCueCount { get; private set; }

        private void Awake()
        {
            Initialize();
            ApplySavedSettings();
        }

        private void Start()
        {
            if (playMenuMusicOnStart)
            {
                PlayMusic(AudioCueId.BgmMainMenu, 0f);
            }
        }

        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            int cueCapacity = GetCueCapacity();
            _lastPlayTimes = new float[cueCapacity];
            _lastVariantIndices = new int[cueCapacity];
            for (int index = 0; index < _lastPlayTimes.Length; index++)
            {
                _lastPlayTimes[index] = -999f;
                _lastVariantIndices[index] = -1;
            }

            _musicSources = new[]
            {
                CreateSource("MusicSourceA"),
                CreateSource("MusicSourceB")
            };
            _ambienceSource = CreateSource("AmbienceSource");
            _uiSource = CreateSource("UiSource");
            _dialogueSource = CreateSource("DialogueSource");

            int sourceCount = Mathf.Clamp(sfxSourceCount, 1, 16);
            _sfxSources = new AudioSource[sourceCount];
            _voiceStates = new VoiceState[sourceCount];
            for (int index = 0; index < sourceCount; index++)
            {
                _sfxSources[index] = CreateSource($"SfxSource{index:00}");
                _voiceStates[index] = new VoiceState();
            }
        }

        public void PlayMusic(AudioCueId cue, float fadeSeconds = 1f)
        {
            Initialize();
            if (_currentMusicCue == cue && _musicSources[_activeMusicSourceIndex].isPlaying)
            {
                return;
            }

            if (!TryGetEntry(cue, AudioCueCategory.Music, out AudioCueEntry entry))
            {
                return;
            }

            AudioClip clip = entry.GetClip(0);
            if (clip == null)
            {
                return;
            }

            if (_musicFadeCoroutine != null)
            {
                StopCoroutine(_musicFadeCoroutine);
            }

            int nextIndex = 1 - _activeMusicSourceIndex;
            AudioSource next = _musicSources[nextIndex];
            ConfigureSource(next, entry);
            next.clip = clip;
            next.loop = true;
            next.volume = fadeSeconds <= 0f ? entry.Volume : 0f;
            next.Play();

            AudioSource previous = _musicSources[_activeMusicSourceIndex];
            _currentMusicCue = cue;
            _activeMusicSourceIndex = nextIndex;

            if (fadeSeconds <= 0f)
            {
                previous.Stop();
                previous.volume = 0f;
                return;
            }

            _musicFadeCoroutine = StartCoroutine(Crossfade(previous, next, entry.Volume, fadeSeconds));
        }

        public void StopMusic(float fadeSeconds = 1f)
        {
            Initialize();
            _currentMusicCue = AudioCueId.None;

            if (_musicFadeCoroutine != null)
            {
                StopCoroutine(_musicFadeCoroutine);
            }

            if (fadeSeconds <= 0f)
            {
                StopSource(_musicSources[0]);
                StopSource(_musicSources[1]);
                return;
            }

            _musicFadeCoroutine = StartCoroutine(FadeOutMusic(fadeSeconds));
        }

        public void PlayAmbience(AudioCueId cue)
        {
            Initialize();
            if (!TryGetEntry(cue, AudioCueCategory.Ambience, out AudioCueEntry entry))
            {
                return;
            }

            AudioClip clip = entry.GetClip(0);
            if (clip == null || _ambienceSource.clip == clip && _ambienceSource.isPlaying)
            {
                return;
            }

            ConfigureSource(_ambienceSource, entry);
            _ambienceSource.clip = clip;
            _ambienceSource.loop = true;
            _ambienceSource.volume = entry.Volume;
            _ambienceSource.Play();
        }

        public void StopAmbience()
        {
            Initialize();
            StopSource(_ambienceSource);
        }

        public bool PlaySfx(AudioCueId cue)
        {
            return PlayCue(cue, AudioCueCategory.Sfx);
        }

        public bool PlayUi(AudioCueId cue)
        {
            return PlaySingleSource(cue, AudioCueCategory.Ui, _uiSource, interrupt: true);
        }

        public bool PlayDialogue(AudioCueId cue)
        {
            return PlaySingleSource(cue, AudioCueCategory.Dialogue, _dialogueSource, interrupt: true);
        }

        public void ApplySavedSettings()
        {
            SetMusicEnabled(PlayerPrefs.GetInt(MusicEnabledPrefsKey, 1) != 0);
            SetSfxEnabled(PlayerPrefs.GetInt(SfxEnabledPrefsKey, 1) != 0);
        }

        public void SetMusicEnabled(bool enabled)
        {
            PlayerPrefs.SetInt(MusicEnabledPrefsKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
            SetMixerVolume(MusicVolumeParameter, enabled);
            SetMixerVolume(AmbienceVolumeParameter, enabled);

            if (catalog == null || catalog.Mixer == null)
            {
                SetSourceMute(_musicSources, !enabled);
                if (_ambienceSource != null)
                {
                    _ambienceSource.mute = !enabled;
                }
            }
        }

        public void SetSfxEnabled(bool enabled)
        {
            PlayerPrefs.SetInt(SfxEnabledPrefsKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
            SetMixerVolume(SfxVolumeParameter, enabled);
            SetMixerVolume(UiVolumeParameter, enabled);
            SetMixerVolume(DialogueVolumeParameter, enabled);

            if (catalog == null || catalog.Mixer == null)
            {
                SetSourceMute(_sfxSources, !enabled);
                if (_uiSource != null)
                {
                    _uiSource.mute = !enabled;
                }

                if (_dialogueSource != null)
                {
                    _dialogueSource.mute = !enabled;
                }
            }
        }

        public bool CanPlayForTest(AudioCueId cue)
        {
            if (!TryGetEntry(cue, null, out AudioCueEntry entry))
            {
                return false;
            }

            return !IsRateLimited(cue, entry);
        }

        private bool PlayCue(AudioCueId cue, AudioCueCategory category)
        {
            Initialize();
            if (!TryGetEntry(cue, category, out AudioCueEntry entry) || IsRateLimited(cue, entry))
            {
                LastRejectedCueCount++;
                return false;
            }

            AudioClip clip = SelectVariant(cue, entry);
            if (clip == null)
            {
                LastRejectedCueCount++;
                return false;
            }

            int sourceIndex = SelectSfxSource(entry);
            if (sourceIndex < 0)
            {
                LastRejectedCueCount++;
                return false;
            }

            AudioSource source = _sfxSources[sourceIndex];
            ConfigureSource(source, entry);
            source.clip = clip;
            source.loop = false;
            source.volume = entry.Volume;
            source.pitch = ResolvePitch(entry);
            source.Play();

            VoiceState state = _voiceStates[sourceIndex];
            state.Cue = cue;
            state.Priority = entry.Priority;
            state.StartTime = Time.unscaledTime;
            MarkPlayed(cue);
            LastAcceptedCueCount++;
            return true;
        }

        private bool PlaySingleSource(AudioCueId cue, AudioCueCategory category, AudioSource source, bool interrupt)
        {
            Initialize();
            if (source == null || !TryGetEntry(cue, category, out AudioCueEntry entry) || IsRateLimited(cue, entry))
            {
                LastRejectedCueCount++;
                return false;
            }

            AudioClip clip = SelectVariant(cue, entry);
            if (clip == null)
            {
                LastRejectedCueCount++;
                return false;
            }

            if (interrupt)
            {
                source.Stop();
            }

            ConfigureSource(source, entry);
            source.clip = clip;
            source.loop = entry.Loop;
            source.volume = entry.Volume;
            source.pitch = ResolvePitch(entry);
            source.Play();
            MarkPlayed(cue);
            LastAcceptedCueCount++;
            return true;
        }

        private bool TryGetEntry(AudioCueId cue, AudioCueCategory? category, out AudioCueEntry entry)
        {
            entry = null;
            return catalog != null
                && catalog.TryGet(cue, out entry)
                && entry != null
                && (!category.HasValue || entry.Category == category.Value);
        }

        private int SelectSfxSource(AudioCueEntry entry)
        {
            int activeForCue = 0;
            int freeIndex = -1;
            int stealIndex = -1;
            float oldestTime = float.MaxValue;

            for (int index = 0; index < _sfxSources.Length; index++)
            {
                AudioSource source = _sfxSources[index];
                VoiceState state = _voiceStates[index];
                if (!source.isPlaying)
                {
                    freeIndex = index;
                    continue;
                }

                if (state.Cue == entry.Id)
                {
                    activeForCue++;
                }

                if (state.Priority <= entry.Priority && state.StartTime < oldestTime)
                {
                    oldestTime = state.StartTime;
                    stealIndex = index;
                }
            }

            if (activeForCue >= entry.MaximumSimultaneousVoices)
            {
                return -1;
            }

            return freeIndex >= 0 ? freeIndex : stealIndex;
        }

        private AudioClip SelectVariant(AudioCueId cue, AudioCueEntry entry)
        {
            AudioClip[] clips = entry.Clips;
            if (clips == null || clips.Length == 0)
            {
                return null;
            }

            if (clips.Length == 1)
            {
                return clips[0];
            }

            int cueIndex = (int)cue;
            int selected = Random.Range(0, clips.Length);
            int last = _lastVariantIndices[cueIndex];
            if (selected == last)
            {
                selected = (selected + 1) % clips.Length;
            }

            _lastVariantIndices[cueIndex] = selected;
            return clips[selected];
        }

        private bool IsRateLimited(AudioCueId cue, AudioCueEntry entry)
        {
            float minimumInterval = entry.MinimumInterval;
            return minimumInterval > 0f && Time.unscaledTime - _lastPlayTimes[(int)cue] < minimumInterval;
        }

        private void MarkPlayed(AudioCueId cue)
        {
            _lastPlayTimes[(int)cue] = Time.unscaledTime;
        }

        private float ResolvePitch(AudioCueEntry entry)
        {
            Vector2 pitchRange = entry.PitchRange;
            float min = Mathf.Min(pitchRange.x, pitchRange.y);
            float max = Mathf.Max(pitchRange.x, pitchRange.y);
            return Mathf.Approximately(min, max) ? min : Random.Range(min, max);
        }

        private void ConfigureSource(AudioSource source, AudioCueEntry entry)
        {
            source.outputAudioMixerGroup = entry.MixerGroup;
            source.spatialBlend = 0f;
            source.playOnAwake = false;
            source.ignoreListenerPause = true;
        }

        private AudioSource CreateSource(string sourceName)
        {
            GameObject sourceObject = new GameObject(sourceName);
            sourceObject.transform.SetParent(transform, false);
            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.spatialBlend = 0f;
            source.playOnAwake = false;
            source.ignoreListenerPause = true;
            return source;
        }

        private IEnumerator Crossfade(AudioSource previous, AudioSource next, float targetVolume, float fadeSeconds)
        {
            float elapsed = 0f;
            float previousStartVolume = previous.volume;
            while (elapsed < fadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeSeconds);
                if (previous != null)
                {
                    previous.volume = Mathf.Lerp(previousStartVolume, 0f, t);
                }

                if (next != null)
                {
                    next.volume = Mathf.Lerp(0f, targetVolume, t);
                }

                yield return null;
            }

            StopSource(previous);
            if (next != null)
            {
                next.volume = targetVolume;
            }

            _musicFadeCoroutine = null;
        }

        private IEnumerator FadeOutMusic(float fadeSeconds)
        {
            AudioSource first = _musicSources[0];
            AudioSource second = _musicSources[1];
            float firstVolume = first.volume;
            float secondVolume = second.volume;
            float elapsed = 0f;

            while (elapsed < fadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeSeconds);
                first.volume = Mathf.Lerp(firstVolume, 0f, t);
                second.volume = Mathf.Lerp(secondVolume, 0f, t);
                yield return null;
            }

            StopSource(first);
            StopSource(second);
            _musicFadeCoroutine = null;
        }

        private void SetMixerVolume(string parameterName, bool enabled)
        {
            if (catalog != null && catalog.Mixer != null)
            {
                catalog.Mixer.SetFloat(parameterName, enabled ? EnabledDb : DisabledDb);
            }
        }

        private static void StopSource(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            source.Stop();
            source.clip = null;
            source.volume = 0f;
        }

        private static void SetSourceMute(AudioSource[] sources, bool muted)
        {
            if (sources == null)
            {
                return;
            }

            for (int index = 0; index < sources.Length; index++)
            {
                if (sources[index] != null)
                {
                    sources[index].mute = muted;
                }
            }
        }

        private static int GetCueCapacity()
        {
            return (int)AudioCueId.CoreShutdown + 1;
        }

        private sealed class VoiceState
        {
            public AudioCueId Cue;
            public int Priority;
            public float StartTime;
        }
    }
}

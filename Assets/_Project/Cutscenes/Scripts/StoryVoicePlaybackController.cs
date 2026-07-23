using _Project.Scripts.Systems.AudioSystem;
using UnityEngine;
using TrueGateAudioSystem = _Project.Scripts.Systems.AudioSystem.AudioSystem;

namespace _Project.Cutscenes
{
    public sealed class StoryVoicePlaybackController : MonoBehaviour
    {
        [SerializeField] private StoryCutsceneDirector director;
        [SerializeField] private VoiceLineCatalog catalog;
        [SerializeField] private TrueGateAudioSystem audioSystem;
        [SerializeField] private bool logMissingVoice = true;

        private bool _subscribed;

        public void Init(
            StoryCutsceneDirector cutsceneDirector,
            VoiceLineCatalog voiceCatalog,
            TrueGateAudioSystem runtimeAudioSystem)
        {
            Unsubscribe();
            director = cutsceneDirector;
            catalog = voiceCatalog;
            audioSystem = runtimeAudioSystem;
            Subscribe();
        }

        private void Awake()
        {
            director ??= GetComponent<StoryCutsceneDirector>();
            if (audioSystem == null)
            {
                audioSystem = FindAnyObjectByType<TrueGateAudioSystem>(FindObjectsInactive.Include);
            }
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            StopDialogue();
            Unsubscribe();
        }

        private void OnDestroy()
        {
            StopDialogue();
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_subscribed || director == null)
            {
                return;
            }

            director.OnDialogueLineShown += HandleDialogueLineShown;
            director.OnDialogueAdvanceRequested += HandleDialogueAdvanceRequested;
            director.OnCutsceneFinished += HandleCutsceneFinished;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || director == null)
            {
                _subscribed = false;
                return;
            }

            director.OnDialogueLineShown -= HandleDialogueLineShown;
            director.OnDialogueAdvanceRequested -= HandleDialogueAdvanceRequested;
            director.OnCutsceneFinished -= HandleCutsceneFinished;
            _subscribed = false;
        }

        private void HandleDialogueLineShown(
            string cutsceneId,
            int lineIndex,
            int lineCount,
            StoryDialogueLine line)
        {
            StopDialogue();

            if (catalog != null
                && catalog.TryGet(cutsceneId, lineIndex, out VoiceLineCatalogEntry entry)
                && entry?.Clip != null)
            {
                audioSystem?.PlayDialogueClip(entry.Clip);
                return;
            }

            if (catalog != null && catalog.IsSkipped(cutsceneId, lineIndex))
            {
                return;
            }

            LogMissing(cutsceneId, lineIndex);
        }

        private void HandleDialogueAdvanceRequested()
        {
            StopDialogue();
        }

        private void HandleCutsceneFinished(string cutsceneId)
        {
            StopDialogue();
        }

        private void StopDialogue()
        {
            audioSystem?.StopDialogue();
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogMissing(string cutsceneId, int lineIndex)
        {
            if (logMissingVoice)
            {
                Debug.LogWarning(
                    $"Missing STORY voice for '{cutsceneId}' line {lineIndex}. Dialogue continues without audio.",
                    this);
            }
        }
    }
}

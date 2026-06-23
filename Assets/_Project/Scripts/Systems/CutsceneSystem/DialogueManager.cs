using System;
using System.Collections;
using System.Collections.Generic;
using _Project.Scripts.Data.ScriptableObjects.CutsceneConfigs;
using UnityEngine;

namespace _Project.Scripts.Systems.CutsceneSystem
{
    public sealed class DialogueManager : MonoBehaviour
    {
        [SerializeField] private DialoguePanel dialoguePanel;
        [SerializeField] private float charactersPerSecond = 42f;

        private Coroutine _playRoutine;
        private Action _onComplete;
        private bool _advanceRequested;
        private bool _skipRequested;
        private AudioSource _audioSource;

        public bool IsPlaying => _playRoutine != null;

        public void Init()
        {
            if (dialoguePanel == null)
            {
                dialoguePanel = FindAnyObjectByType<DialoguePanel>(FindObjectsInactive.Include);
            }

            if (dialoguePanel == null)
            {
                dialoguePanel = DialoguePanel.CreateRuntimePanel();
            }

            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
            }

            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
            }

            dialoguePanel.NextRequested -= HandleNextRequested;
            dialoguePanel.NextRequested += HandleNextRequested;
            dialoguePanel.SkipRequested -= HandleSkipRequested;
            dialoguePanel.SkipRequested += HandleSkipRequested;
            dialoguePanel.Hide();
        }

        private void Awake()
        {
            Init();
        }

        private void OnDestroy()
        {
            if (dialoguePanel == null)
            {
                return;
            }

            dialoguePanel.NextRequested -= HandleNextRequested;
            dialoguePanel.SkipRequested -= HandleSkipRequested;
        }

        public void Play(CutsceneDefinition definition, bool canSkip, Action onComplete)
        {
            Init();
            StopCurrent(invokeCompletion: false);

            _onComplete = onComplete;
            _advanceRequested = false;
            _skipRequested = false;
            dialoguePanel.Show(canSkip);
            _playRoutine = StartCoroutine(PlayRoutine(definition));
        }

        private IEnumerator PlayRoutine(CutsceneDefinition definition)
        {
            IReadOnlyList<DialogueLine> lines = definition.Lines;

            for (int index = 0; index < lines.Count; index++)
            {
                DialogueLine line = lines[index];
                if (line == null)
                {
                    continue;
                }

                yield return RevealLine(line);
                if (_skipRequested)
                {
                    break;
                }

                yield return WaitForAdvance();
                if (_skipRequested)
                {
                    break;
                }
            }

            CompleteCurrent();
        }

        private IEnumerator RevealLine(DialogueLine line)
        {
            _advanceRequested = false;
            string fullText = line.Text;
            dialoguePanel.SetLine(line, string.Empty);
            dialoguePanel.SetContinueVisible(false);

            if (line.BlipAudio != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(line.BlipAudio);
            }

            if (string.IsNullOrEmpty(fullText))
            {
                dialoguePanel.SetBodyText(string.Empty);
                dialoguePanel.SetContinueVisible(true);
                yield break;
            }

            float safeCharactersPerSecond = Mathf.Max(1f, charactersPerSecond);
            float visibleCharacters = 0f;
            int lastVisibleCount = 0;

            while (lastVisibleCount < fullText.Length)
            {
                if (_skipRequested)
                {
                    yield break;
                }

                if (_advanceRequested)
                {
                    dialoguePanel.SetBodyText(fullText);
                    break;
                }

                visibleCharacters += safeCharactersPerSecond * Time.unscaledDeltaTime;
                int visibleCount = Mathf.Clamp(
                    Mathf.FloorToInt(visibleCharacters),
                    0,
                    fullText.Length);

                if (visibleCount != lastVisibleCount)
                {
                    lastVisibleCount = visibleCount;
                    dialoguePanel.SetBodyText(fullText.Substring(0, visibleCount));
                }

                yield return null;
            }

            _advanceRequested = false;
            dialoguePanel.SetContinueVisible(true);
        }

        private IEnumerator WaitForAdvance()
        {
            _advanceRequested = false;

            while (!_advanceRequested && !_skipRequested)
            {
                yield return null;
            }

            _advanceRequested = false;
        }

        private void StopCurrent(bool invokeCompletion)
        {
            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
                _playRoutine = null;
            }

            dialoguePanel?.Hide();
            Action completion = _onComplete;
            _onComplete = null;

            if (invokeCompletion)
            {
                completion?.Invoke();
            }
        }

        private void CompleteCurrent()
        {
            _playRoutine = null;
            dialoguePanel?.Hide();
            Action completion = _onComplete;
            _onComplete = null;
            completion?.Invoke();
        }

        private void HandleNextRequested()
        {
            _advanceRequested = true;
        }

        private void HandleSkipRequested()
        {
            _skipRequested = true;
        }
    }
}

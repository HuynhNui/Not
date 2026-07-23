using System;
using HisaGames.CutsceneManager;
using UnityEngine;
using UnityEngine.Events;

namespace _Project.Cutscenes
{
    public sealed class StoryCutsceneDirector : MonoBehaviour
    {
        [Serializable]
        public sealed class CutsceneEvent : UnityEvent<string>
        {
        }

        [SerializeField] private EcCutsceneManager easyCutsceneManager;
        [SerializeField] private CutsceneDemoUIView view;
        [SerializeField] private bool warnWhenUsingFallback = true;
        [SerializeField] private CutsceneEvent cutsceneStarted = new CutsceneEvent();
        [SerializeField] private CutsceneEvent cutsceneFinished = new CutsceneEvent();

        private string _activeCutsceneId;
        private StoryCutsceneDefinition _activeDefinition;
        private int _activeLineIndex;
        private StoryEasyCutsceneVoiceAdapter _easyCutsceneAdapter;

        public event Action<string> OnCutsceneStarted;
        public event Action<string> OnCutsceneFinished;
        public event Action OnDialogueAdvanceRequested;
        public event Action<string, int, int, StoryDialogueLine> OnDialogueLineShown;
        public event Action<string> OnFinalChoiceSelected;

        public void Init(EcCutsceneManager easyManager, CutsceneDemoUIView demoView)
        {
            easyCutsceneManager = easyManager;
            view = demoView;
            WireButtons();
            view?.ShowMenu();
        }

        public void SetFallbackWarningsEnabled(bool isEnabled)
        {
            warnWhenUsingFallback = isEnabled;
        }

        private void Awake()
        {
            if (view == null)
            {
                view = FindAnyObjectByType<CutsceneDemoUIView>(FindObjectsInactive.Include);
            }

            WireButtons();
        }

        private void OnDestroy()
        {
            _easyCutsceneAdapter?.StopObserving();
            UnwireButtons();
        }

        public void Play(string cutsceneId)
        {
            if (!StoryCutsceneLibrary.TryGet(cutsceneId, out StoryCutsceneDefinition definition))
            {
                Debug.LogWarning($"Story cutscene ID '{cutsceneId}' is not registered.");
                return;
            }

            _activeCutsceneId = definition.CutsceneId;
            _activeDefinition = definition;
            _activeLineIndex = 0;
            _easyCutsceneAdapter?.StopObserving();

            view?.ShowCutscene();
            RaiseStarted(_activeCutsceneId);

            if (TryPlayEasyCutscene(_activeCutsceneId))
            {
                return;
            }

            ShowCurrentLine();
        }

        public void PlayTransient(
            StoryCutsceneDefinition definition,
            StoryCutscenePresentationMode presentationMode = StoryCutscenePresentationMode.FullScreen)
        {
            if (definition == null)
            {
                return;
            }

            _activeCutsceneId = definition.CutsceneId;
            _activeDefinition = definition;
            _activeLineIndex = 0;
            _easyCutsceneAdapter?.StopObserving();

            view?.SetPresentationMode(presentationMode);
            view?.ShowCutscene();
            RaiseStarted(_activeCutsceneId);
            ShowCurrentLine();
        }

        public void PlayBootSequence()
        {
            Play(StoryCutsceneIds.BootSequence);
        }

        public void PlayFirstDeathRecovery()
        {
            Play(StoryCutsceneIds.FirstDeathRecovery);
        }

        public void PlayEnemyDoesNotCharge()
        {
            Play(StoryCutsceneIds.EnemyDoesNotCharge);
        }

        public void PlayGateMemoryLeak()
        {
            Play(StoryCutsceneIds.GateMemoryLeak);
        }

        public void PlayHumanCommand()
        {
            Play(StoryCutsceneIds.HumanCommand);
        }

        public void PlaySystemFatigue()
        {
            Play(StoryCutsceneIds.SystemFatigue);
        }

        public void PlayFinalChoice()
        {
            Play(StoryCutsceneIds.FinalChoice);
        }

        private bool TryPlayEasyCutscene(string cutsceneId)
        {
            if (easyCutsceneManager == null)
            {
                if (warnWhenUsingFallback)
                {
                    Debug.LogWarning("Easy Cutscene manager is not assigned. Using demo fallback dialogue UI.");
                }

                return false;
            }

            HisaGames.Cutscene.EcCutscene easyCutscene = easyCutsceneManager.getCutscenesObject(cutsceneId);
            if (easyCutscene == null)
            {
                if (warnWhenUsingFallback)
                {
                    Debug.LogWarning($"Easy Cutscene entry '{cutsceneId}' is not wired. Using demo fallback dialogue UI.");
                }

                return false;
            }

            easyCutsceneManager.autoplayTime = -1f;
            easyCutsceneManager.InitCutscenes(cutsceneId);
            _easyCutsceneAdapter ??= GetComponent<StoryEasyCutsceneVoiceAdapter>();
            _easyCutsceneAdapter ??= gameObject.AddComponent<StoryEasyCutsceneVoiceAdapter>();
            _easyCutsceneAdapter.Begin(this, easyCutsceneManager, easyCutscene);
            return true;
        }

        internal void NotifyEasyDialogueAdvanceRequested()
        {
            OnDialogueAdvanceRequested?.Invoke();
        }

        internal void NotifyEasyDialogueLineShown(int lineIndex)
        {
            if (_activeDefinition == null
                || lineIndex < 0
                || lineIndex >= _activeDefinition.Lines.Count)
            {
                Debug.LogWarning(
                    $"Easy Cutscene line {lineIndex} is outside STORY definition '{_activeCutsceneId}'.");
                return;
            }

            _activeLineIndex = lineIndex;
            RaiseDialogueLineShown(_activeDefinition.Lines[lineIndex]);
        }

        internal void NotifyEasyCutsceneFinished()
        {
            FinishActiveCutscene();
        }

        private void AdvanceLine()
        {
            if (_activeDefinition == null)
            {
                return;
            }

            OnDialogueAdvanceRequested?.Invoke();
            _activeLineIndex++;
            if (_activeLineIndex >= _activeDefinition.Lines.Count)
            {
                CompleteActiveDefinition();
                return;
            }

            ShowCurrentLine();
        }

        private void ShowCurrentLine()
        {
            if (_activeDefinition == null
                || _activeLineIndex < 0
                || _activeLineIndex >= _activeDefinition.Lines.Count)
            {
                FinishActiveCutscene();
                return;
            }

            StoryDialogueLine line = _activeDefinition.Lines[_activeLineIndex];
            if (view == null)
            {
                Debug.LogWarning("Cutscene demo view is not assigned. Cannot display fallback dialogue.");
                return;
            }

            view.ShowCutscene();
            view.SetDialogueLine(line);
            RaiseDialogueLineShown(line);
        }

        private void RaiseDialogueLineShown(StoryDialogueLine line)
        {
            OnDialogueLineShown?.Invoke(
                _activeCutsceneId,
                _activeLineIndex,
                _activeDefinition.Lines.Count,
                line);
        }

        private void CompleteActiveDefinition()
        {
            if (_activeCutsceneId == StoryCutsceneIds.FinalChoicePreChoice)
            {
                ShowFinalChoice();
                return;
            }

            FinishActiveCutscene();
        }

        private void ShowFinalChoice()
        {
            if (view == null || view.ContinueProtocolButton == null || view.ShutDownCoreButton == null)
            {
                Debug.LogWarning("Final choice UI is not assigned. Closing CS_07 pre-choice.");
                FinishActiveCutscene();
                return;
            }

            view.ShowFinalChoice();
        }

        private void PlayContinueProtocol()
        {
            OnFinalChoiceSelected?.Invoke(StoryCutsceneIds.FinalChoiceContinueProtocol);
            Play(StoryCutsceneIds.FinalChoiceContinueProtocol);
        }

        private void PlayShutDownCore()
        {
            OnFinalChoiceSelected?.Invoke(StoryCutsceneIds.FinalChoiceShutDownCore);
            Play(StoryCutsceneIds.FinalChoiceShutDownCore);
        }

        private void WireButtons()
        {
            UnwireButtons();
            if (view == null)
            {
                return;
            }

            view.NextButton?.onClick.AddListener(AdvanceLine);
            view.CloseButton?.onClick.AddListener(FinishActiveCutscene);
            view.ContinueProtocolButton?.onClick.AddListener(PlayContinueProtocol);
            view.ShutDownCoreButton?.onClick.AddListener(PlayShutDownCore);
        }

        private void UnwireButtons()
        {
            if (view == null)
            {
                return;
            }

            view.NextButton?.onClick.RemoveListener(AdvanceLine);
            view.CloseButton?.onClick.RemoveListener(FinishActiveCutscene);
            view.ContinueProtocolButton?.onClick.RemoveListener(PlayContinueProtocol);
            view.ShutDownCoreButton?.onClick.RemoveListener(PlayShutDownCore);
        }

        private void FinishActiveCutscene()
        {
            _easyCutsceneAdapter?.StopObserving();
            view?.ReturnToMenu();
            view?.SetPresentationMode(StoryCutscenePresentationMode.FullScreen);

            if (string.IsNullOrEmpty(_activeCutsceneId))
            {
                _activeDefinition = null;
                return;
            }

            string completedId = _activeCutsceneId;
            _activeCutsceneId = string.Empty;
            _activeDefinition = null;
            _activeLineIndex = 0;
            RaiseFinished(completedId);
        }

        private void RaiseStarted(string cutsceneId)
        {
            cutsceneStarted?.Invoke(cutsceneId);
            OnCutsceneStarted?.Invoke(cutsceneId);
        }

        private void RaiseFinished(string cutsceneId)
        {
            cutsceneFinished?.Invoke(cutsceneId);
            OnCutsceneFinished?.Invoke(cutsceneId);
        }
    }
}

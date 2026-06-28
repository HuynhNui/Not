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
        [SerializeField] private CutsceneEvent cutsceneStarted = new CutsceneEvent();
        [SerializeField] private CutsceneEvent cutsceneFinished = new CutsceneEvent();

        private string _activeCutsceneId;
        private StoryCutsceneDefinition _activeDefinition;
        private int _activeLineIndex;

        public event Action<string> OnCutsceneStarted;
        public event Action<string> OnCutsceneFinished;

        public void Init(EcCutsceneManager easyManager, CutsceneDemoUIView demoView)
        {
            easyCutsceneManager = easyManager;
            view = demoView;
            WireButtons();
            view?.ShowMenu();
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

            view?.ShowCutscene();
            RaiseStarted(_activeCutsceneId);

            if (TryPlayEasyCutscene(_activeCutsceneId))
            {
                return;
            }

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
                Debug.LogWarning("Easy Cutscene manager is not assigned. Using demo fallback dialogue UI.");
                return false;
            }

            if (easyCutsceneManager.getCutscenesObject(cutsceneId) == null)
            {
                Debug.LogWarning($"Easy Cutscene entry '{cutsceneId}' is not wired. Using demo fallback dialogue UI.");
                return false;
            }

            easyCutsceneManager.InitCutscenes(cutsceneId);
            return true;
        }

        private void AdvanceLine()
        {
            if (_activeDefinition == null)
            {
                return;
            }

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
            Play(StoryCutsceneIds.FinalChoiceContinueProtocol);
        }

        private void PlayShutDownCore()
        {
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
            view?.ReturnToMenu();

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

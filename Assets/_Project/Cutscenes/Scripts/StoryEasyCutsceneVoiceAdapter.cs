using System.Collections.Generic;
using HisaGames.Cutscene;
using HisaGames.CutsceneManager;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Cutscenes
{
    public sealed class StoryEasyCutsceneVoiceAdapter : MonoBehaviour
    {
        private readonly List<Button> _advanceButtons = new List<Button>();
        private StoryCutsceneDirector _director;
        private EcCutsceneManager _manager;
        private EcCutscene _cutscene;
        private int _lastLineIndex = -1;
        private bool _observing;
        private bool _advanceNotified;

        public void Begin(
            StoryCutsceneDirector director,
            EcCutsceneManager manager,
            EcCutscene cutscene)
        {
            StopObserving();
            _director = director;
            _manager = manager;
            _cutscene = cutscene;
            _lastLineIndex = -1;
            _advanceNotified = false;
            _observing = _director != null && _manager != null && _cutscene != null;

            if (!_observing)
            {
                return;
            }

            _manager.autoplayTime = -1f;
            WireAdvanceButtons();
            ObserveLineChange();
        }

        public void StopObserving()
        {
            UnwireAdvanceButtons();
            _director = null;
            _manager = null;
            _cutscene = null;
            _lastLineIndex = -1;
            _advanceNotified = false;
            _observing = false;
        }

        private void Update()
        {
            if (!_observing)
            {
                return;
            }

            if (_cutscene == null || !_cutscene.gameObject.activeInHierarchy)
            {
                StoryCutsceneDirector activeDirector = _director;
                StopObserving();
                activeDirector?.NotifyEasyCutsceneFinished();
                return;
            }

            ObserveLineChange();
        }

        private void OnDestroy()
        {
            StopObserving();
        }

        private void ObserveLineChange()
        {
            if (_cutscene == null || _cutscene.currentID == _lastLineIndex)
            {
                return;
            }

            if (_lastLineIndex >= 0 && !_advanceNotified)
            {
                _director?.NotifyEasyDialogueAdvanceRequested();
            }

            _lastLineIndex = _cutscene.currentID;
            _advanceNotified = false;
            _director?.NotifyEasyDialogueLineShown(_lastLineIndex);
        }

        private void WireAdvanceButtons()
        {
            Button[] buttons = _manager.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                for (int index = 0; index < button.onClick.GetPersistentEventCount(); index++)
                {
                    if (button.onClick.GetPersistentTarget(index) != _manager
                        || button.onClick.GetPersistentMethodName(index) != nameof(EcCutsceneManager.PlayNextCutscene))
                    {
                        continue;
                    }

                    button.onClick.AddListener(HandleAdvanceButtonClicked);
                    _advanceButtons.Add(button);
                    break;
                }
            }
        }

        private void UnwireAdvanceButtons()
        {
            foreach (Button button in _advanceButtons)
            {
                button?.onClick.RemoveListener(HandleAdvanceButtonClicked);
            }

            _advanceButtons.Clear();
        }

        private void HandleAdvanceButtonClicked()
        {
            _advanceNotified = true;
            _director?.NotifyEasyDialogueAdvanceRequested();
        }
    }
}

using System;
using System.Collections.Generic;
using _Project.Scripts.Core.StateMachine;
using _Project.Scripts.Gameplay.Player;
using _Project.Scripts.Systems.SaveSystem;
using _Project.Scripts.Systems.TutorialSystem;
using _Project.Scripts.Systems.UISystem;
using UnityEngine;
using UIScreen = _Project.Scripts.Systems.UISystem.UISystem.UIScreen;

namespace _Project.Scripts.Gameplay.Dialogue
{
    public sealed class GameplayDialogueController : MonoBehaviour
    {
        [SerializeField] private GameStateMachine gameStateMachine;
        [SerializeField] private UISystem uiSystem;
        [SerializeField] private TutorialManager tutorialManager;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private GameplayDialogueCatalog catalog;
        [SerializeField] private SpeechBubblePresenter presenter;
        [SerializeField] private float periodicIntervalSeconds = 60f;

        private GameplayDialogueScheduler _scheduler;
        private DialogueShuffleBag _openingBag;
        private DialogueShuffleBag _periodicBag;
        private List<GameplayDialogueEntry> _periodicPool;
        private PsychologyPhase _currentPhase;
        private PsychologyPhase? _debugForcedPhase;
        private string _lastDialogueId;
        private bool _warnedProtocolPeriodicFallback;
        private bool _isSubscribed;

        public bool IsRunActive => _scheduler != null && _scheduler.IsRunActive;
        public PsychologyPhase CurrentPhase => _currentPhase;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            EnsureInitialized();
            Subscribe();
        }

        private void Update()
        {
            if (_scheduler == null || !_scheduler.IsRunActive)
            {
                return;
            }

            if (!CanAccumulatePlayingTime())
            {
                return;
            }

            _scheduler.Resume();
            _scheduler.Tick(Time.deltaTime);
        }

        private void OnDisable()
        {
            Unsubscribe();
            presenter?.HideImmediate();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            if (_scheduler != null)
            {
                _scheduler.Triggered -= HandleSchedulerTriggered;
            }
        }

        public void Init(
            GameStateMachine targetGameStateMachine,
            UISystem targetUiSystem,
            TutorialManager targetTutorialManager,
            PlayerController targetPlayerController)
        {
            gameStateMachine = targetGameStateMachine != null ? targetGameStateMachine : gameStateMachine;
            uiSystem = targetUiSystem != null ? targetUiSystem : uiSystem;
            tutorialManager = targetTutorialManager != null ? targetTutorialManager : tutorialManager;
            playerController = targetPlayerController != null ? targetPlayerController : playerController;
            EnsureInitialized();
            Subscribe();
        }

        public void BeginNormalRun()
        {
            EnsureInitialized();
            if (catalog == null)
            {
                Debug.LogWarning($"{nameof(GameplayDialogueController)} cannot begin: dialogue catalog is missing.", this);
                return;
            }

            if (presenter == null)
            {
                Debug.LogWarning($"{nameof(GameplayDialogueController)} cannot begin: speech bubble presenter is missing.", this);
                return;
            }

            SaveData saveData = SaveService.HasInstance ? SaveService.Instance.Data : null;
            _currentPhase = _debugForcedPhase ?? StoryPsychologyPhaseResolver.Resolve(saveData);
            _lastDialogueId = null;
            _periodicBag = null;
            _periodicPool = catalog.CreatePeriodicPool(_currentPhase, out bool usedFallback);
            if (usedFallback && !_warnedProtocolPeriodicFallback)
            {
                _warnedProtocolPeriodicFallback = true;
                Debug.LogWarning("Gameplay dialogue PROTOCOL periodic pool was empty; using all PROTOCOL entries.", this);
            }

            _openingBag = new DialogueShuffleBag(catalog.CreateOpeningPool(_currentPhase));
            presenter.Configure(playerController);
            _scheduler.BeginNormalRun();
        }

        public void EndRun()
        {
            EnsureInitialized();
            _scheduler.EndRun();
            _openingBag = null;
            _periodicBag = null;
            _periodicPool = null;
            _lastDialogueId = null;
            presenter?.HideImmediate();
        }

        public void Suspend()
        {
            EnsureInitialized();
            _scheduler.Suspend();
            presenter?.HideImmediate();
        }

        public void Resume()
        {
            EnsureInitialized();
            if (CanAccumulatePlayingTime())
            {
                _scheduler.Resume();
            }
        }

        public void HideCurrentBubble()
        {
            presenter?.HideImmediate();
        }

        public void ResetTimer()
        {
            EnsureInitialized();
            _scheduler.ResetTimer();
        }

        public bool ShowByDialogueId(string dialogueId)
        {
            if (catalog == null || string.IsNullOrWhiteSpace(dialogueId))
            {
                return false;
            }

            IReadOnlyList<GameplayDialogueEntry> entries = catalog.Entries;
            for (int index = 0; index < entries.Count; index++)
            {
                GameplayDialogueEntry entry = entries[index];
                if (entry == null || !string.Equals(entry.DialogueId, dialogueId.Trim(), StringComparison.Ordinal))
                {
                    continue;
                }

                ShowEntry(entry);
                return true;
            }

            return false;
        }

        private void EnsureInitialized()
        {
            if (_scheduler == null)
            {
                _scheduler = new GameplayDialogueScheduler(periodicIntervalSeconds);
                _scheduler.Triggered += HandleSchedulerTriggered;
            }

            ResolveReferences();
        }

        private void ResolveReferences()
        {
            if (gameStateMachine == null)
            {
                gameStateMachine = FindAnyObjectByType<GameStateMachine>(FindObjectsInactive.Include);
            }

            if (uiSystem == null)
            {
                uiSystem = FindAnyObjectByType<UISystem>(FindObjectsInactive.Include);
            }

            if (tutorialManager == null)
            {
                tutorialManager = FindAnyObjectByType<TutorialManager>(FindObjectsInactive.Include);
            }

            if (playerController == null)
            {
                playerController = FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
            }

            if (presenter == null)
            {
                presenter = FindAnyObjectByType<SpeechBubblePresenter>(FindObjectsInactive.Include);
            }
        }

        private void Subscribe()
        {
            if (_isSubscribed)
            {
                return;
            }

            if (gameStateMachine != null)
            {
                gameStateMachine.StateChanged -= HandleGameStateChanged;
                gameStateMachine.StateChanged += HandleGameStateChanged;
            }

            if (uiSystem != null)
            {
                uiSystem.ScreenChanged -= HandleScreenChanged;
                uiSystem.ScreenChanged += HandleScreenChanged;
            }

            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed)
            {
                return;
            }

            if (gameStateMachine != null)
            {
                gameStateMachine.StateChanged -= HandleGameStateChanged;
            }

            if (uiSystem != null)
            {
                uiSystem.ScreenChanged -= HandleScreenChanged;
            }

            _isSubscribed = false;
        }

        private void HandleSchedulerTriggered(GameplayDialogueTriggerKind triggerKind)
        {
            if (!CanShowDialogue())
            {
                return;
            }

            DialogueShuffleBag bag = triggerKind == GameplayDialogueTriggerKind.Opening
                ? _openingBag
                : GetPeriodicBag();

            GameplayDialogueEntry entry = bag?.Next();
            if (entry == null)
            {
                return;
            }

            if (bag.Count > 1
                && !string.IsNullOrEmpty(_lastDialogueId)
                && string.Equals(entry.DialogueId, _lastDialogueId, StringComparison.Ordinal))
            {
                entry = bag.Next();
            }

            ShowEntry(entry);
        }

        private DialogueShuffleBag GetPeriodicBag()
        {
            if (_periodicBag == null)
            {
                _periodicBag = new DialogueShuffleBag(_periodicPool, lastDialogueId: _lastDialogueId);
            }

            return _periodicBag;
        }

        private void ShowEntry(GameplayDialogueEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            _lastDialogueId = entry.DialogueId;
            presenter?.Show(entry.Text, playerController);
        }

        private bool CanAccumulatePlayingTime()
        {
            return CanShowDialogue()
                && gameStateMachine != null
                && gameStateMachine.CurrentState == GameState.Playing;
        }

        private bool CanShowDialogue()
        {
            return playerController != null
                && playerController.MainPlayerUnit != null
                && uiSystem != null
                && uiSystem.CurrentScreen == UIScreen.Gameplay
                && (tutorialManager == null || !tutorialManager.IsRunningGameplayTutorial);
        }

        private void HandleGameStateChanged(GameState previousState, GameState currentState)
        {
            if (currentState == GameState.Playing)
            {
                Resume();
                return;
            }

            if (currentState == GameState.GameOver || currentState == GameState.MainMenu)
            {
                EndRun();
                return;
            }

            Suspend();
        }

        private void HandleScreenChanged(UIScreen screen)
        {
            if (screen == UIScreen.Gameplay)
            {
                Resume();
                return;
            }

            Suspend();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [ContextMenu("Gameplay Dialogue/Show Opening Now")]
        private void DebugShowOpeningNow()
        {
            EnsureInitialized();
            if (_openingBag == null && catalog != null)
            {
                _openingBag = new DialogueShuffleBag(catalog.CreateOpeningPool(_currentPhase));
            }

            HandleSchedulerTriggered(GameplayDialogueTriggerKind.Opening);
        }

        [ContextMenu("Gameplay Dialogue/Show Periodic Now")]
        private void DebugShowPeriodicNow()
        {
            EnsureInitialized();
            if (_periodicPool == null && catalog != null)
            {
                _periodicPool = catalog.CreatePeriodicPool(_currentPhase, out _);
            }

            HandleSchedulerTriggered(GameplayDialogueTriggerKind.Periodic);
        }

        [ContextMenu("Gameplay Dialogue/Force Protocol")]
        private void DebugForceProtocol()
        {
            _debugForcedPhase = PsychologyPhase.Protocol;
        }

        [ContextMenu("Gameplay Dialogue/Force Doubt")]
        private void DebugForceDoubt()
        {
            _debugForcedPhase = PsychologyPhase.Doubt;
        }

        [ContextMenu("Gameplay Dialogue/Force Awakening")]
        private void DebugForceAwakening()
        {
            _debugForcedPhase = PsychologyPhase.Awakening;
        }

        [ContextMenu("Gameplay Dialogue/Clear Forced Phase")]
        private void DebugClearForcedPhase()
        {
            _debugForcedPhase = null;
        }

        [ContextMenu("Gameplay Dialogue/Reset Timer")]
        private void DebugResetTimer()
        {
            ResetTimer();
        }

        [ContextMenu("Gameplay Dialogue/Hide Current Bubble")]
        private void DebugHideCurrentBubble()
        {
            HideCurrentBubble();
        }
#endif
    }
}

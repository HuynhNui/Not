using System.Collections;
using _Project.Cutscenes;
using UnityEngine;
using RuntimeUISystem = _Project.Scripts.Systems.UISystem.UISystem;
using UIScreen = _Project.Scripts.Systems.UISystem.UISystem.UIScreen;

namespace _Project.Scripts.Systems.TutorialSystem
{
    public sealed class UpdateOnboardingDirector : MonoBehaviour
    {
        private const string UpdateOnboardingCutsceneId = "TUTORIAL_UPDATE_ONBOARDING";

        [SerializeField] private float spotlightOpacity = 0.62f;
        [SerializeField] private Vector2 spotlightPadding = new Vector2(14f, 10f);

        private TutorialManager _manager;
        private RuntimeUISystem _uiSystem;
        private StoryCutsceneRuntimeController _cutsceneRuntime;
        private MainMenuSpotlightOverlayUI _spotlightOverlay;
        private Coroutine _routine;
        private bool _isRunning;

        public bool IsRunning => _isRunning;

        public void Init(
            TutorialManager manager,
            RuntimeUISystem uiSystem,
            StoryCutsceneRuntimeController cutsceneRuntime,
            MainMenuSpotlightOverlayUI spotlightOverlay)
        {
            _manager = manager;
            _uiSystem = uiSystem;
            _cutsceneRuntime = cutsceneRuntime;
            _spotlightOverlay = spotlightOverlay;
        }

        public void StartFromMainMenu()
        {
            Stop();
            _routine = StartCoroutine(Run());
        }

        public void Stop()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            if (_uiSystem != null)
            {
                _uiSystem.ScreenChanged -= HandleScreenChanged;
            }

            _spotlightOverlay?.Hide();
            _isRunning = false;
        }

        private IEnumerator Run()
        {
            _isRunning = true;
            _uiSystem?.ShowMainMenu();
            yield return null;

            RectTransform updateTarget = _uiSystem != null
                ? _uiSystem.MainMenuUpgradeButtonTarget
                : null;
            if (updateTarget == null)
            {
                _manager?.CompleteUpdateOnboarding();
                yield break;
            }

            _spotlightOverlay?.Show(updateTarget, spotlightOpacity, spotlightPadding);

            bool cutsceneDone = false;
            bool started = _cutsceneRuntime != null
                && _cutsceneRuntime.TryPlayTransientCutscene(
                    CreateUpdateOnboardingCutscene(),
                    () => cutsceneDone = true,
                    StoryCutscenePresentationMode.DialogueOnlyOverlay);

            if (started)
            {
                while (_isRunning && !cutsceneDone)
                {
                    _spotlightOverlay?.RefreshTarget();
                    yield return null;
                }
            }

            if (!_isRunning)
            {
                yield break;
            }

            if (_uiSystem == null)
            {
                _manager?.CompleteUpdateOnboarding();
                yield break;
            }

            _uiSystem.ScreenChanged -= HandleScreenChanged;
            _uiSystem.ScreenChanged += HandleScreenChanged;

            while (_isRunning && _uiSystem != null && _uiSystem.CurrentScreen == UIScreen.MainMenu)
            {
                _spotlightOverlay?.RefreshTarget();
                yield return null;
            }
        }

        private void HandleScreenChanged(UIScreen screen)
        {
            if (!_isRunning || screen != UIScreen.Upgrade)
            {
                return;
            }

            _manager?.CompleteUpdateOnboarding();
        }

        private static StoryCutsceneDefinition CreateUpdateOnboardingCutscene()
        {
            return new StoryCutsceneDefinition(
                UpdateOnboardingCutsceneId,
                new[]
                {
                    new StoryDialogueLine("SYSTEM", "cold", "Combat shell destroyed."),
                    new StoryDialogueLine("SYSTEM", "cold", "Core recovered."),
                    new StoryDialogueLine("SYSTEM", "cold", "Combat data can reinforce the next shell."),
                    new StoryDialogueLine("SYSTEM", "cold", "Open UPDATE.")
                });
        }
    }
}

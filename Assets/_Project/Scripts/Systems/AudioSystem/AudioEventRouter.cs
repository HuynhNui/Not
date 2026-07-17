using System.Collections.Generic;
using _Project.Cutscenes;
using _Project.Scripts.Core.GameLoop;
using _Project.Scripts.Data.ScriptableObjects.GateConfigs;
using _Project.Scripts.Gameplay.Combat;
using _Project.Scripts.Gameplay.Dialogue;
using _Project.Scripts.Gameplay.Enemies;
using _Project.Scripts.Gameplay.Player;
using _Project.Scripts.Systems.EnemySpawnerSystem;
using _Project.Scripts.Systems.GateSystem;
using _Project.Scripts.Systems.MissionSystem;
using _Project.Scripts.Systems.UISystem;
using UnityEngine;
using RuntimeEnemySpawnerSystem = _Project.Scripts.Systems.EnemySpawnerSystem.EnemySpawnerSystem;
using RuntimeGateSystem = _Project.Scripts.Systems.GateSystem.GateSystem;
using RuntimeMissionSystem = _Project.Scripts.Systems.MissionSystem.MissionSystem;
using RuntimeUISystem = _Project.Scripts.Systems.UISystem.UISystem;

namespace _Project.Scripts.Systems.AudioSystem
{
    public sealed class AudioEventRouter : MonoBehaviour
    {
        private const string FreezeGateId = "utility_freeze";

        [SerializeField] private AudioSystem audioSystem;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private RuntimeUISystem uiSystem;
        [SerializeField] private RuntimeEnemySpawnerSystem enemySpawnerSystem;
        [SerializeField] private RuntimeGateSystem gateSystem;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private StoryCutsceneDirector storyCutsceneDirector;
        [SerializeField] private GameplayDialogueController gameplayDialogueController;
        [SerializeField] private float pressureMusicStartSeconds = 180f;

        private readonly HashSet<string> _missingReferenceWarnings = new HashSet<string>();
        private BulletSpawner _mainBulletSpawner;
        private RuntimeMissionSystem _missionSystem;
        private bool _subscribed;
        private bool _pressureMusicStarted;
        private bool _runDeployPlayed;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
            audioSystem?.ApplySavedSettings();
            HandleScreenChanged(uiSystem != null ? uiSystem.CurrentScreen : RuntimeUISystem.UIScreen.MainMenu);
        }

        private void Update()
        {
            if (_pressureMusicStarted
                || enemySpawnerSystem == null
                || uiSystem == null
                || uiSystem.CurrentScreen != RuntimeUISystem.UIScreen.Gameplay)
            {
                return;
            }

            if (enemySpawnerSystem.ElapsedTime >= Mathf.Max(0f, pressureMusicStartSeconds))
            {
                _pressureMusicStarted = true;
                audioSystem?.PlayMusic(AudioCueId.BgmGameplayPressure, 1.25f);
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void BindMissionSystem(RuntimeMissionSystem missionSystem)
        {
            if (_missionSystem == missionSystem)
            {
                return;
            }

            if (_missionSystem != null)
            {
                _missionSystem.MissionCompleted -= HandleMissionCompleted;
            }

            _missionSystem = missionSystem;
            if (_subscribed && _missionSystem != null)
            {
                _missionSystem.MissionCompleted -= HandleMissionCompleted;
                _missionSystem.MissionCompleted += HandleMissionCompleted;
            }
        }

        private void ResolveReferences()
        {
            audioSystem = ResolveReference(audioSystem, nameof(audioSystem));
            gameManager = ResolveReference(gameManager, nameof(gameManager));
            uiSystem = ResolveReference(uiSystem, nameof(uiSystem));
            enemySpawnerSystem = ResolveReference(enemySpawnerSystem, nameof(enemySpawnerSystem));
            gateSystem = ResolveReference(gateSystem, nameof(gateSystem));
            playerController = ResolveReference(playerController, nameof(playerController));
            storyCutsceneDirector = ResolveReference(storyCutsceneDirector, nameof(storyCutsceneDirector));
            gameplayDialogueController = ResolveReference(gameplayDialogueController, nameof(gameplayDialogueController));

            BulletSpawner resolvedSpawner = playerController != null
                && playerController.MainPlayerUnit != null
                ? playerController.MainPlayerUnit.BulletSpawner
                : null;

            if (resolvedSpawner != _mainBulletSpawner)
            {
                if (_mainBulletSpawner != null)
                {
                    _mainBulletSpawner.VolleyFired -= HandleMainVolleyFired;
                }

                _mainBulletSpawner = resolvedSpawner;
                if (_subscribed && _mainBulletSpawner != null)
                {
                    _mainBulletSpawner.VolleyFired -= HandleMainVolleyFired;
                    _mainBulletSpawner.VolleyFired += HandleMainVolleyFired;
                }
            }
        }

        private T ResolveReference<T>(T current, string fieldName) where T : Object
        {
            if (current != null)
            {
                return current;
            }

            T resolved = FindAnyObjectByType<T>(FindObjectsInactive.Include);
            if (resolved == null && _missingReferenceWarnings.Add(fieldName))
            {
                Debug.LogWarning($"{nameof(AudioEventRouter)} missing serialized reference '{fieldName}'. Runtime lookup failed.", this);
            }
            else if (resolved != null && _missingReferenceWarnings.Add(fieldName))
            {
                Debug.LogWarning($"{nameof(AudioEventRouter)} missing serialized reference '{fieldName}'. Runtime lookup found '{resolved.name}'.", this);
            }

            return resolved;
        }

        private void Subscribe()
        {
            if (_subscribed)
            {
                return;
            }

            if (uiSystem != null)
            {
                uiSystem.ScreenChanged -= HandleScreenChanged;
                uiSystem.ScreenChanged += HandleScreenChanged;
                uiSystem.UiCueRequested -= HandleUiCueRequested;
                uiSystem.UiCueRequested += HandleUiCueRequested;
                uiSystem.MusicSettingChanged -= HandleMusicSettingChanged;
                uiSystem.MusicSettingChanged += HandleMusicSettingChanged;
                uiSystem.SfxSettingChanged -= HandleSfxSettingChanged;
                uiSystem.SfxSettingChanged += HandleSfxSettingChanged;
            }

            if (gameManager != null)
            {
                gameManager.RunBecamePlayable -= HandleRunBecamePlayable;
                gameManager.RunBecamePlayable += HandleRunBecamePlayable;
                gameManager.RunEnded -= HandleRunEnded;
                gameManager.RunEnded += HandleRunEnded;
                gameManager.ReturnedToMenu -= HandleReturnedToMenu;
                gameManager.ReturnedToMenu += HandleReturnedToMenu;
            }

            if (enemySpawnerSystem != null)
            {
                enemySpawnerSystem.EnemyDamaged -= HandleEnemyDamaged;
                enemySpawnerSystem.EnemyDamaged += HandleEnemyDamaged;
                enemySpawnerSystem.ChomboomExploded -= HandleChomboomExploded;
                enemySpawnerSystem.ChomboomExploded += HandleChomboomExploded;
            }

            if (gateSystem != null)
            {
                gateSystem.GateSelected -= HandleGateSelected;
                gateSystem.GateSelected += HandleGateSelected;
            }

            if (storyCutsceneDirector != null)
            {
                storyCutsceneDirector.OnCutsceneStarted -= HandleCutsceneStarted;
                storyCutsceneDirector.OnCutsceneStarted += HandleCutsceneStarted;
                storyCutsceneDirector.OnCutsceneFinished -= HandleCutsceneFinished;
                storyCutsceneDirector.OnCutsceneFinished += HandleCutsceneFinished;
                storyCutsceneDirector.OnDialogueAdvanceRequested -= HandleDialogueAdvanceRequested;
                storyCutsceneDirector.OnDialogueAdvanceRequested += HandleDialogueAdvanceRequested;
                storyCutsceneDirector.OnDialogueLineShown -= HandleDialogueLineShown;
                storyCutsceneDirector.OnDialogueLineShown += HandleDialogueLineShown;
                storyCutsceneDirector.OnFinalChoiceSelected -= HandleFinalChoiceSelected;
                storyCutsceneDirector.OnFinalChoiceSelected += HandleFinalChoiceSelected;
            }

            if (gameplayDialogueController != null)
            {
                gameplayDialogueController.DialogueShown -= HandleGameplayDialogueShown;
                gameplayDialogueController.DialogueShown += HandleGameplayDialogueShown;
            }

            if (_mainBulletSpawner != null)
            {
                _mainBulletSpawner.VolleyFired -= HandleMainVolleyFired;
                _mainBulletSpawner.VolleyFired += HandleMainVolleyFired;
            }

            if (_missionSystem != null)
            {
                _missionSystem.MissionCompleted -= HandleMissionCompleted;
                _missionSystem.MissionCompleted += HandleMissionCompleted;
            }

            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            if (uiSystem != null)
            {
                uiSystem.ScreenChanged -= HandleScreenChanged;
                uiSystem.UiCueRequested -= HandleUiCueRequested;
                uiSystem.MusicSettingChanged -= HandleMusicSettingChanged;
                uiSystem.SfxSettingChanged -= HandleSfxSettingChanged;
            }

            if (gameManager != null)
            {
                gameManager.RunBecamePlayable -= HandleRunBecamePlayable;
                gameManager.RunEnded -= HandleRunEnded;
                gameManager.ReturnedToMenu -= HandleReturnedToMenu;
            }

            if (enemySpawnerSystem != null)
            {
                enemySpawnerSystem.EnemyDamaged -= HandleEnemyDamaged;
                enemySpawnerSystem.ChomboomExploded -= HandleChomboomExploded;
            }

            if (gateSystem != null)
            {
                gateSystem.GateSelected -= HandleGateSelected;
            }

            if (storyCutsceneDirector != null)
            {
                storyCutsceneDirector.OnCutsceneStarted -= HandleCutsceneStarted;
                storyCutsceneDirector.OnCutsceneFinished -= HandleCutsceneFinished;
                storyCutsceneDirector.OnDialogueAdvanceRequested -= HandleDialogueAdvanceRequested;
                storyCutsceneDirector.OnDialogueLineShown -= HandleDialogueLineShown;
                storyCutsceneDirector.OnFinalChoiceSelected -= HandleFinalChoiceSelected;
            }

            if (gameplayDialogueController != null)
            {
                gameplayDialogueController.DialogueShown -= HandleGameplayDialogueShown;
            }

            if (_mainBulletSpawner != null)
            {
                _mainBulletSpawner.VolleyFired -= HandleMainVolleyFired;
            }

            if (_missionSystem != null)
            {
                _missionSystem.MissionCompleted -= HandleMissionCompleted;
            }

            _subscribed = false;
        }

        private void HandleScreenChanged(RuntimeUISystem.UIScreen screen)
        {
            if (audioSystem == null)
            {
                return;
            }

            switch (screen)
            {
                case RuntimeUISystem.UIScreen.MainMenu:
                case RuntimeUISystem.UIScreen.Upgrade:
                case RuntimeUISystem.UIScreen.Settings:
                case RuntimeUISystem.UIScreen.Mission:
                    audioSystem.PlayMusic(AudioCueId.BgmMainMenu, 1f);
                    audioSystem.StopAmbience();
                    break;
                case RuntimeUISystem.UIScreen.GameOver:
                    audioSystem.PlayMusic(AudioCueId.BgmMainMenu, 1f);
                    audioSystem.StopAmbience();
                    break;
            }
        }

        private void HandleRunBecamePlayable()
        {
            _pressureMusicStarted = false;
            if (!_runDeployPlayed)
            {
                audioSystem?.PlaySfx(AudioCueId.RunStartDeploy);
                _runDeployPlayed = true;
            }

            audioSystem?.PlayMusic(AudioCueId.BgmGameplayNormal, 1f);
            audioSystem?.PlayAmbience(AudioCueId.AmbGameplayPlanet);
        }

        private void HandleRunEnded()
        {
            _runDeployPlayed = false;
            audioSystem?.PlaySfx(AudioCueId.SquadDefeated);
            audioSystem?.StopAmbience();
        }

        private void HandleReturnedToMenu()
        {
            _runDeployPlayed = false;
            _pressureMusicStarted = false;
            audioSystem?.PlayMusic(AudioCueId.BgmMainMenu, 1f);
            audioSystem?.StopAmbience();
        }

        private void HandleUiCueRequested(AudioCueId cue)
        {
            audioSystem?.PlayUi(cue);
        }

        private void HandleMusicSettingChanged(bool enabled)
        {
            audioSystem?.SetMusicEnabled(enabled);
        }

        private void HandleSfxSettingChanged(bool enabled)
        {
            audioSystem?.SetSfxEnabled(enabled);
        }

        private void HandleMainVolleyFired(BulletSpawner spawner, int projectileCount)
        {
            audioSystem?.PlaySfx(AudioCueId.PlayerShot);
        }

        private void HandleEnemyDamaged(EnemyController enemy, float damageAmount, float currentHealth)
        {
            audioSystem?.PlaySfx(AudioCueId.BulletHitEnemy);
        }

        private void HandleChomboomExploded(ChomboomController chomboom)
        {
            audioSystem?.PlaySfx(AudioCueId.ChomboomExplosion);
        }

        private void HandleGateSelected(int gateSet, GateConfig config)
        {
            if (config != null && config.GateId == FreezeGateId)
            {
                audioSystem?.PlaySfx(AudioCueId.GateFreezeActivate);
            }
        }

        private void HandleMissionCompleted(MissionDefinition completedMission, MissionDefinition unlockedMission)
        {
            audioSystem?.PlaySfx(AudioCueId.StingerMissionComplete);
        }

        private void HandleCutsceneStarted(string cutsceneId)
        {
            if (cutsceneId == StoryCutsceneIds.FinalChoiceContinueProtocol
                || cutsceneId == StoryCutsceneIds.FinalChoiceShutDownCore)
            {
                audioSystem?.PlayMusic(AudioCueId.BgmEnding, 1f);
            }
            else
            {
                audioSystem?.PlayMusic(AudioCueId.BgmStoryCutscene, 1f);
            }

            audioSystem?.StopAmbience();

            if (cutsceneId == StoryCutsceneIds.GateMemoryLeak || cutsceneId == StoryCutsceneIds.SystemFatigue)
            {
                audioSystem?.PlaySfx(AudioCueId.SystemWarningMemoryGlitch);
            }
        }

        private void HandleCutsceneFinished(string cutsceneId)
        {
            HandleScreenChanged(uiSystem != null ? uiSystem.CurrentScreen : RuntimeUISystem.UIScreen.MainMenu);
        }

        private void HandleDialogueAdvanceRequested()
        {
            audioSystem?.PlayUi(AudioCueId.UiDialogueAdvance);
        }

        private void HandleDialogueLineShown(string cutsceneId, int lineIndex, int lineCount, StoryDialogueLine line)
        {
            if (line != null)
            {
                if (line.Speaker == "SYSTEM")
                {
                    audioSystem?.PlayDialogue(AudioCueId.DialogueTypeSystem);
                }
                else if (line.Speaker == "UNIT-07")
                {
                    audioSystem?.PlayDialogue(AudioCueId.DialogueTypeUnit07);
                }
            }

            if (cutsceneId == StoryCutsceneIds.FinalChoiceShutDownCore && lineIndex == lineCount - 1)
            {
                audioSystem?.PlaySfx(AudioCueId.CoreShutdown);
            }
        }

        private void HandleFinalChoiceSelected(string branchId)
        {
            audioSystem?.PlaySfx(AudioCueId.FinalChoiceConfirm);
        }

        private void HandleGameplayDialogueShown(GameplayDialogueEntry entry)
        {
            audioSystem?.PlayDialogue(AudioCueId.DialogueTypeUnit07);
        }
    }
}

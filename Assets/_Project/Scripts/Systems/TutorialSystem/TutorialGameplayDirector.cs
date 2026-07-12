using System.Collections;
using System.Collections.Generic;
using _Project.Cutscenes;
using _Project.Scripts.Gameplay.Enemies;
using _Project.Scripts.Gameplay.Gates;
using _Project.Scripts.Gameplay.Player;
using _Project.Scripts.Systems.EnemySpawnerSystem;
using _Project.Scripts.Systems.GateSystem;
using _Project.Scripts.Systems.RunStatsSystem;
using UnityEngine;
using RuntimeEnemySpawnerSystem = _Project.Scripts.Systems.EnemySpawnerSystem.EnemySpawnerSystem;
using RuntimeGateSystem = _Project.Scripts.Systems.GateSystem.GateSystem;

namespace _Project.Scripts.Systems.TutorialSystem
{
    public sealed class TutorialGameplayDirector : MonoBehaviour
    {
        private TutorialManager _manager;
        private TutorialOverlayUI _overlay;
        private StoryCutsceneRuntimeController _storyCutsceneRuntime;
        private PlayerController _playerController;
        private MainPlayerUnit _mainPlayerUnit;
        private RuntimeEnemySpawnerSystem _enemySpawnerSystem;
        private RuntimeGateSystem _gateSystem;
        private RunStatsTracker _runStatsTracker;
        private Coroutine _routine;
        private int _tutorialEnemyKills;
        private bool _tutorialGateSelected;

        public bool IsRunning { get; private set; }

        public void Init(
            TutorialManager manager,
            TutorialOverlayUI overlay,
            StoryCutsceneRuntimeController storyCutsceneRuntime,
            PlayerController playerController,
            MainPlayerUnit mainPlayerUnit,
            RuntimeEnemySpawnerSystem enemySpawnerSystem,
            RuntimeGateSystem gateSystem,
            RunStatsTracker runStatsTracker)
        {
            _manager = manager;
            _overlay = overlay;
            _storyCutsceneRuntime = storyCutsceneRuntime;
            _playerController = playerController;
            _mainPlayerUnit = mainPlayerUnit;
            _enemySpawnerSystem = enemySpawnerSystem;
            _gateSystem = gateSystem;
            _runStatsTracker = runStatsTracker;
        }

        public void StartTutorial()
        {
            StopTutorial();
            IsRunning = true;
            _tutorialEnemyKills = 0;
            _tutorialGateSelected = false;
            Subscribe();
            _routine = StartCoroutine(RunTutorial());
        }

        public void SkipTutorial()
        {
            Cleanup();
            _manager?.CompleteGameplayTutorial(startNormalRun: true);
        }

        public void StopTutorial()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            Cleanup();
        }

        private IEnumerator RunTutorial()
        {
            TutorialConfig config = _manager != null ? _manager.Config : null;
            _enemySpawnerSystem?.SetSpawningEnabled(false);
            _gateSystem?.SetSpawningEnabled(false);
            _playerController?.SetControlsEnabled(true);

            yield return PlayTutorialCutscene(TutorialCutsceneDefinitions.MovementIntro);

            yield return PlayTutorialCutscene(TutorialCutsceneDefinitions.MovementPractice);
            yield return RunMovementStep(config);

            yield return PlayTutorialCutscene(TutorialCutsceneDefinitions.AutoFire);
            yield return RunAutoFireStep(config);

            yield return PlayTutorialCutscene(TutorialCutsceneDefinitions.EnemyWarning);
            yield return RunEnemyWarningStep(config);

            yield return RunGateStep(config);

            yield return PlayTutorialCutscene(TutorialCutsceneDefinitions.GameplayComplete);
            Cleanup();
            _manager?.CompleteGameplayTutorial(startNormalRun: true);
        }

        private IEnumerator RunMovementStep(TutorialConfig config)
        {
            ShowGameplayHint(showSwipeIcon: true);

            Transform trackedTransform = _mainPlayerUnit != null ? _mainPlayerUnit.transform : _playerController?.transform;
            float startX = trackedTransform != null ? trackedTransform.position.x : 0f;
            float requiredDistance = config != null ? config.MovementRequiredWorldDistance : 0.75f;

            while (IsRunning)
            {
                if (trackedTransform != null && Mathf.Abs(trackedTransform.position.x - startX) >= requiredDistance)
                {
                    break;
                }

                yield return null;
            }

            _overlay?.HideSwipeIcon();
        }

        private IEnumerator RunAutoFireStep(TutorialConfig config)
        {
            ShowGameplayHint(showSwipeIcon: false);
            _tutorialEnemyKills = 0;

            for (int index = 0; index < 3; index++)
            {
                _enemySpawnerSystem?.SpawnTutorialEnemy(GetTutorialEnemyPosition(index), weak: true);
            }

            float timeout = config != null ? config.AutoFireTimeoutSeconds : 10f;
            int requiredKills = config != null ? config.AutoFireRequiredKills : 3;
            float endTime = Time.realtimeSinceStartup + timeout;

            while (IsRunning && _tutorialEnemyKills < requiredKills && Time.realtimeSinceStartup < endTime)
            {
                yield return null;
            }
        }

        private IEnumerator RunEnemyWarningStep(TutorialConfig config)
        {
            ShowGameplayHint(showSwipeIcon: false);
            EnemyController warningEnemy = _enemySpawnerSystem?.SpawnTutorialEnemy(GetWarningEnemyPosition(), weak: false);
            if (warningEnemy != null)
            {
                warningEnemy.ApplyRuntimeStats(new EnemyRuntimeStats(1f, 0.75f, 0f, 0, 0, false));
            }

            float endTime = Time.realtimeSinceStartup + (config != null ? config.EnemyWarningSeconds : 5f);
            while (IsRunning && Time.realtimeSinceStartup < endTime)
            {
                yield return null;
            }
        }

        private IEnumerator RunGateStep(TutorialConfig config)
        {
            yield return PlayTutorialCutscene(TutorialCutsceneDefinitions.RecruitGate);
            _tutorialGateSelected = false;
            int squadCountBeforeRecruit = _playerController != null ? _playerController.CurrentSquadCount : 1;

            yield return RunSingleGateStep(
                () => _gateSystem?.SpawnTutorialGateById("major_recruit", GetGateSpawnPosition()),
                config);
            EnsureTutorialRecruitApplied(squadCountBeforeRecruit);

            yield return PlayTutorialCutscene(TutorialCutsceneDefinitions.DefaultGateChoice);
            _tutorialGateSelected = false;

            int defaultGateAttempts = 0;
            int maxAttempts = GetGateAttemptCount(config);
            while (IsRunning && !_tutorialGateSelected && defaultGateAttempts < maxAttempts)
            {
                defaultGateAttempts++;
                IReadOnlyList<GateLogic> gates = _gateSystem?.SpawnTutorialDefaultGateSet();
                float defaultGateEndTime = Time.realtimeSinceStartup + (config != null ? config.GateTimeoutSeconds : 15f);
                ShowGameplayHint(showSwipeIcon: false);

                while (IsRunning && !_tutorialGateSelected && Time.realtimeSinceStartup < defaultGateEndTime)
                {
                    TryCollectTutorialGateSet(gates);
                    yield return null;
                }

                if (_tutorialGateSelected)
                {
                    break;
                }

                _gateSystem?.ClearTutorialGates();
                yield return null;
            }

            _overlay?.HideHighlight();
        }

        private IEnumerator RunSingleGateStep(System.Func<GateLogic> spawnGate, TutorialConfig config)
        {
            int attempts = 0;
            int maxAttempts = GetGateAttemptCount(config);
            while (IsRunning && !_tutorialGateSelected && attempts < maxAttempts)
            {
                attempts++;
                GateLogic gate = spawnGate?.Invoke();
                float endTime = Time.realtimeSinceStartup + (config != null ? config.GateTimeoutSeconds : 15f);
                ShowGameplayHint(showSwipeIcon: false);

                while (IsRunning && !_tutorialGateSelected && Time.realtimeSinceStartup < endTime)
                {
                    TryCollectTutorialGate(gate);
                    yield return null;
                }

                if (_tutorialGateSelected)
                {
                    break;
                }

                _gateSystem?.ClearTutorialGates();
                yield return null;
            }

            _overlay?.HideHighlight();
        }

        private void TryCollectTutorialGateSet(IReadOnlyList<GateLogic> gates)
        {
            if (gates == null)
            {
                return;
            }

            for (int index = 0; index < gates.Count; index++)
            {
                if (TryCollectTutorialGate(gates[index]))
                {
                    return;
                }
            }
        }

        private bool TryCollectTutorialGate(GateLogic gate)
        {
            if (!IsRunning || _tutorialGateSelected || gate == null)
            {
                return false;
            }

            PlayerUnit triggerUnit = null;
            Vector3 contactPoint = Vector3.zero;
            if (_playerController != null)
            {
                _playerController.TryGetClosestAliveUnitContactPoint(
                    gate.transform.position,
                    out triggerUnit,
                    out contactPoint);
            }

            if (triggerUnit == null && _mainPlayerUnit != null && !_mainPlayerUnit.IsDead)
            {
                triggerUnit = _mainPlayerUnit;
                contactPoint = _mainPlayerUnit.transform.position;
            }

            if (triggerUnit == null || triggerUnit.IsDead)
            {
                return false;
            }

            Bounds gateBounds = GetTutorialGateCollectionBounds(gate);
            gateBounds.Expand(new Vector3(0.8f, 0.8f, 0f));
            contactPoint.z = gateBounds.center.z;
            if (!gateBounds.Contains(contactPoint))
            {
                return false;
            }

            gate.HandlePlayerTriggered(triggerUnit);
            if (_tutorialGateSelected)
            {
                return true;
            }

            gate.ApplyEffect();
            gate.Despawn();
            _gateSystem?.ClearTutorialGates();
            _tutorialGateSelected = true;
            return true;
        }

        private static int GetGateAttemptCount(TutorialConfig config)
        {
            int respawns = config != null ? config.GateRespawnCount : 1;
            return Mathf.Max(1, respawns + 1);
        }

        private static Bounds GetTutorialGateCollectionBounds(GateLogic gate)
        {
            Collider2D collider = gate != null ? gate.GetComponent<Collider2D>() : null;
            if (collider != null && collider.enabled)
            {
                return collider.bounds;
            }

            Renderer renderer = gate != null ? gate.GetComponentInChildren<Renderer>() : null;
            if (renderer != null && renderer.enabled)
            {
                return renderer.bounds;
            }

            Vector3 center = gate != null ? gate.transform.position : Vector3.zero;
            return new Bounds(center, new Vector3(1.6f, 1.6f, 0f));
        }

        private void EnsureTutorialRecruitApplied(int previousSquadCount)
        {
            if (_playerController == null)
            {
                return;
            }

            int expectedSquadCount = Mathf.Clamp(
                Mathf.Max(1, previousSquadCount) + 1,
                1,
                _playerController.MaxSquadCount);
            if (_playerController.CurrentSquadCount >= expectedSquadCount)
            {
                return;
            }

            _playerController.SetSquadCount(
                expectedSquadCount,
                _playerController.RecruitSpawnHpRatio);
        }

        private IEnumerator PlayTutorialCutscene(StoryCutsceneDefinition definition)
        {
            if (!IsRunning)
            {
                yield break;
            }

            _overlay?.HideOverlay();

            if (_storyCutsceneRuntime == null || definition == null)
            {
                yield break;
            }

            bool completed = false;
            float previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            if (!_storyCutsceneRuntime.TryPlayTransientCutscene(definition, () => completed = true))
            {
                Time.timeScale = previousTimeScale;
                yield break;
            }

            while (IsRunning && !completed)
            {
                yield return null;
            }

            Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
        }

        private void ShowGameplayHint(bool showSwipeIcon)
        {
            _overlay?.ShowOverlay(dimBackgroundVisible: false, blockInput: false);
            _overlay?.SetInputBlocking(false);
            _overlay?.ShowSkipButton(true);
            _overlay?.HideDialogue();
            _overlay?.HideHighlight();

            if (showSwipeIcon)
            {
                _overlay?.ShowSwipeIcon();
                return;
            }

            _overlay?.HideSwipeIcon();
        }

        private Vector3 GetTutorialEnemyPosition(int index)
        {
            Camera camera = Camera.main;
            if (camera == null || !camera.orthographic)
            {
                return transform.position + new Vector3(index - 1f, 4f, 0f);
            }

            float x = Mathf.Lerp(-0.45f, 0.45f, index / 2f);
            return camera.ViewportToWorldPoint(new Vector3(0.5f + x * 0.5f, 0.82f, Mathf.Abs(camera.transform.position.z)));
        }

        private Vector3 GetWarningEnemyPosition()
        {
            Camera camera = Camera.main;
            if (camera == null || !camera.orthographic)
            {
                return transform.position + Vector3.up * 5f;
            }

            Vector3 playerPosition = _mainPlayerUnit != null ? _mainPlayerUnit.transform.position : transform.position;
            Vector3 viewport = camera.WorldToViewportPoint(playerPosition);
            viewport.y = 0.92f;
            viewport.z = Mathf.Abs(camera.transform.position.z);
            return camera.ViewportToWorldPoint(viewport);
        }

        private Vector3 GetGateSpawnPosition()
        {
            Camera camera = Camera.main;
            if (camera == null || !camera.orthographic)
            {
                return transform.position + Vector3.up * 5f;
            }

            return camera.ViewportToWorldPoint(new Vector3(0.5f, 1.08f, Mathf.Abs(camera.transform.position.z)));
        }

        private void Subscribe()
        {
            if (_enemySpawnerSystem != null)
            {
                _enemySpawnerSystem.EnemyKilled -= HandleTutorialEnemyKilled;
                _enemySpawnerSystem.EnemyKilled += HandleTutorialEnemyKilled;
            }

            if (_gateSystem != null)
            {
                _gateSystem.GateSelected -= HandleTutorialGateSelected;
                _gateSystem.GateSelected += HandleTutorialGateSelected;
            }
        }

        private void Cleanup()
        {
            IsRunning = false;
            _overlay?.HideOverlay();
            _enemySpawnerSystem?.CleanupTutorialEnemies();
            _gateSystem?.ClearTutorialGates();

            if (_enemySpawnerSystem != null)
            {
                _enemySpawnerSystem.EnemyKilled -= HandleTutorialEnemyKilled;
            }

            if (_gateSystem != null)
            {
                _gateSystem.GateSelected -= HandleTutorialGateSelected;
            }
        }

        private void HandleTutorialEnemyKilled(EnemyController enemy)
        {
            if (IsRunning)
            {
                _tutorialEnemyKills++;
            }
        }

        private void HandleTutorialGateSelected(int gateSetIndex, _Project.Scripts.Data.ScriptableObjects.GateConfigs.GateConfig config)
        {
            if (IsRunning)
            {
                _tutorialGateSelected = true;
            }
        }
    }
}

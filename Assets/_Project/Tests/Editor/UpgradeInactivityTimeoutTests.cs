using System;
using System.IO;
using System.Reflection;
using _Project.Scripts.Systems.ProgressionSystem;
using _Project.Scripts.Systems.SaveSystem;
using _Project.Scripts.Systems.UISystem;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _Project.Tests.Editor
{
    public sealed class UIInactivityTimeoutControllerTests
    {
        [Test]
        public void Advance_AtWarningThreshold_ShowsWarningAndFullGraceCountdown()
        {
            UIInactivityTimeoutController controller = new UIInactivityTimeoutController(60f, 10f);
            int lastCountdown = -1;
            controller.GraceCountdownChanged += seconds => lastCountdown = seconds;

            controller.StartMonitoring();
            controller.Advance(59.99f);

            Assert.That(controller.IsWarningVisible, Is.False);

            controller.Advance(0.01f);

            Assert.That(controller.IsWarningVisible, Is.True);
            Assert.That(lastCountdown, Is.EqualTo(10));
        }

        [Test]
        public void RegisterActivity_BeforeThreshold_RestartsIdleWindow()
        {
            UIInactivityTimeoutController controller = new UIInactivityTimeoutController(60f, 10f);

            controller.StartMonitoring();
            controller.Advance(59f);
            controller.RegisterActivity();
            controller.Advance(2f);

            Assert.That(controller.IsWarningVisible, Is.False);
            Assert.That(controller.IdleElapsedSeconds, Is.EqualTo(2f));
        }

        [Test]
        public void RegisterActivity_DuringGrace_HidesWarningAndStartsFreshCycle()
        {
            UIInactivityTimeoutController controller = new UIInactivityTimeoutController(60f, 10f);

            controller.StartMonitoring();
            controller.Advance(65f);
            Assert.That(controller.IsWarningVisible, Is.True);

            controller.RegisterActivity();
            controller.Advance(59f);

            Assert.That(controller.IsWarningVisible, Is.False);
            Assert.That(controller.GraceRemainingSeconds, Is.EqualTo(10f));
        }

        [Test]
        public void GraceExpiry_RaisesTimeoutExactlyOnce()
        {
            UIInactivityTimeoutController controller = new UIInactivityTimeoutController(60f, 10f);
            int timeoutCount = 0;
            controller.TimedOut += () => timeoutCount++;

            controller.StartMonitoring();
            controller.Advance(70f);
            controller.Advance(100f);

            Assert.That(timeoutCount, Is.EqualTo(1));
            Assert.That(controller.IsMonitoring, Is.False);
            Assert.That(controller.IsWarningVisible, Is.False);
        }

        [Test]
        public void StopThenRestart_ClearsWarningAndElapsedState()
        {
            UIInactivityTimeoutController controller = new UIInactivityTimeoutController(60f, 10f);

            controller.StartMonitoring();
            controller.Advance(65f);
            controller.StopMonitoring();
            controller.StartMonitoring();

            Assert.That(controller.IsMonitoring, Is.True);
            Assert.That(controller.IsWarningVisible, Is.False);
            Assert.That(controller.IdleElapsedSeconds, Is.Zero);
            Assert.That(controller.GraceRemainingSeconds, Is.EqualTo(10f));
        }

        [Test]
        public void TimeScale_DoesNotChangeExplicitUnscaledAdvance()
        {
            UIInactivityTimeoutController controller = new UIInactivityTimeoutController(60f, 10f);
            float originalTimeScale = Time.timeScale;

            try
            {
                Time.timeScale = 0f;
                controller.StartMonitoring();
                controller.Advance(60f);

                Assert.That(controller.IsWarningVisible, Is.True);
            }
            finally
            {
                Time.timeScale = originalTimeScale;
            }
        }
    }

    public sealed class UpgradeInactivityUiIntegrationTests
    {
        private const string MainScenePath = "Assets/_Project/Scenes/Main.unity";
        private const string UpgradePanelPath = "GameCanvas/UIRoot/SafeAreaRoot/UpgradePanel";
        private const string PopupPath = UpgradePanelPath + "/InactivityWarningPopup";
        private const string WarningTextPath =
            PopupPath + "/ConfirmSafeAreaRoot/ConfirmPanel/BodyText";
        private const string StayButtonPath =
            PopupPath + "/ConfirmSafeAreaRoot/ConfirmPanel/ButtonRow/StayButton";
        private const string BackButtonPath =
            UpgradePanelPath + "/UpgradeContentRoot/TopBar/BackButton";

        private string _saveDirectoryPath;
        private SaveService _saveService;
        private UISystem _uiSystem;

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            _saveDirectoryPath = Path.Combine(
                Path.GetTempPath(),
                $"true-gate-upgrade-inactivity-test-{Guid.NewGuid():N}");
            _saveService = SaveService.CreateForTests(_saveDirectoryPath);
            SaveService.SetInstanceForTests(_saveService);
            _saveService.EnsureLoaded();
            PlayerMetaUpgradeService.Configure(null, null, null);

            _uiSystem = UnityEngine.Object.FindAnyObjectByType<UISystem>(FindObjectsInactive.Include);
            Assert.That(_uiSystem, Is.Not.Null);
            _uiSystem.Init();
        }

        [TearDown]
        public void TearDown()
        {
            PlayerMetaUpgradeService.Configure(null, null, null);
            SaveService.SetInstanceForTests(null);
            Time.timeScale = 1f;

            if (Directory.Exists(_saveDirectoryPath))
            {
                Directory.Delete(_saveDirectoryPath, recursive: true);
            }
        }

        [Test]
        public void Scene_HasInactiveWarningPopupWithNonBlockingOverlayAndStayButton()
        {
            GameObject popup = FindSceneObjectByPath(PopupPath);
            Image overlay = FindSceneObjectByPath(PopupPath + "/ConfirmOverlay").GetComponent<Image>();
            Button stayButton = FindSceneObjectByPath(StayButtonPath).GetComponent<Button>();

            Assert.That(popup, Is.Not.Null);
            Assert.That(popup.activeSelf, Is.False);
            Assert.That(overlay, Is.Not.Null);
            Assert.That(overlay.raycastTarget, Is.False);
            Assert.That(stayButton, Is.Not.Null);
            Assert.That(stayButton.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void Warning_UsesCountdownCopyAndStayStartsFreshCycle()
        {
            _uiSystem.ShowUpgrade();
            UIInactivityTimeoutController timer = GetTimer();

            timer.Advance(60f);

            Assert.That(_uiSystem.IsUpgradeInactivityWarningVisible, Is.True);
            Assert.That(FindSceneObjectByPath(PopupPath).activeSelf, Is.True);
            Assert.That(FindSceneObjectByPath(WarningTextPath).GetComponent<TextMeshProUGUI>().text,
                Is.EqualTo("No activity detected. Returning to Main Menu in 10s."));

            FindSceneObjectByPath(StayButtonPath).GetComponent<Button>().onClick.Invoke();
            timer.Advance(59f);

            Assert.That(_uiSystem.CurrentScreen, Is.EqualTo(UISystem.UIScreen.Upgrade));
            Assert.That(_uiSystem.IsUpgradeInactivityWarningVisible, Is.False);
        }

        [Test]
        public void GraceExpiry_ReturnsMainMenuOnceAndDoesNotQuit()
        {
            int mainMenuTransitionCount = 0;
            _uiSystem.ScreenChanged += screen =>
            {
                if (screen == UISystem.UIScreen.MainMenu)
                {
                    mainMenuTransitionCount++;
                }
            };

            _uiSystem.ShowUpgrade();
            UIInactivityTimeoutController timer = GetTimer();
            timer.Advance(70f);
            timer.Advance(100f);

            Assert.That(_uiSystem.CurrentScreen, Is.EqualTo(UISystem.UIScreen.MainMenu));
            Assert.That(mainMenuTransitionCount, Is.EqualTo(1));
            Assert.That(_uiSystem.IsUpgradeInactivityMonitoring, Is.False);
            Assert.That(FindSceneObjectByPath(PopupPath).activeSelf, Is.False);
        }

        [Test]
        public void BackWhileWarningVisible_ReturnsMainMenuAndStopsTimer()
        {
            _uiSystem.ShowUpgrade();
            GetTimer().Advance(60f);

            FindSceneObjectByPath(BackButtonPath).GetComponent<Button>().onClick.Invoke();

            Assert.That(_uiSystem.CurrentScreen, Is.EqualTo(UISystem.UIScreen.MainMenu));
            Assert.That(_uiSystem.IsUpgradeInactivityMonitoring, Is.False);
            Assert.That(FindSceneObjectByPath(PopupPath).activeSelf, Is.False);
        }

        [Test]
        public void RepeatedOpenClose_AlwaysStartsClean()
        {
            for (int index = 0; index < 3; index++)
            {
                _uiSystem.ShowUpgrade();
                GetTimer().Advance(65f);
                Assert.That(_uiSystem.IsUpgradeInactivityWarningVisible, Is.True);

                _uiSystem.ShowMainMenu();
                Assert.That(_uiSystem.IsUpgradeInactivityMonitoring, Is.False);
                Assert.That(FindSceneObjectByPath(PopupPath).activeSelf, Is.False);
            }

            _uiSystem.ShowUpgrade();

            Assert.That(_uiSystem.IsUpgradeInactivityMonitoring, Is.True);
            Assert.That(_uiSystem.IsUpgradeInactivityWarningVisible, Is.False);
            Assert.That(GetTimer().IdleElapsedSeconds, Is.Zero);
        }

        [Test]
        public void SuccessfulPurchase_KeepsAuthoritativeSaveEffectsAndResetsIdleTimer()
        {
            _saveService.Data.walletCoins = 1000;
            int cost = PlayerMetaUpgradeService.GetCost(PlayerMetaUpgradeType.Damage);
            int levelBefore = _saveService.Data.GetUpgradeLevel(PlayerMetaUpgradeType.Damage);
            int lifetimeSpentBefore = _saveService.Data.lifetimeCoinsSpent;

            _uiSystem.ShowUpgrade();
            GetTimer().Advance(59f);
            InvokePurchase(PlayerMetaUpgradeType.Damage);
            GetTimer().Advance(2f);

            Assert.That(_saveService.Data.GetUpgradeLevel(PlayerMetaUpgradeType.Damage), Is.EqualTo(levelBefore + 1));
            Assert.That(_saveService.Data.walletCoins, Is.EqualTo(1000 - cost));
            Assert.That(_saveService.Data.lifetimeCoinsSpent, Is.EqualTo(lifetimeSpentBefore + cost));
            Assert.That(_uiSystem.CurrentScreen, Is.EqualTo(UISystem.UIScreen.Upgrade));
            Assert.That(_uiSystem.IsUpgradeInactivityWarningVisible, Is.False);
        }

        [TestCase(PlayerMetaUpgradeType.Damage, 0, false)]
        [TestCase((PlayerMetaUpgradeType)999, 1000, false)]
        [TestCase(PlayerMetaUpgradeType.Damage, 1000, true)]
        public void RejectedPurchase_DoesNotMutateSaveAndStillResetsIdleTimer(
            PlayerMetaUpgradeType type,
            int walletCoins,
            bool setMaxLevel)
        {
            _saveService.Data.walletCoins = walletCoins;
            if (setMaxLevel)
            {
                _saveService.Data.SetUpgradeLevel(type, PlayerMetaUpgradeService.GetMaxLevel(type));
            }

            int levelBefore = _saveService.Data.GetUpgradeLevel(type);
            int lifetimeSpentBefore = _saveService.Data.lifetimeCoinsSpent;

            _uiSystem.ShowUpgrade();
            GetTimer().Advance(59f);
            InvokePurchase(type);
            GetTimer().Advance(2f);

            Assert.That(_saveService.Data.GetUpgradeLevel(type), Is.EqualTo(levelBefore));
            Assert.That(_saveService.Data.walletCoins, Is.EqualTo(walletCoins));
            Assert.That(_saveService.Data.lifetimeCoinsSpent, Is.EqualTo(lifetimeSpentBefore));
            Assert.That(_uiSystem.IsUpgradeInactivityWarningVisible, Is.False);
        }

        private UIInactivityTimeoutController GetTimer()
        {
            FieldInfo field = typeof(UISystem).GetField(
                "_upgradeInactivityTimeout",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (UIInactivityTimeoutController)field.GetValue(_uiSystem);
        }

        private void InvokePurchase(PlayerMetaUpgradeType type)
        {
            MethodInfo method = typeof(UISystem).GetMethod(
                "TryPurchaseUpgrade",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(_uiSystem, new object[] { type });
        }

        private static GameObject FindSceneObjectByPath(string path)
        {
            string[] parts = path.Split('/');
            Scene scene = SceneManager.GetActiveScene();
            GameObject current = null;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == parts[0])
                {
                    current = root;
                    break;
                }
            }

            for (int index = 1; index < parts.Length && current != null; index++)
            {
                Transform child = current.transform.Find(parts[index]);
                current = child != null ? child.gameObject : null;
            }

            Assert.That(current, Is.Not.Null, $"Missing scene object at '{path}'.");
            return current;
        }
    }
}

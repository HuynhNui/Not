using System;
using System.Collections.Generic;
using _Project.Scripts.Systems.ProgressionSystem;
using _Project.Scripts.Systems.RunStatsSystem;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace _Project.Scripts.Systems.UISystem
{
    /// <summary>
    /// Controls prefab/scene-built UI panels. This class does not create UI objects.
    /// </summary>
    public sealed class UISystem : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [FormerlySerializedAs("hudRoot")]
        [SerializeField] private GameObject gameplayHudPanel;
        [SerializeField] private GameObject upgradePanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject gameOverPanel;

        [Header("Main Menu")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button mainMenuUpgradeButton;
        [SerializeField] private Button mainMenuSettingsButton;
        [SerializeField] private TextMeshProUGUI bestRunText;
        [SerializeField] private TextMeshProUGUI walletText;
        [SerializeField] private TextMeshProUGUI bestScoreText;
        [SerializeField] private TextMeshProUGUI bestTimeText;
        [SerializeField] private TextMeshProUGUI bestEnemiesKilledText;
        [SerializeField] private TextMeshProUGUI bestCoinsText;

        [Header("Gameplay HUD")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private TextMeshProUGUI timeSurvivalText;
        [SerializeField] private TextMeshProUGUI moneyText;
        [SerializeField] private TextMeshProUGUI enemyDefeatedCountText;
        [SerializeField] private TextMeshProUGUI scoreText;

        [Header("Upgrade")]
        [SerializeField] private TextMeshProUGUI upgradeCurrencyText;
        [SerializeField] private List<UpgradeRowBinding> upgradeRows = new List<UpgradeRowBinding>();
        [SerializeField] private Button upgradeBackButton;

        [Header("Settings")]
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Toggle vibrationToggle;
        [SerializeField] private Toggle performanceModeToggle;
        [SerializeField] private Button settingsBackButton;

        [Header("Pause")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button pauseRestartButton;
        [SerializeField] private Button pauseSettingsButton;
        [SerializeField] private Button pauseHomeButton;

        [Header("Game Over")]
        [SerializeField] private TextMeshProUGUI finalTimeText;
        [SerializeField] private TextMeshProUGUI finalKillText;
        [SerializeField] private TextMeshProUGUI moneyEarnedText;
        [FormerlySerializedAs("bestTimeText")]
        [SerializeField] private TextMeshProUGUI gameOverBestTimeText;
        [FormerlySerializedAs("bestKillText")]
        [SerializeField] private TextMeshProUGUI gameOverBestKillText;
        [FormerlySerializedAs("playAgainButton")]
        [SerializeField] private Button retryButton;
        [SerializeField] private Button gameOverUpgradeButton;
        [SerializeField] private Button gameOverHomeButton;

        private const string MusicVolumePrefsKey = "Settings.MusicVolume";
        private const string SfxVolumePrefsKey = "Settings.SfxVolume";
        private const string VibrationPrefsKey = "Settings.Vibration";
        private const string PerformanceModePrefsKey = "Settings.PerformanceMode";

        private RunStatsTracker _runStatsTracker;
        private UIScreen _currentScreen = UIScreen.None;
        private UIScreen _settingsReturnScreen = UIScreen.MainMenu;
        private readonly HashSet<string> _missingReferenceWarnings = new HashSet<string>();
        private bool _isInitialized;

        public event Action PlayRequested;
        public event Action PauseRequested;
        public event Action ResumeRequested;
        public event Action RestartRequested;
        public event Action HomeRequested;

        public void Init(RunStatsTracker runStatsTracker = null)
        {
            if (runStatsTracker != null)
            {
                _runStatsTracker = runStatsTracker;
            }

            ValidateRequiredReferences();
            WireButtons();
            RefreshSettingsControls();
            RefreshMenuStats();
            RefreshUpgradePanel();

            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;
            ShowMainMenu();
        }

        private void Awake()
        {
            Init();
        }

        private void Update()
        {
            if (_currentScreen == UIScreen.Gameplay)
            {
                RefreshHud();
            }
        }

        public void BindRunStatsTracker(RunStatsTracker runStatsTracker)
        {
            _runStatsTracker = runStatsTracker;
            RefreshMenuStats();
            RefreshUpgradePanel();
            RefreshHud();
        }

        public void ShowMainMenu()
        {
            Time.timeScale = 1f;
            SetPrimaryPanel(UIScreen.MainMenu);
            RefreshMenuStats();
        }

        public void ShowHud()
        {
            ShowGameplayHud();
        }

        public void ShowGameplayHud()
        {
            Time.timeScale = 1f;
            SetPrimaryPanel(UIScreen.Gameplay);
            RefreshHud();
        }

        public void ShowPause()
        {
            Time.timeScale = 0f;
            SetPrimaryPanel(UIScreen.Pause);
        }

        public void ShowSettingsFromMainMenu()
        {
            _settingsReturnScreen = UIScreen.MainMenu;
            SetPrimaryPanel(UIScreen.Settings);
            RefreshSettingsControls();
        }

        public void ShowSettingsFromPause()
        {
            _settingsReturnScreen = UIScreen.Pause;
            SetPrimaryPanel(UIScreen.Settings);
            RefreshSettingsControls();
        }

        public void ShowUpgrade()
        {
            Time.timeScale = 1f;
            SetPrimaryPanel(UIScreen.Upgrade);
            RefreshUpgradePanel();
        }

        public void ShowGameOver()
        {
            RunStatsSnapshot snapshot = _runStatsTracker != null ? _runStatsTracker.CreateSnapshot() : default;
            ShowGameOver(snapshot);
        }

        public void ShowGameOver(RunStatsSnapshot snapshot)
        {
            Time.timeScale = 0f;
            SetPrimaryPanel(UIScreen.GameOver);
            SetText(finalTimeText, FormatTime(snapshot.SurvivalTime));
            SetText(finalKillText, snapshot.EnemyKills.ToString());
            SetText(moneyEarnedText, snapshot.CoinsEarned.ToString());
            SetText(gameOverBestTimeText, FormatTime(Mathf.Max(snapshot.BestSurvivalTime, snapshot.SurvivalTime)));
            SetText(gameOverBestKillText, Mathf.Max(snapshot.BestKillCount, snapshot.EnemyKills).ToString());
            RefreshMenuStats();
        }

        public void RestartCurrentScene()
        {
            Time.timeScale = 1f;
            Scene activeScene = SceneManager.GetActiveScene();

            if (activeScene.buildIndex >= 0)
            {
                SceneManager.LoadScene(activeScene.buildIndex);
                return;
            }

            SceneManager.LoadScene(activeScene.name);
        }

        private void WireButtons()
        {
            WireButton(playButton, nameof(playButton), () => PlayRequested?.Invoke());
            WireButton(mainMenuUpgradeButton, nameof(mainMenuUpgradeButton), ShowUpgrade);
            WireButton(mainMenuSettingsButton, nameof(mainMenuSettingsButton), ShowSettingsFromMainMenu);
            WireButton(pauseButton, nameof(pauseButton), () => PauseRequested?.Invoke());
            WireButton(upgradeBackButton, nameof(upgradeBackButton), ShowMainMenu);
            WireButton(settingsBackButton, nameof(settingsBackButton), HandleSettingsBack);
            WireButton(resumeButton, nameof(resumeButton), () => ResumeRequested?.Invoke());
            WireButton(pauseRestartButton, nameof(pauseRestartButton), () => RestartRequested?.Invoke());
            WireButton(pauseSettingsButton, nameof(pauseSettingsButton), ShowSettingsFromPause);
            WireButton(pauseHomeButton, nameof(pauseHomeButton), () => HomeRequested?.Invoke());
            WireButton(retryButton, nameof(retryButton), () => RestartRequested?.Invoke());
            WireButton(gameOverUpgradeButton, nameof(gameOverUpgradeButton), ShowUpgrade);
            WireButton(gameOverHomeButton, nameof(gameOverHomeButton), () => HomeRequested?.Invoke());

            WireSettingsControls();
            WireUpgradeRows();
        }

        private void WireSettingsControls()
        {
            WireSlider(musicVolumeSlider, MusicVolumePrefsKey, 1f, nameof(musicVolumeSlider));
            WireSlider(sfxVolumeSlider, SfxVolumePrefsKey, 1f, nameof(sfxVolumeSlider));
            WireToggle(vibrationToggle, VibrationPrefsKey, true, nameof(vibrationToggle));
            WireToggle(performanceModeToggle, PerformanceModePrefsKey, false, nameof(performanceModeToggle));
        }

        private void WireUpgradeRows()
        {
            if (upgradeRows == null)
            {
                return;
            }

            for (int index = 0; index < upgradeRows.Count; index++)
            {
                UpgradeRowBinding row = upgradeRows[index];
                if (row == null || row.UpgradeButton == null)
                {
                    continue;
                }

                PlayerMetaUpgradeType upgradeType = row.UpgradeType;
                row.UpgradeButton.onClick.RemoveAllListeners();
                row.UpgradeButton.onClick.AddListener(() => TryPurchaseUpgrade(upgradeType));
            }
        }

        private void WireButton(Button button, string fieldName, Action action)
        {
            if (button == null)
            {
                WarnMissing(fieldName);
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => action?.Invoke());
        }

        private void WireSlider(Slider slider, string prefsKey, float defaultValue, string fieldName)
        {
            if (slider == null)
            {
                WarnMissing(fieldName);
                return;
            }

            slider.onValueChanged.RemoveAllListeners();
            slider.value = PlayerPrefs.GetFloat(prefsKey, defaultValue);
            slider.onValueChanged.AddListener(value =>
            {
                PlayerPrefs.SetFloat(prefsKey, value);
                PlayerPrefs.Save();
            });
        }

        private void WireToggle(Toggle toggle, string prefsKey, bool defaultValue, string fieldName)
        {
            if (toggle == null)
            {
                WarnMissing(fieldName);
                return;
            }

            toggle.onValueChanged.RemoveAllListeners();
            toggle.isOn = PlayerPrefs.GetInt(prefsKey, defaultValue ? 1 : 0) != 0;
            toggle.onValueChanged.AddListener(value =>
            {
                PlayerPrefs.SetInt(prefsKey, value ? 1 : 0);
                PlayerPrefs.Save();
            });
        }

        private void HandleSettingsBack()
        {
            if (_settingsReturnScreen == UIScreen.Pause)
            {
                ShowPause();
                return;
            }

            ShowMainMenu();
        }

        private void TryPurchaseUpgrade(PlayerMetaUpgradeType upgradeType)
        {
            int cost = PlayerMetaUpgradeService.GetCost(upgradeType);
            bool purchased = _runStatsTracker != null
                ? _runStatsTracker.TrySpendWalletCoins(cost)
                : TrySpendWalletCoinsFallback(cost);

            if (!purchased || !PlayerMetaUpgradeService.TryPurchase(upgradeType, cost))
            {
                return;
            }

            RefreshUpgradePanel();
            RefreshMenuStats();
        }

        private static bool TrySpendWalletCoinsFallback(int cost)
        {
            int walletCoins = PlayerPrefs.GetInt(RunStatsTracker.WalletCoinsPrefsKey, 0);
            if (walletCoins < cost)
            {
                return false;
            }

            PlayerPrefs.SetInt(RunStatsTracker.WalletCoinsPrefsKey, walletCoins - cost);
            PlayerPrefs.Save();
            return true;
        }

        private void RefreshSettingsControls()
        {
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = PlayerPrefs.GetFloat(MusicVolumePrefsKey, 1f);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = PlayerPrefs.GetFloat(SfxVolumePrefsKey, 1f);
            }

            if (vibrationToggle != null)
            {
                vibrationToggle.isOn = PlayerPrefs.GetInt(VibrationPrefsKey, 1) != 0;
            }

            if (performanceModeToggle != null)
            {
                performanceModeToggle.isOn = PlayerPrefs.GetInt(PerformanceModePrefsKey, 0) != 0;
            }
        }

        private void RefreshMenuStats()
        {
            if (_runStatsTracker == null)
            {
                int walletCoins = PlayerPrefs.GetInt(RunStatsTracker.WalletCoinsPrefsKey, 0);
                float bestSurvivalTime = PlayerPrefs.GetFloat(RunStatsTracker.BestSurvivalTimePrefsKey, 0f);
                int bestKillCount = PlayerPrefs.GetInt(RunStatsTracker.BestKillCountPrefsKey, 0);
                int bestCoinsEarned = PlayerPrefs.GetInt(RunStatsTracker.BestCoinsEarnedPrefsKey, 0);
                int bestScore = PlayerPrefs.GetInt(RunStatsTracker.BestScorePrefsKey, 0);

                SetText(bestRunText, $"BEST {FormatTime(bestSurvivalTime)} | KILLS {bestKillCount}");
                SetText(walletText, $"{walletCoins}");
                SetText(bestScoreText, bestScore.ToString());
                SetText(bestTimeText, FormatTime(bestSurvivalTime));
                SetText(bestEnemiesKilledText, bestKillCount.ToString());
                SetText(bestCoinsText, bestCoinsEarned.ToString());
                return;
            }

            SetText(bestRunText, $"BEST {FormatTime(_runStatsTracker.BestSurvivalTime)} | KILLS {_runStatsTracker.BestKillCount}");
            SetText(walletText, $"{_runStatsTracker.WalletCoins}");
            SetText(bestScoreText, _runStatsTracker.BestScore.ToString());
            SetText(bestTimeText, FormatTime(_runStatsTracker.BestSurvivalTime));
            SetText(bestEnemiesKilledText, _runStatsTracker.BestKillCount.ToString());
            SetText(bestCoinsText, _runStatsTracker.BestCoinsEarned.ToString());
        }

        private void RefreshHud()
        {
            if (_runStatsTracker == null)
            {
                SetText(timeSurvivalText, "00:00");
                SetText(moneyText, "0");
                SetText(enemyDefeatedCountText, "0");
                SetText(scoreText, "0");
                return;
            }

            SetText(timeSurvivalText, FormatTime(_runStatsTracker.SurvivalTime));
            SetText(moneyText, _runStatsTracker.CoinsEarned.ToString());
            SetText(enemyDefeatedCountText, _runStatsTracker.EnemyKills.ToString());
            SetText(scoreText, _runStatsTracker.Score.ToString());
        }

        private void RefreshUpgradePanel()
        {
            int walletCoins = _runStatsTracker != null
                ? _runStatsTracker.WalletCoins
                : PlayerPrefs.GetInt(RunStatsTracker.WalletCoinsPrefsKey, 0);

            SetText(upgradeCurrencyText, $"CREDITS {walletCoins}");

            if (upgradeRows == null)
            {
                return;
            }

            for (int index = 0; index < upgradeRows.Count; index++)
            {
                UpgradeRowBinding row = upgradeRows[index];
                if (row == null)
                {
                    continue;
                }

                UpgradeDefinition definition = PlayerMetaUpgradeService.GetDefinition(row.UpgradeType);
                int level = PlayerMetaUpgradeService.GetLevel(row.UpgradeType);
                int cost = PlayerMetaUpgradeService.GetCost(row.UpgradeType);
                float nextValue = definition.ValuePerLevel * (level + 1);

                SetText(row.LevelText, $"LV {level}");
                SetText(row.ValueText, string.Format(definition.ValueFormat, nextValue));
                SetText(row.CostText, walletCoins >= cost ? $"{cost}" : $"NEED {cost}");

                if (row.UpgradeButton != null)
                {
                    row.UpgradeButton.interactable = walletCoins >= cost;
                }
            }
        }

        private void SetPrimaryPanel(UIScreen screen)
        {
            _currentScreen = screen;

            SetActive(mainMenuPanel, screen == UIScreen.MainMenu);
            SetActive(upgradePanel, screen == UIScreen.Upgrade);
            SetActive(settingsPanel, screen == UIScreen.Settings);
            SetActive(pausePanel, screen == UIScreen.Pause);
            SetActive(gameOverPanel, screen == UIScreen.GameOver);
            SetActive(gameplayHudPanel, screen == UIScreen.Gameplay
                || screen == UIScreen.Pause
                || (_settingsReturnScreen == UIScreen.Pause && screen == UIScreen.Settings));
        }

        private void ValidateRequiredReferences()
        {
            WarnIfMissing(mainMenuPanel, nameof(mainMenuPanel), "GameCanvas/UIRoot/SafeAreaRoot/MainMenuPanel");
            WarnIfMissing(gameplayHudPanel, nameof(gameplayHudPanel), "GameCanvas/UIRoot/SafeAreaRoot/GameplayHUDPanel");
            WarnIfMissing(upgradePanel, nameof(upgradePanel), "GameCanvas/UIRoot/SafeAreaRoot/UpgradePanel");
            WarnIfMissing(settingsPanel, nameof(settingsPanel), "GameCanvas/UIRoot/SafeAreaRoot/SettingsPanel");
            WarnIfMissing(pausePanel, nameof(pausePanel), "GameCanvas/UIRoot/SafeAreaRoot/PausePanel");
            WarnIfMissing(gameOverPanel, nameof(gameOverPanel), "GameCanvas/UIRoot/SafeAreaRoot/GameOverPanel");
            WarnIfMissing(playButton, nameof(playButton), "MainMenuPanel/StartRunButton");
            WarnIfMissing(mainMenuUpgradeButton, nameof(mainMenuUpgradeButton), "MainMenuPanel/BottomNavigationBar/UPDATEButton");
            WarnIfMissing(mainMenuSettingsButton, nameof(mainMenuSettingsButton), "MainMenuPanel/BottomNavigationBar/SETTINGButton");
            WarnIfMissing(walletText, nameof(walletText), "MainMenuPanel/TopHUD/ResourceBox/CoinValueText");
            WarnIfMissing(bestScoreText, nameof(bestScoreText), "MainMenuPanel/StatsBar/BESTSCORECell/BestScoreValueText");
            WarnIfMissing(bestTimeText, nameof(bestTimeText), "MainMenuPanel/StatsBar/BESTTIMECell/BestTimeValueText");
            WarnIfMissing(bestEnemiesKilledText, nameof(bestEnemiesKilledText), "MainMenuPanel/StatsBar/ENEMIESKILLEDCell/BestEnemiesKilledValueText");
            WarnIfMissing(bestCoinsText, nameof(bestCoinsText), "MainMenuPanel/StatsBar/BESTCOINSCell/BestCoinsValueText");
            WarnIfMissing(pauseButton, nameof(pauseButton), "GameplayHUDPanel/HudContentRoot/TopRightHud/PauseButton");
            WarnIfMissing(timeSurvivalText, nameof(timeSurvivalText), "GameplayHUDPanel/HudContentRoot/TopLeftHud/TimeFrame/TimeText");
            WarnIfMissing(moneyText, nameof(moneyText), "GameplayHUDPanel/HudContentRoot/TopRightHud/CoinFrame/CoinText");
            WarnIfMissing(enemyDefeatedCountText, nameof(enemyDefeatedCountText), "GameplayHUDPanel/HudContentRoot/TopLeftHud/KillFrame/KillText");
            WarnIfMissing(scoreText, nameof(scoreText), "GameplayHUDPanel/HudContentRoot/ScoreFrame/ScoreValueText");
            WarnIfMissing(retryButton, nameof(retryButton), "GameOverPanel/RetryButton");
        }

        private void WarnIfMissing(UnityEngine.Object reference, string fieldName, string expectedObject)
        {
            if (reference != null)
            {
                return;
            }

            WarnMissing(fieldName, expectedObject);
        }

        private void WarnMissing(string fieldName, string expectedObject = null)
        {
            if (!_missingReferenceWarnings.Add(fieldName))
            {
                return;
            }

            string expectedMessage = string.IsNullOrEmpty(expectedObject)
                ? "Assign it in the Inspector."
                : $"Create or assign '{expectedObject}' in the scene/prefab Inspector.";

            Debug.LogWarning($"{nameof(UISystem)} missing reference '{fieldName}'. {expectedMessage}", this);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }

        private static string FormatTime(float seconds)
        {
            int safeSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{safeSeconds / 60:00}:{safeSeconds % 60:00}";
        }

        private static void SetText(TextMeshProUGUI target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }

        private enum UIScreen
        {
            None,
            MainMenu,
            Gameplay,
            Upgrade,
            Settings,
            Pause,
            GameOver
        }
    }

    [Serializable]
    public sealed class UpgradeRowBinding
    {
        [SerializeField] private PlayerMetaUpgradeType upgradeType;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private Button upgradeButton;

        public PlayerMetaUpgradeType UpgradeType => upgradeType;
        public TextMeshProUGUI LevelText => levelText;
        public TextMeshProUGUI ValueText => valueText;
        public TextMeshProUGUI CostText => costText;
        public Button UpgradeButton => upgradeButton;
    }
}
